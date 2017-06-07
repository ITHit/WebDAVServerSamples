using ITHit.WebDAV.Server;

namespace CalDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Contains description of principal's properties.
    /// </summary>
    public static class PrincipalProperties
    {        
        public static readonly PropertyName FullName = new PropertyName("full-name", "ithit");
        public static readonly PropertyName Description = new PropertyName("description", "ithit");
        public static readonly PropertyName[] ALL;
        
        static PrincipalProperties()
        {
            ALL = new[] { FullName, Description };
        }        
    }
}
