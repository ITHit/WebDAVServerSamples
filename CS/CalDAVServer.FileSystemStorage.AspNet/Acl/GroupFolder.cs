using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;

namespace CalDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Logical folder which contains windows groups and is located
    /// at '/acl/groups' path.
    /// </summary>
    public class GroupFolder : LogicalFolder, IPrincipalFolderAsync
    {
        /// <summary>
        /// Path to folder which contains groups.
        /// </summary>
        public static string PREFIX = AclFolder.PREFIX + "/groups";

        /// <summary>
        /// Gets path of folder which contains groups.
        /// </summary>
        public static string PATH
        {
            get { return PREFIX + "/"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupFolder"/> class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        public GroupFolder(DavContext context) : base(context, "groups", PATH)
        {
        }

        /// <summary>
        /// Retrieves list of windows groups.
        /// </summary>
        /// <param name="properties">Properties which will be requested from the item by the engine later.</param>
        /// <returns>Enumerable with groups.</returns>
        public override async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> properties)
        {
            return Context.PrincipalOperation<IEnumerable<IHierarchyItemAsync>>(getGroups);
        }

        /// <summary>
        /// Required by interface. However we don't allow creating folders inside this folder.
        /// </summary>
        /// <param name="name">New folder name.</param>
        /// <returns>New folder.</returns>
        public async Task<IPrincipalFolderAsync> CreateFolderAsync(string name)
        {
            //Creating folders inside this folder is not supported.
            throw new DavException("Creating folders is not implemented", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Creates group with specified name.
        /// </summary>
        /// <param name="name">Group name.</param>
        /// <returns>Newly created group.</returns>
        public async Task<IPrincipalAsync> CreatePrincipalAsync(string name)
        {
            if (!PrincipalBase.IsValidUserName(name))
            {
                throw new DavException("Group name contains invalid characters", DavStatus.FORBIDDEN);
            }

            GroupPrincipal groupPrincipal = new GroupPrincipal(Context.GetPrincipalContext());

            groupPrincipal.Name = name;

            groupPrincipal.Save();

            return new Group(groupPrincipal, Context);
        }

        /// <summary>
        /// Finds groups which have matching property values.
        /// </summary>
        /// <param name="propValues">Property values which group must have.</param>
        /// <param name="props">Properties that will be retrieved later for the group found.</param>
        /// <returns>Groups which have matching property.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> FindPrincipalsByPropertyValuesAsync(
            IList<PropertyValue> propValues,
            IList<PropertyName> props)
        {
            GroupPrincipal group = new GroupPrincipal(Context.GetPrincipalContext());
            group.Name = "*";
            foreach (PropertyValue v in propValues)
            {
                if (v.QualifiedName == PropertyName.DISPLAYNAME)
                {
                    group.Name = "*" + v.Value + "*";
                }
            }

            PrincipalSearcher searcher = new PrincipalSearcher(group);
            return searcher.FindAll().Select(u => new Group((GroupPrincipal)u, Context)).Cast<IPrincipalAsync>();
        }

        /// <summary>
        /// Returns properties which can be used in <see cref="FindPrincipalsByPropertyValuesAsync"/> method.
        /// </summary>
        /// <returns>List of property description.</returns>
        public async Task<IEnumerable<PropertyDescription>> GetPrincipalSearcheablePropertiesAsync()
        {
            return new[]{ new PropertyDescription
                             {
                                 Name = PropertyName.DISPLAYNAME,
                                 Description = "Principal name",
                                 Lang = "en"
                             } };
        }

        /// <summary>
        /// Returns all groups current user is member of.
        /// </summary>
        /// <param name="props">Properties that will be asked later from the groups returned.</param>
        /// <returns>Enumerable with groups.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> GetMatchingPrincipalsAsync(IList<PropertyName> props)
        {
            User user = User.FromName(Context.WindowsIdentity.Name, Context);
            return await user.GetGroupMembershipAsync();
        }

        /// <summary>
        /// Retrieves all groups in computer/domain.
        /// </summary>
        /// <returns>Enumerable with groups.</returns>
        private IEnumerable<IHierarchyItemAsync> getGroups()
        {
            GroupPrincipal insGroupPrincipal = new GroupPrincipal(Context.GetPrincipalContext());

            insGroupPrincipal.Name = "*";
            PrincipalSearcher insPrincipalSearcher = new PrincipalSearcher(insGroupPrincipal);

            PrincipalSearchResult<Principal> r = insPrincipalSearcher.FindAll();

            return r.Select(g => new Group((GroupPrincipal)g, Context)).Cast<IHierarchyItemAsync>().ToList();
        }
    }
}
