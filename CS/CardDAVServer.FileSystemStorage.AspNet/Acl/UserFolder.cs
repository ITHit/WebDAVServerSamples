using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Paging;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Logical folder which contains users.
    /// It has path '/acl/users'
    /// </summary>
    public class UserFolder : LogicalFolder, IPrincipalFolderAsync
    {
        /// <summary>
        /// Path to folder which contains users.
        /// </summary>
        public static string PREFIX = AclFolder.PREFIX + "/users";

        /// <summary>
        /// Gets path of folder which contains users.
        /// </summary>
        public static string PATH
        {
            get { return PREFIX + "/"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFolder"/> class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        public UserFolder(DavContext context) : base(context, "users", PATH)
        {
        }

        /// <summary>
        /// Retrieves users.
        /// </summary>
        /// <param name="properties">List of properties which will be retrieved by the engine later.</param>      
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Enumerable with users and a total number of users.</returns>
        public override async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            return new PageResults(Context.PrincipalOperation<IEnumerable<IHierarchyItemAsync>>(getUsers), null);
        }

        /// <summary>
        /// Retrieves all users in computer/domain.
        /// </summary>
        /// <returns>Enumerable with users.</returns>
        private IEnumerable<IHierarchyItemAsync> getUsers()
        {
            UserPrincipal insUserPrincipal = new UserPrincipal(Context.GetPrincipalContext());
            insUserPrincipal.Name = "*";
            PrincipalSearcher insPrincipalSearcher = new PrincipalSearcher(insUserPrincipal);

            return insPrincipalSearcher.FindAll().Select(
                u => new User((UserPrincipal)u, Context)).Cast<IHierarchyItemAsync>().ToList();
        }

        /// <summary>
        /// We don't support creating folders inside this folder.
        /// </summary>        
        public async Task<IPrincipalFolderAsync> CreateFolderAsync(string name)
        {
            throw new DavException("Creating folders is not implemented", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Creates user.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <returns>Newly created user.</returns>
        public async Task<IPrincipalAsync> CreatePrincipalAsync(string name)
        {
            if (!PrincipalBase.IsValidUserName(name))
            {
                throw new DavException("User name contains invalid characters", DavStatus.FORBIDDEN);
            }

            UserPrincipal userPrincipal = new UserPrincipal(Context.GetPrincipalContext());

            userPrincipal.Name = name;
            userPrincipal.UserPrincipalName = name;

            userPrincipal.Enabled = true;
            userPrincipal.ExpirePasswordNow();

            Context.PrincipalOperation(userPrincipal.Save);
            
            return new User(userPrincipal, Context);
        }

        /// <summary>
        /// Finds users whose properties have certain values.
        /// </summary>
        /// <param name="propValues">Properties and values to look for.</param>
        /// <param name="props">Properties that will be requested by the engine from the returned users.</param>
        /// <returns>Enumerable with users whose properties match.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> FindPrincipalsByPropertyValuesAsync(
            IList<PropertyValue> propValues,
            IList<PropertyName> props)
        {
            UserPrincipal user = new UserPrincipal(Context.GetPrincipalContext());
            user.Name = "*";
            foreach (PropertyValue v in propValues)
            {
                if (v.QualifiedName == PropertyName.DISPLAYNAME)
                {
                    user.Name = "*" + v.Value + "*";
                }
            }

            PrincipalSearcher searcher = new PrincipalSearcher(user);
            return searcher.FindAll().Select(u => new User((UserPrincipal)u, Context)).Cast<IPrincipalAsync>();
        }

        /// <summary>
        /// Returns list of properties which can be used in <see cref="FindPrincipalsByPropertyValuesAsync"/>.
        /// </summary>
        /// <returns></returns>
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
        /// Returns <see cref="IPrincipalAsync"/> for the current user.
        /// </summary>
        /// <param name="props">Properties that will be asked later from the user returned.</param>
        /// <returns>Enumerable with users.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> GetMatchingPrincipalsAsync(IList<PropertyName> props)
        {
            return new[]{ User.FromName(Context.WindowsIdentity.Name, Context) };
        }
    }
}
