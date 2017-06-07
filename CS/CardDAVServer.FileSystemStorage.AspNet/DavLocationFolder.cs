using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;


namespace CardDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Logical folder which contains /acl/, /calendars/ and /addressbooks/ folders.
    /// Represents a folder with the following path: [DAVLocation]
    /// </summary>
    /// <example>
    /// [DavLocation]  -- this class
    ///  |-- acl
    ///  |-- calendars
    ///  |-- addressbooks
    /// </example>
    public class DavLocationFolder : DavFolder
    {
        /// <summary>
        /// Path to this folder.
        /// </summary>
        /// <value>Returns first non-root path from DavLocation section from config file or "/" if no DavLocation section is found.</value>
        public static string DavLocationFolderPath
        {
            get
            {
                NameValueCollection davLocationsSection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("davLocations");

                if (davLocationsSection != null)
                {
                    foreach (string path in davLocationsSection.AllKeys)
                    {
                        // Typically you will enable WebDAV on site root ('/') to allow CalDAV/CardDAV 
                        // discovery. We skip site root WebDAV location to find first non-root location.
                        if (!string.IsNullOrEmpty(path.Trim('/')))
                            return path.TrimEnd('/') + '/';
                    }
                }

                // If no davLocation section is found or no non-root WebDAV location is specified in 
                // configuration file asume the WebDAV is on web site root.
                return "/";
            }
        }

        /// <summary>
        /// Returns DavLocationFolder folder if path corresponds to [DavLocation].
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>DavLocationFolder instance or null if physical folder not found in file system.</returns>
        public static DavLocationFolder GetDavLocationFolder(DavContext context, string path)
        {
            string davPath = DavLocationFolderPath;
            if (!path.Equals(davPath.Trim(new[] { '/' }), StringComparison.OrdinalIgnoreCase))
                return null;

            string folderPath = context.MapPath(davPath).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            
            if (!folder.Exists)
                throw new Exception(string.Format("Can not find folder that corresponds to '{0}' ([DavLocation] folder) in file system.", davPath));

            return new DavLocationFolder(folder, context, davPath);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="directory">Instance of <see cref="DirectoryInfo"/> class with information about the folder in file system.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <param name="path">Relative to WebDAV root folder path.</param>
        private DavLocationFolder(DirectoryInfo directory, DavContext context, string path)
            : base(directory, context, path)
        {
        }

        /// <summary>
        /// Retrieves children of this folder: /acl/, /calendars/ and /addressbooks/ folders.
        /// </summary>
        /// <param name="propNames">Properties requested by client application for each child.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {
            List<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();

            // At the upper level we have folder named [DavLocation]/acl/ which stores users and groups.
            // This is a 'virtual' folder, it does not exist in file system.
            children.Add(new Acl.AclFolder(context));
            
            // Get [DavLocation]/calendars/ and [DavLocation]/addressbooks/ folders.
            children.AddRange(await base.GetChildrenAsync(propNames));

            return children;
        }
    }
}
