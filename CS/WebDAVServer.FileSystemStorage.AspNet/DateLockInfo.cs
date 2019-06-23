using System;
using ITHit.WebDAV.Server.Class2;

namespace WebDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Stores information about lock.
    /// </summary>
    public class DateLockInfo
    {
        /// <summary>
        /// Gets or sets lock owner as specified by client.
        /// </summary>
        public string ClientOwner { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets time when the lock expires.
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Gets or sets lock token.
        /// </summary>
        public string LockToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets lock level.
        /// </summary>
        public LockLevel Level { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lock is deep.
        /// </summary>
        public bool IsDeep { get; set; }

        /// <summary>
        /// Gets or sets path of item item which has the lock specified explicitly.
        /// </summary>
        public string LockRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets timeout for the lock requested by client.
        /// </summary>
        public TimeSpan TimeOut { get; set; }
    }
}
