using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Web;
using System.Diagnostics;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using CardDAVServer.SqlStorage.AspNet.Acl;
using CardDAVServer.SqlStorage.AspNet.CardDav;

namespace CardDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// WebDAV request context. Is used by WebDAV engine to resolve path into items.
    /// Implements abstract methods from <see cref="DavContextBaseAsync"/>,
    /// contains useful methods for working with transactions, connections, reading
    /// varios items from database.
    /// </summary>
    public class DavContext :
        DavContextWebBaseAsync
        , IDisposable
    {
        /// <summary>
        /// Database connection string.
        /// </summary>
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["WebDAV"].ConnectionString;

        /// <summary>
        /// Cached connection for the request.
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Transaction for the request.
        /// </summary>
        private SqlTransaction transaction;

        /// <summary>
        /// Currently logged-in identity.
        /// </summary>
        internal IIdentity Identity { get; private set; }

        /// <summary>
        /// Currently logged-in user ID.
        /// </summary>
        internal string UserId { get { return Identity.Name.ToLower(); } }

        /// <summary>
        /// Gets <see cref="ILogger"/> instance.
        /// </summary>
        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DavContext"/> class from <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">Instance of <see cref="HttpContext"/>.</param>
        public DavContext(HttpContext context) : base(context)
        {
            Identity = context.User.Identity;
            Logger = CardDAVServer.SqlStorage.AspNet.Logger.Instance;
        }

        /// <summary>
        /// Resolves path to instance of <see cref="IHierarchyItem"/>.
        /// This method is called by WebDAV engine to resolve paths it encounters
        /// in request.
        /// </summary>
        /// <param name="path">Relative path to the item including query string.</param>
        /// <returns><see cref="IHierarchyItem"/> instance if item is found, <c>null</c> otherwise.</returns>
        public override async Task<IHierarchyItemAsync> GetHierarchyItemAsync(string path)
        {
            path = path.Trim(new[] { ' ', '/' });

            //remove query string.
            int ind = path.IndexOf('?');
            if (ind > -1)
            {
                path = path.Remove(ind);
            }

            IHierarchyItemAsync item = null;

            // Return items from [DAVLocation]/acl/ folder and subfolders.
            item = await AclFactory.GetAclItemAsync(this, path);
            if (item != null)
                return item;

            // Return items from [DAVLocation]/addressbooks/ folder and subfolders.
            item = await CardDavFactory.GetCardDavItemAsync(this, path);
            if (item != null)
                return item;

            // Return folder that corresponds to [DAVLocation] path. If no DavLocation section is defined in web.config/app.config [DAVLocation] is a website root.
            string davLocation = DavLocationFolder.DavLocationFolderPath.Trim('/');
            if (davLocation.Equals(path, StringComparison.InvariantCultureIgnoreCase))
            {
                return new DavLocationFolder(this);
            }

            // Return any folders above [DAVLocation] path.
            // Root folder with ICalendarDiscovery/IAddressbookDiscovery implementation is required for calendars and address books discovery by CalDAV/CardDAV clients.
            // All other folders are returned just for folders structure browsing convenience using WebDAV clients during dev time, not required by CalDAV/CardDAV clients.
            if (davLocation.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
            {
                int childFolderPathLength =  (davLocation+"/").IndexOf('/', path.Length+1);
                string childFolderPath = davLocation.Substring(0, childFolderPathLength);
                return new LogicalFolder(this, path, new []{new LogicalFolder(this, childFolderPath)});
            }

            Logger.LogDebug("Could not find item that corresponds to path: " + path);

            return null; // no hierarchy item that corresponds to path parameter was found in the repository
        }

        /// <summary>
        /// The method is called by WebDAV engine right before starting sending response to client.
        /// It is good point to either commit or rollback a transaction depending on whether
        /// and exception occurred.
        /// </summary>
        public override async Task BeforeResponseAsync()
        {
            // Analyze Exception property to see if there was an exception during request execution.
            // The property is set by engine.
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
        /// Executes SQL command which returns scalar result.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        /// <typeparam name="T">Type of object SQL command returns.</typeparam>
        /// <returns>Command result of type <typeparamref name="T"/>.</returns>
        public async Task<T> ExecuteScalarAsync<T>(string command, params object[] prms)
        {
            SqlCommand sqlCommand = await prepareCommandAsync(command, prms);
            object o = await sqlCommand.ExecuteScalarAsync();
            return o is DBNull ? default(T) : (T)o;
        }

        /// <summary>
        /// Executes SQL command which returns no results.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> ExecuteNonQueryAsync(string command, params object[] prms)
        {
            SqlCommand sqlCommand = await prepareCommandAsync(command, prms);

            Logger.LogDebug(string.Format("Executing SQL: {0}", sqlCommand.CommandText));
            Stopwatch stopWatch = Stopwatch.StartNew();

            int rowsAffected = await sqlCommand.ExecuteNonQueryAsync();

            Logger.LogDebug(string.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds));

            return rowsAffected;
        }

        /// <summary>
        /// Executes specified command and returns <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="commandBehavior">Value of <see cref="CommandBehavior"/> enumeration.</param>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Parameter pairs: SQL param name, SQL param value</param>
        /// <returns>Instance of <see cref="SqlDataReader"/>.</returns>
        public async Task<SqlDataReader> ExecuteReaderAsync(string command, params object[] prms)
        {
            SqlCommand sqlCommand = await prepareCommandAsync(command, prms);
            Logger.LogDebug(string.Format("Executing SQL: {0}", sqlCommand.CommandText));
            return await sqlCommand.ExecuteReaderAsync();
        }

        /// <summary>
        /// Executes specified command and returns <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="commandBehavior">Value of <see cref="CommandBehavior"/> enumeration.</param>
        /// <param name="command">Command text.</param>
        /// <param name="prms">Parameter pairs: SQL param name, SQL param value.</param>
        /// <returns>Instance of <see cref="SqlDataReader"/>.</returns>
        public async Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior commandBehavior, string command, params object[] prms)
        {
            SqlCommand sqlCommand = await prepareCommandAsync(command, prms);
            Logger.LogDebug(string.Format("Executing SQL: {0}", sqlCommand.CommandText));
            return await sqlCommand.ExecuteReaderAsync(commandBehavior);
        }

        /// <summary>
        /// Creates <see cref="SqlCommand"/>.
        /// </summary>
        /// <returns>Instance of <see cref="SqlCommand"/>.</returns>
        private async Task<SqlCommand> createNewCommandAsync()
        {
            if (this.connection == null)
            {
                this.connection = new SqlConnection(connectionString);
      
                await this.connection.OpenAsync();
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
        /// <param name="prms">Either list of SqlParameters or command parameters in pairs: name, value</param>
        /// <returns>Instace of <see cref="SqlCommand"/>.</returns>
        private async Task<SqlCommand> prepareCommandAsync(string command, params object[] prms)
        {
            SqlCommand cmd = await createNewCommandAsync();
            cmd.CommandText = command;
            for (int i = 0; i < prms.Length; i++)
            {
                if (prms[i] is string)
                {
                    // name-value pair
                    cmd.Parameters.AddWithValue((string)prms[i], prms[i + 1] ?? DBNull.Value);
                    i ++;
                }
                else if (prms[i] is SqlParameter)
                {
                    // SqlParameter
                    cmd.Parameters.Add(prms[i] as SqlParameter);
                }
                else
                {
                    throw new ArgumentException(prms[i] + "is invalid parameter name");
                }
            }

            return cmd;
        }
    }
}
