using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;
using ITHit.WebDAV.Server.Acl;

using CalDAVServer.FileSystemStorage.AspNet.Acl;

namespace CalDAVServer.FileSystemStorage.AspNet.CalDav
{
    /// <summary>
    /// Represents CalDAV calendar (calendar folder).
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/[user_name]/[calendar_name]/
    /// </summary>
    /// <remarks>Mozilla Thunderbird Lightning requires ICurrentUserPrincipalAsync on calendar folder, it does not support discovery.</remarks>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- calendars
    ///      |-- ...
    ///      |-- [User2]
    ///           |-- [Calendar 1]  -- this class
    ///           |-- ...
    ///           |-- [Calendar X]  -- this class
    /// </example>
    /// <remarks>
    /// IAclHierarchyItemAsync is required by OS X Calendar.
    /// </remarks>
    public class CalendarFolder : DavFolder, ICalendarFolderAsync, IAppleCalendarAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync
    {
        /// <summary>
        /// Returns calendar folder that corresponds to path.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>CalendarFolder instance or null if not found.</returns>
        public static CalendarFolder GetCalendarFolder(DavContext context, string path)
        {
            string pattern = string.Format("^/?{0}/(?<user_name>[^/]+)/(?<calendar_name>[^/]+)/?",
                                           CalendarsRootFolder.CalendarsRootFolderPath.Trim(new char[] { '/' }).Replace("/", "/?"));
            if (!Regex.IsMatch(path, pattern))
                return null;

            string folderPath = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            // to block vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'
            if (!folder.Exists || String.Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) != 0)
                return null;

            return new CalendarFolder(folder, context, path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarFolder"/> class.
        /// </summary>
        /// <param name="directoryInfo">Instance of <see cref="DirectoryInfo"/> class with information about the folder in file system.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <param name="path">Relative to WebDAV root folder path.</param>
        private CalendarFolder(DirectoryInfo directoryInfo, DavContext context, string path)
            : base(directoryInfo, context, path)
        {
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
            // Here you can load all items from pathList in one request to your storage, instead of 
            // getting items one-by-one using GetHierarchyItem call.

            IList<ICalendarFileAsync> calendarFileList = new List<ICalendarFileAsync>();
            foreach (string path in pathList)
            {
                ICalendarFileAsync calendarFile = await context.GetHierarchyItemAsync(path) as ICalendarFileAsync;
                calendarFileList.Add(calendarFile);
            }
            return calendarFileList;
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
            return (await GetChildrenAsync(propNames.ToList(), null, null, null)).Page.Cast<ICalendarFileAsync>();
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
            get
            {
                return string.Format("Some {0} description.", Name);
            }
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
            return null;
            /*
            return new[]
            {
                new SharingInvite
                { 
                    Address = "email: john.doe@caldavserver.com",
                    Access = SharingInviteAccess.ReadWrite,
                    CommonName = "John Doe",
                    Status = SharingInviteStatus.Noresponse
                },

                new SharingInvite
                { 
                    Address = "email: michael.marshall@caldavserver.com",
                    Access = SharingInviteAccess.Read,
                    CommonName = "Michael Marshall",
                    Status = SharingInviteStatus.Accepted
                }
            };
            */
        }

        /// <summary>
        /// Indicates that the calendar is shared and if it is shared by the current user who is the owner of the calendar.
        /// </summary>
        public async Task<CalendarSharedBy> GetSharedByAsync()
        {
            return CalendarSharedBy.NotShared;

            /*
            if(//calendar is not shared)
            {
                return CalendarSharedBy.NotShared;
            }
                
            IPrincipalAsync principal = await this.GetCurrentUserPrincipalAsync();
            IPrincipalAsync owner = await this.GetOwnerAsync();
            if(owner.Name.Equals(principal.Name))
            {
                return CalendarSharedBy.SharedByOwner;                
            }
            else
            {
                return CalendarSharedBy.Shared;
            }
            */
        }

        public Task SetOwnerAsync(IPrincipalAsync value)
        {
            throw new NotImplementedException();
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
            return context.FileOperation(
                this,
                () =>
                {
                    FileSecurity acl = File.GetAccessControl(fileSystemInfo.FullName);
                    return AclFactory.GetPrincipalFromSid(acl.GetOwner(typeof(SecurityIdentifier)).Value, context);
                },
                Privilege.Read);
        }

        public Task SetGroupAsync(IPrincipalAsync value)
        {
            throw new NotImplementedException();
        }

        public Task<IPrincipalAsync> GetGroupAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<SupportedPrivilege>> GetSupportedPrivilegeSetAsync()
        {
            throw new NotImplementedException();
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
            return new[] { Privilege.Write, Privilege.Read };
        }

        public Task<IEnumerable<ReadAce>> GetAclAsync(IList<PropertyName> propertyNames)
        {
            throw new NotImplementedException();
        }

        public Task SetAclAsync(IList<WriteAce> aces)
        {
            throw new NotImplementedException();
        }

        public Task<AclRestriction> GetAclRestrictionsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHierarchyItemAsync>> GetInheritedAclSetAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IPrincipalFolderAsync>> GetPrincipalCollectionSetAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IPrincipalAsync> ResolveWellKnownPrincipalAsync(WellKnownPrincipal wellKnownPrincipal)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IAclHierarchyItemAsync>> GetItemsByPropertyAsync(MatchBy matchBy, IList<PropertyName> props)
        {
            throw new NotImplementedException();
        }
    }
}
