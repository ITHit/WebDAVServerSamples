using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using CalDAVServer.SqlStorage.AspNetCore.Acl;
using CalDAVServer.SqlStorage.AspNetCore.CalDav;
using CalDAVServer.SqlStorage.AspNetCore.Configuration;

namespace CalDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// WebDAV request context. Is used by WebDAV engine to resolve path into items.
    /// Implements abstract methods from <see cref="ContextAsync{IHierarchyItem}"/>,
    /// contains useful methods for working with transactions, connections, reading
    /// varios items from database.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItem>
         , IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Context options.
        /// </summary>
        private readonly DavContextConfig contextConfig;
        public DavContextConfig ContextConfig { get { return contextConfig; } }

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
        /// Represents array of users from storage.
        /// </summary>
        internal DavUser[] Users { get; private set; }
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="userIdentity">User Identity.</param>
        /// <param name="logger"><see cref="ILogger"/> instance.</param>
        /// <param name="configContext">WebDAV Context configuration.</param>
        /// <param name="configUsers">WebDAV Users configuration.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, ILogger logger, IOptions<DavContextConfig> configContext, IOptions<DavUsersConfig> configUsers)
            : base(httpContextAccessor.HttpContext)
        {
            this.contextConfig = configContext.Value;
            this.Users = configUsers.Value.Users;
            this.Identity = httpContextAccessor.HttpContext.User.Identity;
            this.Logger = logger;
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

            IHierarchyItem item = null;

            // Return items from [DAVLocation]/acl/ folder and subfolders.
            item = await AclFactory.GetAclItemAsync(this, path);
            if (item != null)
                return item;

            // Return items from [DAVLocation]/calendars/ folder and subfolders.
            item = await CalDavFactory.GetCalDavItemAsync(this, path);
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
                await RollBackTransactionAsync();
            }
            else
            {
                //commit the transaction if everything is ok.
                await CommitTransactionAsync();
            }
        }

        /// <summary>
        /// We implement <see cref="IDisposable"/> to have connection closed.
        /// </summary>
        public void Dispose()
        {
            CloseConnection();
        }
        public async ValueTask DisposeAsync()
        {
            await CloseConnectionAsync();
        }
        /// <summary>
        /// Asynchronously commits active transaction.
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (transaction != null && transaction.Connection != null)
            {
                await transaction.CommitAsync();
            }
        }
        /// <summary>
        /// Asynchronously rollbacks active transaction.
        /// </summary>
        public async Task RollBackTransactionAsync()
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
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
        /// Asynchronously closes the connection to the database.
        /// </summary>
        public async Task CloseConnectionAsync()
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
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
                this.connection = new SqlConnection(ContextConfig.ConnectionString);
      
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
