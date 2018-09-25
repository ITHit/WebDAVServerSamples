using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Paging;

namespace CardDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Base class for logical folders which are not present in file system, like '/acl/,
    /// '/acl/groups/'
    /// </summary>
    public abstract class LogicalFolder : Discovery , IItemCollectionAsync
    {
        public DavContext Context { get; private set; }

        public string Name { get; private set; }

        public string Path { get; private set; }

        protected LogicalFolder(DavContext context, string name, string path): base(context)
        {
            this.Context = context;
            this.Name = name;
            this.Path = path;
        }

        public DateTime Created
        {
            get { return DateTime.UtcNow; }
        }

        public DateTime Modified
        {
            get { return DateTime.UtcNow; }
        }

        public async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            throw new NotImplementedException();
        }

        public async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(MultistatusException multistatus)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop)
        {
            return new PropertyValue[0];
        }

        public async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            return new PropertyName[0];
        }

        public async Task UpdatePropertiesAsync(IList<PropertyValue> setProps, IList<PropertyName> delProps, MultistatusException multistatus)
        {
            throw new NotImplementedException();
        }

        public abstract Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps);
    }
}
