using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks; 

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Core;
using ITHit.WebDAV.Server.Class2;
using WebDAVServer.SqlStorage.AspNetCore.Options;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// WebDAV request context. Is used by WebDAV engine to resolve path into items.
    /// Implements abstract methods from <see cref="DavContextBaseAsync"/>,
    /// contains useful methods for working with transactions, connections, reading
    /// varios items from database.
    /// </summary>
    public class DavContext :
        DavContextCoreBaseAsync
        , IDisposable
    {
        /// <summary>
        /// Context options.
        /// </summary>
        private readonly DavContextOptions contextOptions;
        public DavContextOptions ContextOptions { get { return contextOptions; } }

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
        public WebSocketsService socketService { get; private set; }

        /// <summary>
        /// Cached connection for the request.
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Transaction for the request.
        /// </summary>
        private SqlTransaction transaction;
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="configOptions">WebDAV Context configuration options.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, IOptions<DavContextOptions> configOptions
            , WebSocketsService socketService
            )
            : base(httpContextAccessor.HttpContext)
        {
            this.contextOptions = configOptions.Value;
            this.currentUser = httpContextAccessor.HttpContext.User;
            this.socketService = socketService;
        }

        /// <summary>
        /// Gets currently logged in user. <c>null</c> if request is anonymous.
        /// </summary>
        public IPrincipal User
        {
            get { return currentUser; }
        }

        /// <summary>
        /// Resolves path to instance of <see cref="IHierarchyItemAsync"/>.
        /// This method is called by WebDAV engine to resolve paths it encounters
        /// in request.
        /// </summary>
        /// <param name="path">Relative path to the item including query string.</param>
        /// <returns><see cref="IHierarchyItemAsync"/> instance if item is found, <c>null</c> otherwise.</returns>
        public override async Task<IHierarchyItemAsync> GetHierarchyItemAsync(string path)
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
            if (transaction != null)
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
            where T : class, IHierarchyItemAsync
        {
            IList<T> children = new List<T>();
            using (SqlDataReader reader = await prepareCommand(command, prms).ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    Guid itemId = (Guid)reader["ItemID"];
                    Guid parentId = (Guid)reader["ParentItemID"];
                    ItemType itemType = (ItemType)reader.GetInt32(reader.GetOrdinal("ItemType"));
                    string name = reader.GetString(reader.GetOrdinal("Name"));
                    DateTime created = reader.GetDateTime(reader.GetOrdinal("Created"));
                    DateTime modified = reader.GetDateTime(reader.GetOrdinal("Modified"));
                    FileAttributes fileAttributes = (FileAttributes)reader.GetInt32(
                        reader.GetOrdinal("FileAttributes"));
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
                                modified,fileAttributes) as T);
                            break;
                        case ItemType.Folder:
                            children.Add(new DavFolder(
                                this,
                                itemId,
                                parentId,
                                name,
                                (parentPath + EncodeUtil.EncodeUrlPart(name) + "/").TrimStart('/'),
                                created,
                                modified,fileAttributes) as T);
                            break;
                    }
                }
            }

            return children;
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
                while (reader.Read())
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
            return o is DBNull ? default(T) : (T)o;
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
            return o is DBNull ? default(T) : (T)o;
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
        public List<LockInfo> ExecuteLockInfo(string command, params object[] prms)
        {
            List<LockInfo> l = new List<LockInfo>();
            using (SqlDataReader reader = prepareCommand(command, prms).ExecuteReader())
            {
                while (reader.Read())
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
        /// <returns>Instance of <see cref="IHierarchyItemAsync"/>.</returns>
        private async Task<IHierarchyItemAsync> getItemByPathAsync(string path)
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
                     , FileAttributes       
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
        /// <returns>Instance of <see cref="IHierarchyItemAsync"/>.</returns>
        public async Task<IHierarchyItemAsync> getRootFolderAsync()
        {
            string command =
               @"SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , '' as Name
                    , Created
                    , Modified
                    , FileAttributes       
                    FROM Item
                 WHERE ItemId = @ItemId";
            IList<IHierarchyItemAsync> hierarchyItems = await ExecuteItemAsync<IHierarchyItemAsync>(
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
                this.connection = new SqlConnection(ContextOptions.ConnectionString);
               
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
