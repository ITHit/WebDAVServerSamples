using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Paging;

namespace CalDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Base class for logical folders which are not present in your 
    /// back-end storage (datatbase, file system, etc), like [DavLocation], '[DavLocation]/acl/, '[DavLocation]/acl/users/'
    /// </summary>
    public class LogicalFolder : DavHierarchyItem, IItemCollectionAsync
    {

        private IEnumerable<IHierarchyItemAsync> children;

        /// <summary>
        /// Creates instance of <see cref="LogicalFolder"/> class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <param name="children">List of child items that will be returned when enumerating this folder children.</param>
        public LogicalFolder(DavContext context, string path, IEnumerable<IHierarchyItemAsync> children = null)
            : base(context)
        {
            this.Context = context;
            this.itemPath = path;
            this.children = children ?? new IHierarchyItemAsync[0];
            
            path = path.TrimEnd('/');
            string encodedName = path.Substring(path.LastIndexOf('/') + 1);
            this.displayName = EncodeUtil.DecodeUrlPart(encodedName);
        }

        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }
  
        public virtual async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            return new PageResults(children, null);
        }
    }
}
