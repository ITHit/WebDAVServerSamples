using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Linq;
using System.Data;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Paging;

namespace CardDAVServer.SqlStorage.AspNet.CardDav
{
    /// <summary>
    /// Represents a CardDAV address book (address book folder).
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[AddressbookFolderId]
    /// </summary>
    public class AddressbookFolder : DavHierarchyItem, IAddressbookFolderAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync
    {
        /// <summary>
        /// Loads address book folder by ID. Returns null if addressbook folder was not found.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="addressbookFolderId">ID of the address book folder to load.</param>
        /// <returns><see cref="IAddressbookFolderAsync"/> instance.</returns>
        public static async Task<IAddressbookFolderAsync> LoadByIdAsync(DavContext context, Guid addressbookFolderId)
        {
            // Load only address book that the use has access to. 
            // Also load complete ACL for this address book.
            string sql =
                @"SELECT * FROM [card_AddressbookFolder] 
                  WHERE [AddressbookFolderId] = @AddressbookFolderId
                  AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [card_Access]
                  WHERE [AddressbookFolderId] = @AddressbookFolderId
                  AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)";

            return (await LoadAsync(context, sql,
                  "@UserId"             , context.UserId
                , "@AddressbookFolderId", addressbookFolderId
                )).FirstOrDefault();
        }

        /// <summary>
        /// Loads all address books.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>List of <see cref="IAddressbookFolderAsync"/> items.</returns>
        public static async Task<IEnumerable<IAddressbookFolderAsync>> LoadAllAsync(DavContext context)
        {
            // Load only address books that the use has access to. 
            // Also load complete ACL for each address book, but only if user has access to that address book.
            string sql =
                @"SELECT * FROM [card_AddressbookFolder] 
                  WHERE [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [card_Access] 
                  WHERE [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)";

            return await LoadAsync(context, sql, "@UserId", context.UserId);
        }

        /// <summary>
        /// Loads address book by SQL.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="sql">SQL that queries [card_AddressbookFolder] table.</param>
        /// <param name="prms">List of SQL parameters.</param>
        /// <returns>List of <see cref="IAddressbookFolderAsync"/> items.</returns>
        private static async Task<IEnumerable<IAddressbookFolderAsync>> LoadAsync(DavContext context, string sql, params object[] prms)
        {
            IList<IAddressbookFolderAsync> addressbookFolders = new List<IAddressbookFolderAsync>();

            using (SqlDataReader reader = await context.ExecuteReaderAsync(sql, prms))            
            {
                DataTable addressbooks = new DataTable();
                addressbooks.Load(reader);

                DataTable access = new DataTable();
                access.Load(reader);

                foreach (DataRow rowAddressbookFolder in addressbooks.Rows)
                {
                    Guid addressbookFolderId = rowAddressbookFolder.Field<Guid>("AddressbookFolderId");

                    string filter = string.Format("AddressbookFolderId = '{0}'", addressbookFolderId);
                    DataRow[] rowsAccess = access.Select(filter);

                    addressbookFolders.Add(new AddressbookFolder(context, addressbookFolderId, rowAddressbookFolder, rowsAccess));
                }
            }

            return addressbookFolders;
        }

        /// <summary>
        /// Creates a new address book folder.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param> 
        /// <param name="name">Address book name.</param>
        /// <param name="description">Address book description.</param>
        internal static async Task CreateAddressbookFolderAsync(DavContext context, string name, string description)
        {
            // 1. Create address book.
            // 2. Grant owner privileges to the user on the created address book(s).
            string sql = @"INSERT INTO [card_AddressbookFolder] (
                          [AddressbookFolderId]
                        , [Name]
                        , [Description]
                    ) VALUES (
                          @AddressbookFolderId
                        , @Name
                        , @Description
                    )
                    ; INSERT INTO [card_Access] (
                          [AddressbookFolderId]
                        , [UserId]
                        , [Owner]
                        , [Read]
                        , [Write]
                    ) VALUES (
                          @AddressbookFolderId
                        , @UserId
                        , @Owner
                        , @Read
                        , @Write
                    )";

            Guid addressbookFolderId = Guid.NewGuid();

            await context.ExecuteNonQueryAsync(sql,
                  "@AddressbookFolderId", addressbookFolderId
                , "@Name"               , name
                , "@Description"        , description
                , "@UserId"             , context.UserId
                , "@Owner"              , true
                , "@Read"               , true
                , "@Write"              , true
                );
        }

        /// <summary>
        /// This address book folder ID.
        /// </summary>
        private readonly Guid addressbookFolderId;

        /// <summary>
        /// Contains data from [card_AddressbookFolder] table.
        /// </summary>
        private readonly DataRow rowAddressbookFolder;

        /// <summary>
        /// Contains data from [card_Access] table.
        /// </summary>
        private readonly DataRow[] rowsAccess;

        /// <summary>
        /// Gets display name of the address book.
        /// </summary>
        /// <remarks>CalDAV clients typically never request this property.</remarks>
        public override string Name
        {
            get { return rowAddressbookFolder != null ? rowAddressbookFolder.Field<string>("Name") : null; }
        }

        /// <summary>
        /// Gets item path.
        /// </summary>
        public override string Path
        {
            get
            {
                return string.Format("{0}{1}/", AddressbooksRootFolder.AddressbooksRootFolderPath, addressbookFolderId);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressbookFolder"/> class from database source.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="addressbookFolderId">Address book folder ID.</param>
        /// <param name="addressbook">Contains data from [card_AddressbookFolder] table.</param>
        /// <param name="rowsAccess">Contains data from [card_Access] table for this address book.</param>
        private AddressbookFolder(DavContext context, Guid addressbookFolderId, DataRow addressbook, DataRow[] rowsAccess)
            : base(context)
        {
            this.addressbookFolderId    = addressbookFolderId;
            this.rowAddressbookFolder   = addressbook;
            this.rowsAccess             = rowsAccess;
        }

        /// <summary>
        /// Returns a list of address book files that correspont to the specified list of item paths.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by the Engine during <b>addressbook-multiget</b> call.
        /// </para>
        /// <para>
        /// For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        /// </para>
        /// </remarks>
        /// <param name="pathList">Addressbook files path list.</param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of address book files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICardFileAsync>> MultiGetAsync(IEnumerable<string> pathList, IEnumerable<PropertyName> propNames)
        {
            // Get list of file names from path list.
            IEnumerable<string> fileNames = pathList.Select(a => System.IO.Path.GetFileNameWithoutExtension(a));

            return await CardFile.LoadByFileNamesAsync(Context, fileNames, PropsToLoad.All);
        }

        /// <summary>
        /// Returns a list of address book files that match specified filter. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by the Engine during <b>addressbook-query</b> call.
        /// </para>
        /// </remarks>
        /// <param name="rawQuery">
        /// Raw query sent by the client.
        /// </param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of address book files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICardFileAsync>> QueryAsync(string rawQuery, IEnumerable<PropertyName> propNames)
        {
            // For the sake of simplicity we just call GetChildren returning all items. 
            // Typically you will return only items that match the query.
            return (await GetChildrenAsync(propNames.ToList(), null, null, null)).Page.Cast<ICardFileAsync>();
        }

        /// <summary>
        /// Provides a human-readable description of the address book collection.
        /// </summary>
        public string AddressbookDescription 
        {
            get { return rowAddressbookFolder.Field<string>("Description"); }
        }

        /// <summary>
        /// Retrieves children of this folder.
        /// </summary>
        /// <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Children of the folder.</returns>
        public async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            // Here we enumerate all business cards contained in this address book.
            // You can filter children items in this implementation and 
            // return only items that you want to be available for this 
            // particular user.

            // Typically only getcontenttype and getetag properties are requested in GetChildren call by CalDAV/CardDAV clients.
            // The iCalendar/vCard (calendar-data/address-data) is typically requested not in GetChildren, but in a separate multiget 
            // report, in MultiGetAsync() method call, that follows this request.

            // Bynari submits PROPFIND without props - Engine will request getcontentlength

            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();
            return new PageResults((await CardFile.LoadByAddressbookFolderIdAsync(Context, addressbookFolderId, PropsToLoad.Minimum)), null);
        }

        /// <summary>
        /// Creates a file that contains business card item in this address book.
        /// </summary>
        /// <param name="name">Name of the new file. Unlike with CalDAV it is NOT equel to vCard UID.</param>
        /// <returns>The newly created file.</returns>
        /// <remarks></remarks>
        public async Task<IFileAsync> CreateFileAsync(string name)
        {
            // The actual business card file is created in datatbase in CardFile.Write call.
            string fileName = System.IO.Path.GetFileNameWithoutExtension(name);
            return CardFile.CreateCardFile(Context, addressbookFolderId, fileName);
        }

        /// <summary>
        /// Creating new folders is not allowed in address book folders.
        /// </summary>
        /// <param name="name">Name of the folder.</param>
        public async Task CreateFolderAsync(string name)
        {
            throw new DavException("Not allowed.", DavStatus.NOT_ALLOWED);
        }

        /// <summary>
        /// Move this folder to folder <paramref name="destFolder"/>.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">Name for this folder at destination.</param>
        /// <param name="multistatus">Instance of <see cref="MultistatusException"/>
        /// to fill with errors ocurred while moving child items.</param>
        /// <returns></returns>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            // Here we support only addressbooks renaming. Check that user has permissions to write.
            string sql = @"UPDATE [card_AddressbookFolder] SET Name=@Name 
                WHERE [AddressbookFolderId]=@AddressbookFolderId
                AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Write] = 1)";

            if (await Context.ExecuteNonQueryAsync(sql, 
                  "@Name"               , destName
                , "@UserId"             , Context.UserId
                , "@AddressbookFolderId", addressbookFolderId) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }
        }

        /// <summary>
        /// Deletes this address book.
        /// </summary>
        /// <param name="multistatus"><see cref="MultistatusException"/> to populate with child files and folders failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            // Delete address book and all vCards associated with it. Check that user has permissions to delete.
            string sql = @"DELETE FROM [card_AddressbookFolder] 
                WHERE [AddressbookFolderId]=@AddressbookFolderId
                AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Owner] = 1)";

            if (await Context.ExecuteNonQueryAsync(sql,
                  "@UserId"             , Context.UserId
                , "@AddressbookFolderId", addressbookFolderId) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }
        }

        /// <summary>
        /// Gets values of all properties or selected properties for this item.
        /// </summary>
        /// <param name="names">Property names which values are
        /// requested by WebDAV client. If a property does not exist for this hierarchy item
        /// then the property value shall not be returned.
        /// </param>
        /// <param name="allprop">If it is true, besides properties listed in props you need to return
        /// all properties you think may be useful to client.
        /// </param>
        /// <returns></returns>
        public override async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> names, bool allprop)
        {
            IList<PropertyValue> propVals = await GetPropertyValuesAsync(
                    "SELECT [Name], [Namespace], [PropVal] FROM [card_AddressbookFolderProperty] WHERE [AddressbookFolderId] = @AddressbookFolderId",
                    "@AddressbookFolderId", addressbookFolderId);

            if (allprop)
            {
                return propVals;
            }
            else
            {
                IList<PropertyValue> requestedPropVals = new List<PropertyValue>();
                foreach (PropertyValue p in propVals)
                {
                    if (names.Contains(p.QualifiedName))
                    {
                        requestedPropVals.Add(p);
                    }
                }
                return requestedPropVals;
            }
        }

        /// <summary>
        /// Adds, modifies and removes properties for this item.
        /// </summary>
        /// <param name="setProps">List of properties to be set.</param>
        /// <param name="delProps">List of property names to be removed. Properties that don't exist shall be skipped.</param>
        /// <param name="multistatus">Information about errors.</param>
        public override async Task UpdatePropertiesAsync(
            IList<PropertyValue> setProps,
            IList<PropertyName> delProps,
            MultistatusException multistatus)
        {
            foreach (PropertyValue p in setProps)
            {
                await SetPropertyAsync(p); // create or update property
            }

            foreach (PropertyName p in delProps)
            {
                await RemovePropertyAsync(p.Name, p.Namespace);
            }
        }

        /// <summary>
        /// Reads <see cref="PropertyValue"/> from database by executing SQL command.
        /// </summary>
        /// <param name="command">Command text.</param>
        /// <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value</param>
        /// <returns>List of <see cref="PropertyValue"/>.</returns>
        private async Task<IList<PropertyValue>> GetPropertyValuesAsync(string command, params object[] prms)
        {
            List<PropertyValue> l = new List<PropertyValue>();

            using (SqlDataReader reader = await Context.ExecuteReaderAsync(command, prms))            
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

        private async Task SetPropertyAsync(PropertyValue prop)
        {
            string selectCommand =
                @"SELECT Count(*) FROM [card_AddressbookFolderProperty]
                  WHERE [AddressbookFolderId] = @AddressbookFolderId AND [Name] = @Name AND [Namespace] = @Namespace";

            int count = await Context.ExecuteScalarAsync<int>(
                selectCommand,
                "@AddressbookFolderId"  , addressbookFolderId,
                "@Name"                 , prop.QualifiedName.Name,
                "@Namespace"            , prop.QualifiedName.Namespace);

            // insert
            if (count == 0)
            {
                string insertCommand = @"INSERT INTO [card_AddressbookFolderProperty] ([AddressbookFolderId], [Name], [Namespace], [PropVal])
                                          VALUES(@AddressbookFolderId, @Name, @Namespace, @PropVal)";

                await Context.ExecuteNonQueryAsync(
                    insertCommand,
                    "@PropVal"              , prop.Value,
                    "@AddressbookFolderId"  , addressbookFolderId,
                    "@Name"                 , prop.QualifiedName.Name,
                    "@Namespace"            , prop.QualifiedName.Namespace);
            }
            else
            {
                // update
                string command = @"UPDATE [card_AddressbookFolderProperty]
                      SET [PropVal] = @PropVal
                      WHERE [AddressbookFolderId] = @AddressbookFolderId AND [Name] = @Name AND [Namespace] = @Namespace";

                await Context.ExecuteNonQueryAsync(
                    command,
                    "@PropVal"              , prop.Value,
                    "@AddressbookFolderId"  , addressbookFolderId,
                    "@Name"                 , prop.QualifiedName.Name,
                    "@Namespace"            , prop.QualifiedName.Namespace);
            }
        }

        private async Task RemovePropertyAsync(string name, string ns)
        {
            string command = @"DELETE FROM [card_AddressbookFolderProperty]
                              WHERE [AddressbookFolderId] = @AddressbookFolderId
                              AND [Name] = @Name
                              AND [Namespace] = @Namespace";

            await Context.ExecuteNonQueryAsync(
                command,
                "@AddressbookFolderId"  , addressbookFolderId,
                "@Name"                 , name,
                "@Namespace"            , ns);
        }

        public Task SetOwnerAsync(IPrincipalAsync value)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Retrieves a particular principal as being the "owner" of the item. 
        /// </summary>
        /// <remarks>Required by OS X.</remarks>
        /// <returns>
        /// Item that represents owner of this item and implements <see cref="IPrincipalAsync"/>.
        /// </returns>
        public async Task<IPrincipalAsync> GetOwnerAsync()
        {
            DataRow rowOwner = rowsAccess.FirstOrDefault(x => x.Field<bool>("Owner") == true);
            if (rowOwner == null)
                return null;

            return await Acl.User.GetUserAsync(Context, rowOwner.Field<string>("UserId"));
        }

        /// <summary>
        /// Retrieves a particular principal as being the "group" of the item. This property is commonly
        /// found on repositories that implement the Unix privileges model.
        /// </summary>
        /// <param name="value">Identifies whether to search by owner or group.</param>
        public Task SetGroupAsync(IPrincipalAsync value)
        {
            throw new DavException("Group cannot be set", DavStatus.FORBIDDEN);
        }

        /// <summary>
        /// Retrieves a particular principal as being the "group" of the item. This property is commonly
        /// found on repositories that implement the Unix privileges model.
        /// </summary>
        /// <returns>
        /// Group principal that implements <see cref="IPrincipalAsync"/>.
        /// </returns>
        /// <remarks>
        /// Can return null if group is not assigned.
        /// </remarks>
        public async Task<IPrincipalAsync> GetGroupAsync()
        {
            return null; // Groups are not supported.
        }

        /// <summary>
        /// Retrieves list of all privileges (permissions) which can be set for the item.
        /// </summary>
        /// <returns>Enumerable with supported permissions.</returns>
        public async Task<IEnumerable<SupportedPrivilege>> GetSupportedPrivilegeSetAsync()
        {
            return new[] {
                new SupportedPrivilege
                {
                    Privilege = Privilege.Read, IsAbstract = false, DescriptionLanguage = "en",
                    Description = "Allows or denies the user the ability to read content and properties of files/folders."
                },
                new SupportedPrivilege
                {
                    Privilege = Privilege.Write, IsAbstract = false, DescriptionLanguage = "en",
                    Description = "Allows or denies locking an item or modifying the content, properties, or membership of a collection."
                }
            };
        }

        /// <summary>
        /// Retrieves the exact set of privileges (as computed by
        /// the server) granted to the currently authenticated HTTP user. Aggregate privileges and their contained
        /// privileges are listed.
        /// </summary>
        /// <returns>
        /// List of current user privileges.
        /// </returns>
        public async Task<IEnumerable<Privilege>> GetCurrentUserPrivilegeSetAsync()
        {
            DataRow rowAccess = rowsAccess.FirstOrDefault(x => x.Field<string>("UserId")== Context.UserId);
            if (rowAccess == null)
                return null;

            List<Privilege> privileges = new List<Privilege>();
            if (rowAccess.Field<bool>("Read"))
                privileges.Add(Privilege.Read);

            if (rowAccess.Field<bool>("Write"))
                privileges.Add(Privilege.Write);

            return privileges;
        }

        /// <summary>
        /// Retrieves access control list for this file or folder.
        /// </summary>
        /// <param name="propertyNames">Properties which will be retrieved from users/groups specified in
        /// access control list returned.</param>
        /// <returns>Enumerable with access control entries.</returns>
        public async Task<IEnumerable<ReadAce>> GetAclAsync(IList<PropertyName> propertyNames)
        {
            IList<ReadAce> aceList = new List<ReadAce>();
            foreach (DataRow rowAccess in rowsAccess)
            {
                ReadAce ace = new ReadAce();
                ace.Principal = await Acl.User.GetUserAsync(Context, rowAccess.Field<string>("UserId"));
                if (rowAccess.Field<bool>("Read"))
                    ace.GrantPrivileges.Add(Privilege.Read);
                if (rowAccess.Field<bool>("Write"))
                    ace.GrantPrivileges.Add(Privilege.Write);

                ace.IsProtected = rowAccess.Field<bool>("Owner");
                aceList.Add(ace);
            }
            return aceList;
        }

        public Task SetAclAsync(IList<WriteAce> aces)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Retrieves list of restrictions for access control entries.
        /// We don't support WebDAV inverted permissions.
        /// </summary>
        /// <returns>ACL restrictions.</returns>
        public async Task<AclRestriction> GetAclRestrictionsAsync()
        {
            return new AclRestriction { NoInvert = true, GrantOnly = true };
        }

        /// <summary>
        /// Gets all folders, from which this file/folder has inherited access control entries.
        /// </summary>
        /// <returns>Enumerable with files/folders from which this file/folder has inherited
        /// access control entries.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> GetInheritedAclSetAsync()
        {
            return new IHierarchyItemAsync[] { };
        }

        /// <summary>
        /// Gets collections which contain principals.
        /// </summary>
        /// <returns>Folders which contain users/groups.</returns>
        public async Task<IEnumerable<IPrincipalFolderAsync>> GetPrincipalCollectionSetAsync()
        {
            return new IPrincipalFolderAsync[] { new Acl.UsersFolder(Context) };
        }

        /// <summary>
        /// Retrieves user or group which correspond to a well known principal
        /// (defined in <see cref="WellKnownPrincipal"/>.)
        /// </summary>
        /// <param name="wellKnownPrincipal">Well known principal type.</param>
        /// <returns>Instance of corresponding user/group or <c>null</c> if corresponding user/group
        /// is not supported.</returns>
        public async Task<IPrincipalAsync> ResolveWellKnownPrincipalAsync(WellKnownPrincipal wellKnownPrincipal)
        {
            return null;
        }

        public Task<IEnumerable<IAclHierarchyItemAsync>> GetItemsByPropertyAsync(MatchBy matchBy, IList<PropertyName> props)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }
    }
}
