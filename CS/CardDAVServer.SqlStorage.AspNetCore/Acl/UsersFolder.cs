using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Paging;

namespace CardDAVServer.SqlStorage.AspNetCore.Acl
{
    /// <summary>
    /// Logical folder which contains users.
    /// Instances of this class correspond to the following path: [DAVLocation]/acl/users/.
    /// </summary>
    public class UsersFolder : LogicalFolder, IPrincipalFolderAsync
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string usersFolderName = "users";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static readonly string UsersFolderPath = string.Format("{0}{1}/", AclFolder.AclFolderPath, usersFolderName);

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersFolder"/> class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        public UsersFolder(DavContext context) : base(context, UsersFolderPath)
        {
        }

        /// <summary>
        /// Retrieves users.
        /// </summary>
        /// <param name="propNames">Properties requested by client application for each child.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Children of this folder - list of user principals.</returns>
        public override async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            // Here you will list users from OWIN Identity or from membership provider, 
            // you can replace it with your own users source.

            // In this implementation we return only one user - current user, for demo purposes.
            // We also do not populate user e-mail to avoid any queries to back-end storage.

            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();
            children.Add(new User(Context, Context.UserId, Context.Identity.Name, null, new DateTime(2000, 1, 1), new DateTime(2000, 1, 1)));

            return new PageResults(children, null);
        }

        /// <summary>
        /// We don't support creating folders inside this folder.
        /// </summary>        
        public async Task<IPrincipalFolderAsync> CreateFolderAsync(string name)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Creates user.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <returns>Newly created user.</returns>
        public async Task<IPrincipalAsync> CreatePrincipalAsync(string name)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
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
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Returns list of properties which can be used in <see cref="FindPrincipalsByPropertyValues"/>.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<PropertyDescription>> GetPrincipalSearcheablePropertiesAsync()
        {
            return new PropertyDescription[0];
        }

        /// <summary>
        /// Returns <see cref="IPrincipal"/> for the current user.
        /// </summary>
        /// <param name="props">Properties that will be asked later from the user returned.</param>
        /// <returns>Enumerable with users.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> GetMatchingPrincipalsAsync(IList<PropertyName> props)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }
    }
}
