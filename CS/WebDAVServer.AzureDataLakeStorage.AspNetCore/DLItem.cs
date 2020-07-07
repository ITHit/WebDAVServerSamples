using System;
using System.Collections.Generic;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Class for representing user object.
    /// </summary>
    public class DLItem
    {
        /// <summary>
        /// Name of the item.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Relative path of the item in the Data Lake.
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// Content Type of the item.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        /// <summary>
        /// Content length of the item.
        /// </summary>
        public long ContentLength { get; set; } = 0;
        /// <summary>
        /// Created time of the item in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Modified time of the item in UTC.
        /// </summary>
        public DateTime ModifiedUtc { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Custom properties of the item.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
