using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class2;
using ITHit.WebDAV.Server.Paging;

namespace WebDAVServer.SqlStorage.HttpListener
{
    /// <summary>
    /// WebDAV request context. Is used by WebDAV engine to resolve path into items.
    /// Implements abstract methods from <see cref="ContextAsync{IHierarchyItem}"/>,
    /// contains useful methods for working with transactions, connections, reading
    /// varios items from database.
    /// </summary>
    public class DavContext :
        ContextHttpListenerAsync<IHierarchyItem>
        , IDisposable
    {
        /// <summary>
        /// Database connection string.
        /// </summary>
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["WebDAV"].ConnectionString;

        /// <summary>
        /// Id of root folder.
        /// </summary>
        private readonly Guid rootId = new Guid("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Currently authenticated user.
        /// </summary>
        private readonly IPrincipal currentUser;
        /// <summary>
        /// Singleton instance of <see cref="WebSocketsService"/>.
        /// </summary>
        public WebSocketsService socketService { get { return WebSocketsService.Service; } private set { } }

        /// <summary>
        /// Cached connection for the request.
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Transaction for the request.
        /// </summary>
        private SqlTransaction transaction;

        /// <summary>
        /// Initializes DavContext class.
        /// </summary>
        static DavContext()
        {
            var exePath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath);
            var dataPath = Path.Combine(Directory.GetParent(exePath).FullName, "App_Data");
            connectionString = connectionString.Replace("|DataDirectory|", dataPath);
        }

        /// <summary>
        /// Initializes a new instance of the DavContext class.
        /// </summary>
        /// <param name="context">Instance of <see cref="HttpListenerContext"/>.</param>
        /// <param name="prefixes">Collection of http listener prefixes.</param>
        /// <param name="user">Current use principal. null if anonymous.</param>
        public DavContext(HttpListenerContext context, HttpListenerPrefixCollection prefixes, IPrincipal user, ILogger logger)
            : base(context, prefixes)
        {
            this.currentUser = user;
        }

        /// <summary>
        /// Gets currently logged in user. <c>null</c> if request is anonymous.
        /// </summary>
        public IPrincipal User
        {
            get { return currentUser; }
        }

        /// <summary>
        /// Resolves path to instance of <see cref="IHierarchyItem"/>.
        /// This method is called by WebDAV engine to resolve paths it encounters
        /// in request.
        /// </summary>
        /// <param name="path">Relative path to the item including query string.</param>
        /// <returns><see cref="IHierarchyItem"/> instance if item is found, <c>null</c> otherwise.</returns>
        public override async Task<IHierarchyItem> GetHierarchyItemAsync(string path)
        {
            path = path.Trim(new[] { ' ', '/' });

            //remove query string.
            int ind = path.IndexOf('?');
            if (ind > -1)
            {
                path = path.Remove(ind);
            }

            if (path == "")
            {
                // get root folder
                return await getRootFolderAsync();
            }

            // find root
            return await getItemByPathAsync(path);
        }

        /// <summary>
        /// The method is called by WebDAV engine right before starting sending response to client.
        /// It is good point to either commit or rollback a transaction depending on whether
        /// and exception occurred.
        /// </summary>
        public override async Task BeforeResponseAsync()
        {
            //analyze Exception property to see if there was an exception during request execution.
            //The property is set by engine.
            if (Exception != null)
            {
                //rollback the transaction if something went wrong.
                
                RollBackTransaction();
            }
            else
            {
                //commit the transaction if everything is ok.
                
                CommitTransaction();
            }
        }
        /// <summary>
        /// We implement <see cref="IDisposable"/> to have connection closed.
        /// </summary>
        public void Dispose()
        {
            CloseConnection();
        }
     
        /// <summary>
        /// Commits active transaction.
        /// </summary>
        public void CommitTransaction()
        {
            if (transaction != null && transaction.Connection != null)
            {
                transaction.Commit();
            }
        }
     
        /// <summary>
        /// Rollbacks active transaction.
        /// </summary>
        public void RollBackTransaction()
        {
            if (transaction != null)
            {
                transaction.Rollback();
            }
        }


        /// <summary>
        /// Closes connection.
        /// </summary>
        public void CloseConnection()
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Reads <see cref="DavFile"/> or <see cref="DavFolder"/> depending on type 
        /// <typeparamref name="T"/> from database.
        /// </summary>
        /// <typeparam name="T">Type of hierarchy item to read(file or folder).</typeparam>
        /// <param name="parentPath">Path to parent hierarchy item.</param>
        /// <param name="command">SQL expression which returns hierachy item records.</param>
        /// <param name="prms">Sequence: sql parameter1 name, sql parameter1 value, sql parameter2 name,
        /// sql parameter2 value...</param>
        /// <returns>List of requested items.</returns>
        public async Task<IList<T>> ExecuteItemAsync<T>(string parentPath, string command, params object[] prms)
            where T : class, IHierarchyItem
        {
            IList<T> children = new List<T>();

            using (SqlDataReader reader = await prepareCommand(command, prms).ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Guid itemId = (Guid)reader["ItemID"];
                    Guid parentId = (Guid)reader["ParentItemID"];
                    ItemType itemType = (ItemType)reader.GetInt32(reader.GetOrdinal("ItemType"));
                    string name = reader.GetString(reader.GetOrdinal("Name"));
                    DateTime created = reader.GetDateTime(reader.GetOrdinal("Created"));
                    DateTime modified = reader.GetDateTime(reader.GetOrdinal("Modified"));
                    switch (itemType)
                    {
                        case ItemType.File:
                            children.Add(new DavFile(
                                this,
                                itemId,
                                parentId,
                                name,
                                parentPath + EncodeUtil.EncodeUrlPart(name),
                                created,
                                modified) as T);
                            break;
                        case ItemType.Folder:
                            children.Add(new DavFolder(
                                this,
                                itemId,
                                parentId,
                                name,
                                (parentPath + EncodeUtil.EncodeUrlPart(name) + "/").TrimStart('/'),
                                created,
                                modified) as T);
                            break;
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// Reads <see cref="DavFile"/> or <see cref="DavFolder"/> depending on type 
        /// Fills Reads <see cref="PageResults.TotalItems"/> from TotalRowsCount and <see cref="IHierarchyItem.Path"/> from RelativePath fields.
        /// </summary>
        /// <param name="parentPath">Path to parent hierarchy item.</param>
        /// <param name="command">SQL expression which returns hierachy item records.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="prms">Sequence: sql parameter1 name, sql parameter1 value, sql parameter2 name,
        /// sql parameter2 value...</param>
        /// <returns>List of requested items.</returns>
        public async Task<PageResults> ExecuteItemPagedHierarchyAsync(string parentPath, string command, long? offset, long? nResults, params object[] prms)
        {
            long? totalRowsCount = null;
            IList<IHierarchyItem> children = new List<IHierarchyItem>();

            using (SqlDataReader reader = await prepareCommand(command, prms).ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Guid itemId = (Guid)reader["ItemID"];
                    Guid parentId = (Guid)reader["ParentItemID"];
                    ItemType itemType = (ItemType)reader.GetInt32(reader.GetOrdinal("ItemType"));
                    string name = reader.GetString(reader.GetOrdinal("Name"));
                    DateTime created = reader.GetDateTime(reader.GetOrdinal("Created"));
                    DateTime modified = reader.GetDateTime(reader.GetOrdinal("Modified"));
                    FileAttributes fileAttributes = (FileAttributes)reader.GetInt32(
                        reader.GetOrdinal("FileAttributes"));
                    string relativePath = reader.GetString(reader.GetOrdinal("RelativePath"));
                    string relativePathEncoded = string.Join("/", relativePath.Split('/').Select(EncodeUtil.EncodeUrlPart));
                    if (!totalRowsCount.HasValue)
                    {
                        totalRowsCount = reader.GetInt32(reader.GetOrdinal("TotalRowsCount"));
                    }

                    switch (itemType)
                    {
                        case ItemType.File:
                            children.Add(new DavFile(
                                this,
                                itemId,
                                parentId,
                                name,
                                parentPath + relativePathEncoded,
                                created,
                                modified));
                            break;
                        case ItemType.Folder:
                            children.Add(new DavFolder(
                                this,
                                itemId,
                                parentId,
                                name,
                                (parentPath + relativePathEncoded + "/").TrimStart('/'),
                                created,
                                modified));
                            break;
                    }
                }
            }

            if (!(offset.HasValue || nResults.HasValue))
            {
                return new PageResults(children, totalRowsCount);
            }

            IEnumerable<IHierarchyItem> pagedResult = children.AsEnumerable();
            if (offset.HasValue)
            {
                pagedResult = pagedResult.Skip((int)offset.Value);
            }

            if (nResults.HasValue)
            {
                pagedResult = pagedResult.Take((int)nResults.Value);
            }

            return new PageResults(pagedResult, totalRowsCount);
        }

        /// <summary>
        /// Reads <see cref="PropertyValue"/> from database by executing SQL command.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value</param>
        /// <returns>List of <see cref="PropertyValue"/>.</returns>
        public async Task<IList<PropertyValue>> ExecutePropertyValueAsync(string command, params object[] prms)
        {
            List<PropertyValue> l = new List<PropertyValue>();

            using (SqlDataReader reader = await prepareCommand(command, prms).ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string name = reader.GetString(reader.GetOrdinal("Name"));
                    string ns = reader.GetString(reader.GetOrdinal("Namespace"));
                    string value = reader.GetString(reader.GetOrdinal("PropVal"));
                    l.Add(new PropertyValue(new PropertyName(name, ns), value));
                }
            }

            return l;
        }

        /// <summary>
        /// Executes SQL command which returns scalar result.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        /// <typeparam name="T">Type of object SQL command returns.</typeparam>
        /// <returns>Command result of type <typeparamref name="T"/>.</returns>
        public T ExecuteScalar<T>(string command, params object[] prms)
        {
            object o = prepareCommand(command, prms).ExecuteScalar();
            return o == null || o is DBNull ? default(T) : (T)o;
        }
        /// <summary>
        /// Executes SQL command which returns scalar result.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        /// <typeparam name="T">Type of object SQL command returns.</typeparam>
        /// <returns>Command result of type <typeparamref name="T"/>.</returns>
        public async Task<T> ExecuteScalarAsync<T>(string command, params object[] prms)
        {
            object o = await prepareCommand(command, prms).ExecuteScalarAsync();
            return o == null || o is DBNull ? default(T) : (T)o;
        }

        /// <summary>
        /// Executes SQL command which returns no results.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        public void ExecuteNonQuery(string command, params object[] prms)
        {
            prepareCommand(command, prms).ExecuteNonQuery();
        }
        /// <summary>
        /// Executes SQL command which returns no results.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        public async Task ExecuteNonQueryAsync(string command, params object[] prms)
        {
            await prepareCommand(command, prms).ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes SQL command which returns no results.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Command parameters as <see cref="SqlParameter"/> instances.</param>
        public async Task ExecuteNonQueryAsync(string command, params SqlParameter[] prms)
        {
            SqlCommand cmd = createNewCommand();
            cmd.CommandText = command;
            cmd.Parameters.AddRange(prms);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes specified command and returns <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="commandBehavior">Value of <see cref="CommandBehavior"/> enumeration.</param>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Parameter pairs: SQL param name, SQL param value</param>
        /// <returns>Instance of <see cref="SqlDataReader"/>.</returns>
        public async Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior commandBehavior, string command, params object[] prms)
        {
            return await prepareCommand(command, prms).ExecuteReaderAsync(commandBehavior);
        }

        /// <summary>
        /// Returns list of <see cref="LockInfo"/> from database by executing specified command
        /// with specified parameters.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Pairs of parameter name, parameter value.</param>
        /// <returns>List of <see cref="LockInfo"/>.</returns>
        public async Task<List<LockInfo>> ExecuteLockInfo(string command, params object[] prms)
        {
            List<LockInfo> l = new List<LockInfo>();

            using (SqlDataReader reader = await prepareCommand(command, prms).ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    LockInfo li = new LockInfo();
                    li.Token = reader.GetString(reader.GetOrdinal("Token"));
                    li.Level = reader.GetBoolean(reader.GetOrdinal("Shared")) ? LockLevel.Shared : LockLevel.Exclusive;
                    li.IsDeep = reader.GetBoolean(reader.GetOrdinal("Deep"));

                    DateTime expires = reader.GetDateTime(reader.GetOrdinal("Expires"));
                    if (expires <= DateTime.UtcNow)
                    {
                        li.TimeOut = TimeSpan.Zero;
                    }
                    else
                    {
                        li.TimeOut = expires - DateTime.UtcNow;
                    }

                    li.Owner = reader.GetString(reader.GetOrdinal("Owner"));

                    l.Add(li);
                }
            }

            return l;
        }

        /// <summary>
        /// Reads item from database by path.
        /// </summary>
        /// <param name="path">Item path.</param>
        /// <returns>Instance of <see cref="IHierarchyItem"/>.</returns>
        private async Task<IHierarchyItem> getItemByPathAsync(string path)
        {
            Guid id = rootId;

            string[] names = path.Split('/');
            int last = names.Length - 1;
            while (last > 0 && names[last] == string.Empty)
            {
                last--;
            }

            for (int i = 0; i < last; i++)
            {
                if (!string.IsNullOrEmpty(names[i]))
                {
                    object result = await ExecuteScalarAsync<object>(
                        @"SELECT 
                             ItemId
                          FROM Item
                          WHERE Name = @Name AND ParentItemId = @Parent",
                        "@Name", EncodeUtil.DecodeUrlPart(names[i]),
                        "@Parent", id);

                    if (result != null)
                    {
                        id = (Guid)result;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            // get item properties
            string command =
                  @"SELECT
                       ItemId
                     , ParentItemId
                     , ItemType
                     , Name
                     , Created
                     , Modified
                           
                     FROM Item
                  WHERE Name = @Name AND ParentItemId = @Parent";
            IList<DavHierarchyItem> davHierarchyItems = await ExecuteItemAsync<DavHierarchyItem>(
                string.Join("/", names, 0, last) + "/",
                command,
                "@Name", EncodeUtil.DecodeUrlPart(names[last]),
                "@Parent", id);
            return davHierarchyItems.FirstOrDefault();
        }

        /// <summary>
        /// Reads root folder.
        /// </summary>
        /// <param name="path">Root folder path.</param>
        /// <returns>Instance of <see cref="IHierarchyItem"/>.</returns>
        public async Task<IHierarchyItem> getRootFolderAsync()
        {
            string command =
               @"SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , '' as Name
                    , Created
                    , Modified
                          
                    FROM Item
                 WHERE ItemId = @ItemId";
            IList<IHierarchyItem> hierarchyItems = await ExecuteItemAsync<IHierarchyItem>(
                "",
                command,
                "@ItemId", rootId);
            return hierarchyItems.FirstOrDefault();
        }

        /// <summary>
        /// Creates <see cref="SqlCommand"/>.
        /// </summary>
        /// <returns>Instance of <see cref="SqlCommand"/>.</returns>
        private SqlCommand createNewCommand()
        {
            if (this.connection == null)
            {
                this.connection = new SqlConnection(connectionString);
               
                this.connection.Open();
                this.transaction = this.connection.BeginTransaction(IsolationLevel.ReadUncommitted);
            }

            SqlCommand newCmd = connection.CreateCommand();
            newCmd.Transaction = transaction;
            return newCmd;
        }

        /// <summary>
        /// Creates <see cref="SqlCommand"/>.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Command parameters in pairs: name, value</param>
        /// <returns>Instace of <see cref="SqlCommand"/>.</returns>
        private SqlCommand prepareCommand(string command, params object[] prms)
        {
            if (prms.Length % 2 != 0)
            {
                throw new ArgumentException("Incorrect number of parameters");
            }

            SqlCommand cmd = createNewCommand();
            cmd.CommandText = command;
            for (int i = 0; i < prms.Length; i += 2)
            {
                if (!(prms[i] is string))
                {
                    throw new ArgumentException(prms[i] + "is invalid parameter name");
                }

                cmd.Parameters.AddWithValue((string)prms[i], prms[i + 1]);
            }

            return cmd;
        }
    }
}
