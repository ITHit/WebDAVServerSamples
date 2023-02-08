using ITHit.WebDAV.Server.Synchronization;
using System.Collections.Generic;

namespace WebDAVServer.FileSystemSynchronization.AspNetCore
{
    public class DavChanges : List<IChangedItem>, IChanges
    {
        public string NewSyncToken { get; set; }

        public bool MoreResults { get; set; }
    }
}
