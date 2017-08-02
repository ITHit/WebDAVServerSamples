namespace HttpListenerLibrary.Options
{
    /// <summary>
    /// Class for reading user credentials from json.
    /// </summary>
    public class DavUserOptions
    {
        /// <summary>
        /// Represents array of users from storage.
        /// </summary>
        public DavUser[] Users { get; set; }
    }

    /// <summary>
    /// Class for representing user object.
    /// </summary>
    public class DavUser
    {
        /// <summary>
        /// Represents user name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Represents user password.
        /// </summary>
        public string Password { get; set; }
    }
}
