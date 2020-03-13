namespace WebDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Class for representing user object.
    /// </summary>
    public class DavUser
    {
        /// <summary>
        /// Represents user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Represents user email.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Represents user password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class for reading user credentials from json.
    /// </summary>
    public class DavUsersConfig
    {
        /// <summary>
        /// Represents array of users from storage.
        /// </summary>
        public DavUser[] Users { get; set; } = new DavUser[0];
    }  
}
