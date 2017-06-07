using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;
using ITHit.WebDAV.Server.Acl;

using CardDAVServer.FileSystemStorage.AspNet.Acl;

namespace CardDAVServer.FileSystemStorage.AspNet.CardDav
{
    /// <summary>
    /// Represents CrdDAV address book (address book folder).
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[user_name]/[addressbook_name]/
    /// </summary>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- addressbooks
    ///      |-- ...
    ///      |-- [User2]
    ///           |-- [Address Book 1]  -- this class
    ///           |-- ...
    ///           |-- [Address Book X]  -- this class
    /// </example>
    /// <remarks>
    /// IAclHierarchyItemAsync is required by OS X Contacts.
    /// </remarks>
    public class AddressbookFolder : DavFolder, IAddressbookFolderAsync, IAclHierarchyItemAsync
    {
        /// <summary>
        /// Returns address book folder that corresponds to path.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>AddressbookFolder instance or null if not found.</returns>
        public static AddressbookFolder GetAddressbookFolder(DavContext context, string path)
        {
            string pattern = string.Format("^/?{0}/(?<user_name>[^/]+)/(?<addressbook_name>[^/]+)/?",
                                           AddressbooksRootFolder.AddressbooksRootFolderPath.Trim(new char[] { '/' }).Replace("/", "/?"));
            if (!Regex.IsMatch(path, pattern))
                return null;

            string folderPath = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            // to block vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'
            if (!folder.Exists || String.Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) != 0)
                return null;

            return new AddressbookFolder(folder, context, path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressbookFolder"/> class.
        /// </summary>
        /// <param name="directoryInfo">Instance of <see cref="DirectoryInfo"/> class with information about the folder in file system.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <param name="path">Relative to WebDAV root folder path.</param>
        private AddressbookFolder(DirectoryInfo directoryInfo, DavContext context, string path)
            : base(directoryInfo, context, path)
        {
        }

        public string AddressbookDescription 
        { 
            get 
            {
                return string.Format("Some {0} description.", Name);
            }
        }

        /// <summary>
        /// Returns a list of business card files that correspont to the specified list of item paths.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called by the Engine during <b>addressbook-multiget</b> call.
        /// </para>
        /// <para>
        /// For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        /// </para>
        /// </remarks>
        /// <param name="pathList">Business card files path list.</param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of business card files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICardFileAsync>> MultiGetAsync(IEnumerable<string> pathList, IEnumerable<PropertyName> propNames)
        {
            // Here you can load all items from pathList in one request to your storage, instead of 
            // getting items one-by-one using GetHierarchyItem call.

            IList<ICardFileAsync> cardFileList = new List<ICardFileAsync>();
            foreach(string path in pathList)
            {
                ICardFileAsync cardFile = await context.GetHierarchyItemAsync(path) as ICardFileAsync;
                cardFileList.Add(cardFile);
            }
            return cardFileList;
        }

        /// <summary>
        /// Returns a list of business card files that match specified filter. 
        /// </summary>
        /// <remarks>
        /// <param name="rawQuery">
        /// Raw query sent by the client.
        /// </param>
        /// <param name="propNames">
        /// Properties requested by the client. You can use this as a hint about what properties will be called by 
        /// the Engine for each item that are returned from this method.
        /// </param>
        /// <returns>List of  business card files. Returns <b>null</b> for any item that is not found.</returns>
        public async Task<IEnumerable<ICardFileAsync>> QueryAsync(string rawQuery, IEnumerable<PropertyName> propNames)
        {
            // For the sake of simplicity we just call GetChildren returning all items. 
            // Typically you will return only items that match the query.
            return (await GetChildrenAsync(propNames.ToList())).Cast<ICardFileAsync>();
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