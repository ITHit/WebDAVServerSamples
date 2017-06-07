using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Linq;
using System.Data;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;


namespace CalDAVServer.SqlStorage.AspNet.CalDav
{
    // Note:
    //  - Mozilla Thunderbird Lightning requires ICurrentUserPrincipalAsync on calendar folder, it does not support discovery.
    //  - Outlook CalDAV Synchronizer requires IAclHierarchyItemAsync on calendar folder.

    /// <summary>
    /// Represents a CalDAV calendar (calendar folder).
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/[CalendarFolderId]
    /// </summary>
    public class CalendarFolder : DavHierarchyItem, ICalendarFolderAsync, IAppleCalendarAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync
    {
        /// <summary>
        /// Loads calendar folder by ID. Returns null if calendar folder was not found.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="calendarFolderId">ID of the calendar folder to load.</param>
        /// <returns><see cref="ICalendarFolderAsync"/> instance.</returns>
        public static async Task<ICalendarFolderAsync> LoadByIdAsync(DavContext context, Guid calendarFolderId)
        {
            // Load only calendar that the use has access to. 
            // Also load complete ACL for this calendar.
            string sql =
                @"SELECT * FROM [cal_CalendarFolder] 
                  WHERE [CalendarFolderId] = @CalendarFolderId
                  AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [cal_Access]
                  WHERE [CalendarFolderId] = @CalendarFolderId
                  AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)";

            return (await LoadAsync(context, sql,
                  "@UserId", context.UserId
                , "@CalendarFolderId", calendarFolderId
                )).FirstOrDefault();
        }

        /// <summary>
        /// Loads all calendars.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>List of <see cref="ICalendarFolderAsync"/> items.</returns>
        public static async Task<IEnumerable<ICalendarFolderAsync>> LoadAllAsync(DavContext context)
        {
            // Load only calendars that the use has access to. 
            // Also load complete ACL for each calendar, but only if user has access to that calendar.
            string sql =
                @"SELECT * FROM [cal_CalendarFolder] 
                  WHERE [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [cal_Access] 
                  WHERE [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)";

            return await LoadAsync(context, sql, "@UserId", context.UserId);
        }

        /// <summary>
        /// Loads calendars by SQL.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="sql">SQL that queries [cal_CalendarFolder] table.</param>
        /// <param name="prms">List of SQL parameters.</param>
        /// <returns>List of <see cref="ICalendarFolderAsync"/> items.</returns>
        private static async Task<IEnumerable<ICalendarFolderAsync>> LoadAsync(DavContext context, string sql, params object[] prms)
        {
            IList<ICalendarFolderAsync> calendarFolders = new List<ICalendarFolderAsync>();

            using (SqlDataReader reader = await context.ExecuteReaderAsync(sql, prms))
            {
                DataTable calendars = new DataTable();
                calendars.Load(reader);

                DataTable access = new DataTable();
                access.Load(reader);

                foreach (DataRow rowCalendarFolder in calendars.Rows)
                {
                    Guid calendarFolderId = rowCalendarFolder.Field<Guid>("CalendarFolderId");

                    string filter = string.Format("CalendarFolderId = '{0}'", calendarFolderId);
                    DataRow[] rowsAccess = access.Select(filter);

                    calendarFolders.Add(new CalendarFolder(context, calendarFolderId, rowCalendarFolder, rowsAccess));
                }
            }

            return calendarFolders;
        }

        /// <summary>
        /// Creates a new calendar folder.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param> 
        /// <param name="name">Calendar folder name.</param>
        public static async Task CreateCalendarFolderAsync(DavContext context, string name, string description)
        {
            // 1. Create calendar.
            // 2. Grant owner privileges to the user on the created calendar.
            string sql = @"INSERT INTO [cal_CalendarFolder] (
                          [CalendarFolderId]
                        , [Name]
                        , [Description]
                    ) VALUES (
                          @CalendarFolderId
                        , @Name
                        , @Description
                    )
                    ; INSERT INTO [cal_Access] (
                          [CalendarFolderId]
                        , [UserId]
                        , [Owner]
                        , [Read]
                        , [Write]
                    ) VALUES (
                          @CalendarFolderId
                        , @UserId
                        , @Owner
                        , @Read
                        , @Write
                    )";

            Guid calendarFolderId = Guid.NewGuid();

            await context.ExecuteNonQueryAsync(sql,
                  "@CalendarFolderId"   , calendarFolderId
                , "@Name"               , name
                , "@Description"        , description
                , "@UserId"             , context.UserId
                , "@Owner"              , true
                , "@Read"               , true
                , "@Write"              , true
                );
        }

        /// <summary>
        /// This calendar folder ID.
        /// </summary>
        private readonly Guid calendarFolderId;

        /// <summary>
        /// Contains data from [cal_CalendarFolder] table.
        /// </summary>
        private readonly DataRow rowCalendarFolder;

        /// <summary>
        /// Contains data from [card_Access] table.
        /// </summary>
        private readonly DataRow[] rowsAccess;

        /// <summary>
        /// Gets display name of the calendar.
        /// </summary>
        /// <remarks>CalDAV clients typically never request this property.</remarks>
        public override string Name
        {
            get { return rowCalendarFolder != null ? rowCalendarFolder.Field<string>("Name") : null; }
        }

        /// <summary>
        /// Gets item path.
        /// </summary>
        public override string Path
        {
            get
            {
                return string.Format("{0}{1}/", CalendarsRootFolder.CalendarsRootFolderPath, calendarFolderId);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarFolder"/> class from database source.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="calendarFolderId">Calendar folder ID.</param>
        /// <param name="calendar">Contains data from [cal_CalendarFolder] table.</param>
        /// <param name="rowsAccess">Contains data from [cal_Access] table for this calendar.</param>
        private CalendarFolder(DavContext context, Guid calendarFolderId, DataRow calendar, DataRow[] rowsAccess)
            : base(context)
        {
            this.calendarFolderId = calendarFolderId;
            this.rowCalendarFolder = calendar;
            this.rowsAccess = rowsAccess;
        }

        /// <summary>
        /// Returns a list of calendar files that correspont to the specified list of item paths.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by the Engine during <b>calendar-multiget</b> call.
        /// </para>
        /// <para>
        /// For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        /// </para>
        /// </remarks>
        /// <param name="pathList">Calendar files path list.</param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of calendar files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICalendarFileAsync>> MultiGetAsync(IEnumerable<string> pathList, IEnumerable<PropertyName> propNames)
        {
            // Get list of UIDs from path list.
            IEnumerable<string> uids = pathList.Select(a => System.IO.Path.GetFileNameWithoutExtension(a));

            return await CalendarFile.LoadByUidsAsync(Context, uids, PropsToLoad.All);
        }

        /// <summary>
        /// Returns a list of calendar files that match specified filter. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by the Engine during <b>calendar-query</b> call.
        /// </para>
        /// </remarks>
        /// <param name="rawQuery">
        /// Raw query sent by the client.
        /// </param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of calendar files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICalendarFileAsync>> QueryAsync(string rawQuery, IEnumerable<PropertyName> propNames)
        {
            // For the sake of simplicity we just call GetChildren returning all items. 
            // Typically you will return only items that match the query.
            return (await GetChildrenAsync(propNames.ToList())).Cast<ICalendarFileAsync>();
        }

        /// <summary>
        /// Specifies the calendar component types (e.g., VEVENT, VTODO, etc.) 
        /// that calendar object resources can contain in the calendar collection.
        /// </summary>
        public IEnumerable<CalendarComponentType> SupportedComponentTypes
        {
            get
            {
                return new[]
                    {
                        CalendarComponentType.VEVENT,
                        CalendarComponentType.VTODO,
                    };
            }
        }

        /// <summary>
        /// Provides a human-readable description of the calendar collection.
        /// </summary>
        public string CalendarDescription 
        {
            get { return rowCalendarFolder.Field<string>("Description"); }
        }

        /// <summary>
        /// Gets a numeric value indicating the maximum size of a
        /// resource in bytes that the server is willing to accept when a
        /// calendar object resource is stored in a calendar collection.
        /// More details at http://tools.ietf.org/html/rfc4791#section-5.2.5
        /// </summary>
        public ulong MaxResourceSize
        {
            get { return ulong.MaxValue; }
        }

        /// <summary>
        /// Gets a numeric value indicating the maximum number of
        /// recurrence instances that a calendar object resource stored in a
        /// calendar collection can generate.
        /// More details at http://tools.ietf.org/html/rfc4791#section-5.2.8
        /// </summary>
        public ulong MaxInstances
        {
            get { return ulong.MaxValue; }
        }

        /// <summary>
        /// Provides a numeric value indicating the maximum number of
        /// ATTENDEE properties in any instance of a calendar object resource
        /// stored in a calendar collection.
        /// More details at http://tools.ietf.org/html/rfc4791#section-5.2.9
        /// </summary>
        public ulong MaxAttendeesPerInstance
        {
            get { return ulong.MaxValue; }
        }

        /// <summary>
        /// Gets a DATE-TIME value indicating the earliest date and
        /// time (in UTC) that the server is willing to accept for any DATE or
        /// DATE-TIME value in a calendar object resource stored in a calendar
        /// collection.
        /// More details at http://tools.ietf.org/html/rfc4791#section-5.2.6
        /// </summary>
        public DateTime UtcMinDateTime
        {
            get { return DateTime.MinValue.ToUniversalTime(); }
        }

        /// <summary>
        /// Gets a DATE-TIME value indicating the latest date and
        /// time (in UTC) that the server is willing to accept for any DATE or
        /// DATE-TIME value in a calendar object resource stored in a calendar
        /// collection.
        /// More details at http://tools.ietf.org/html/rfc4791#section-5.2.7
        /// </summary>
        public DateTime UtcMaxDateTime
        {
            get { return DateTime.MaxValue.ToUniversalTime(); }
        }

        /// <summary>
        /// Retrieves children of this folder.
        /// </summary>
        /// <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        /// <returns>Children of the folder.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {
            // Here we enumerate all events and to-dos contained in this calendar.
            // You can filter children items in this implementation and 
            // return only items that you want to be available for this 
            // particular user.

            // Typically only getcontenttype and getetag properties are requested in GetChildren call by CalDAV/CardDAV clients.
            // The iCalendar/vCard (calendar-data/address-data) is typically requested not in GetChildren, but in a separate multiget 
            // report, in MultiGetAsync, that follow this request.

            // Bynari submits PROPFIND without props - Engine will request getcontentlength

            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();
            return await CalendarFile.LoadByCalendarFolderIdAsync(Context, calendarFolderId, PropsToLoad.Minimum);
        }

        /// <summary>
        /// Creates a file that contains event or to-do item in this calendar.
        /// </summary>
        /// <param name="name">Name of the file. Same as event/to-do UID but ending with '.ics'.</param>
        /// <returns>The newly created file.</returns>
        /// <remarks></remarks>
        public async Task<IFileAsync> CreateFileAsync(string name)
        {
            // The actual event or to-do object is created in datatbase in CardFile.Write call.
            return CalendarFile.CreateCalendarFile(Context, calendarFolderId);
        }

        /// <summary>
        /// Creating new folders is not allowed in calendar folders.
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
            // Here we support only calendars renaming. Check that user has permissions to write.
            string sql = @"UPDATE [cal_CalendarFolder] SET Name=@Name
                WHERE [CalendarFolderId]=@CalendarFolderId
                AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId] = @UserId AND [Write] = 1)";

            if (await Context.ExecuteNonQueryAsync(sql,
                  "@UserId"             , Context.UserId
                , "@CalendarFolderId"   , calendarFolderId
                , "@Name"               , destName) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }
        }

        /// <summary>
        /// Deletes this calendar.
        /// </summary>
        /// <param name="multistatus"><see cref="MultistatusException"/> to populate with child files and folders failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            // Delete calendar and all events / to-dos associated with it. Check that user has permissions to delete.
            string sql = @"DELETE FROM [cal_CalendarFolder] 
                WHERE [CalendarFolderId]=@CalendarFolderId
                AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId] = @UserId AND [Owner] = 1)";

            if (await Context.ExecuteNonQueryAsync(sql,
                  "@UserId"             , Context.UserId
                , "@CalendarFolderId"   , calendarFolderId) < 1)
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
                    "SELECT [Name], [Namespace], [PropVal] FROM [cal_CalendarFolderProperty] WHERE [CalendarFolderId] = @CalendarFolderId",
                    "@CalendarFolderId", calendarFolderId);

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
                @"SELECT Count(*) FROM [cal_CalendarFolderProperty]
                  WHERE [CalendarFolderId] = @CalendarFolderId AND [Name] = @Name AND [Namespace] = @Namespace";

            int count = await Context.ExecuteScalarAsync<int>(
                selectCommand,
                "@CalendarFolderId" , calendarFolderId,
                "@Name"             , prop.QualifiedName.Name,
                "@Namespace"        , prop.QualifiedName.Namespace);

            // insert
            if (count == 0)
            {
                string insertCommand = @"INSERT INTO [cal_CalendarFolderProperty] ([CalendarFolderId], [Name], [Namespace], [PropVal])
                                          VALUES(@CalendarFolderId, @Name, @Namespace, @PropVal)";

                await Context.ExecuteNonQueryAsync(
                    insertCommand,
                    "@PropVal"          , prop.Value,
                    "@CalendarFolderId" , calendarFolderId,
                    "@Name"             , prop.QualifiedName.Name,
                    "@Namespace"        , prop.QualifiedName.Namespace);
            }
            else
            {
                // update
                string command = @"UPDATE [cal_CalendarFolderProperty]
                      SET [PropVal] = @PropVal
                      WHERE [CalendarFolderId] = @CalendarFolderId AND [Name] = @Name AND [Namespace] = @Namespace";

                await Context.ExecuteNonQueryAsync(
                    command,
                    "@PropVal"          , prop.Value,
                    "@CalendarFolderId" , calendarFolderId,
                    "@Name"             , prop.QualifiedName.Name,
                    "@Namespace"        , prop.QualifiedName.Namespace);
            }
        }

        private async Task RemovePropertyAsync(string name, string ns)
        {
            string command = @"DELETE FROM [cal_CalendarFolderProperty]
                              WHERE [CalendarFolderId] = @CalendarFolderId
                              AND [Name] = @Name
                              AND [Namespace] = @Namespace";

            await Context.ExecuteNonQueryAsync(
                command,
                "@CalendarFolderId" , calendarFolderId,
                "@Name"             , name,
                "@Namespace"        , ns);
        }


        /// <summary>
        /// Indicates which sharing or publishing capabilities are supported 
        /// by this calendar collection.
        /// </summary>
        public IEnumerable<AppleAllowedSharingMode> AllowedSharingModes
        {
            get
            {
                return new[]
                    {
                        AppleAllowedSharingMode.CanBePublished,
                        AppleAllowedSharingMode.CanBeShared,
                    };
            }
        }

        /// <summary>
        /// This metod is called when user is granting or 
        /// withdrowing acces to the calendar. 
        /// </summary>
        /// <remarks>
        /// In this metod implementation you will grant 
        /// or withdraw acces to the calendar as well as you will send sharing invitation.
        /// </remarks>
        /// <param name="sharesToAddAndRemove">Each item in this list describes the share to 
        /// add or delete.</param>
        public async Task UpdateSharingAsync(IList<AppleShare> sharesToAddAndRemove)
        {
            // Drop all shares first regardless of operation order. When resending 
            // invitations Apple Calendar drops and adds shares for the user in one \
            // request.
            foreach (AppleShare share in sharesToAddAndRemove)
            {
                if (share.Operation == AppleSharingOperation.Withdraw)
                {
                    // remove sharing here
                    // share.Address
                    // share.CommonName
                }
            }

            // Add new shares
            foreach (AppleShare share in sharesToAddAndRemove)
            {
                if (share.Operation != AppleSharingOperation.Withdraw)
                {
                    // enable sharing and send invitation here
                    // share.Address
                    // share.CommonName
                }
            }
        }

        /// <summary>
        /// Provides a list of users to whom the calendar has been shared.
        /// </summary>
        /// <remarks>
        /// http://svn.calendarserver.org/repository/calendarserver/CalendarServer/trunk/doc/Extensions/caldav-sharing.txt
        /// (Section 5.2.2)        
        public async Task<IEnumerable<SharingInvite>> GetInviteAsync()
        {

            IList<SharingInvite> invites = new List<SharingInvite>();

            foreach (DataRow rowAccess in rowsAccess)
            {
                if (rowAccess.Field<bool>("Owner"))
                    continue;

                string userId = rowAccess.Field<string>("UserId");
                System.Web.Security.MembershipUser user = System.Web.Security.Membership.GetUser(userId);

                SharingInvite ace = new SharingInvite
                {
                      Address       = string.Format("email:{0}", user.Email)
                    , Access        = rowAccess.Field<bool>("Write") ? SharingInviteAccess.ReadWrite : SharingInviteAccess.Read
                    , CommonName    = user.UserName
                    , Status        = SharingInviteStatus.Accepted
                };
            }

            return invites;
        }

        /// <summary>
        /// Indicates that the calendar is shared and if it is shared by the current user who is the owner of the calendar.
        /// </summary>
        public async Task<CalendarSharedBy> GetSharedByAsync()
        {
            if (rowsAccess.Any(x => !x.Field<bool>("Owner")))
            {
                return CalendarSharedBy.NotShared;
            }

            string ownerId = rowsAccess.First(x => x.Field<bool>("Owner")).Field<string>("UserId");
            if (ownerId.Equals(Context.UserId, StringComparison.InvariantCultureIgnoreCase))
            {
                return CalendarSharedBy.SharedByOwner;
            }
            else
            {
                return CalendarSharedBy.Shared;
            }
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
            DataRow rowAccess = rowsAccess.FirstOrDefault(x => x.Field<string>("UserId") == Context.UserId);
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
