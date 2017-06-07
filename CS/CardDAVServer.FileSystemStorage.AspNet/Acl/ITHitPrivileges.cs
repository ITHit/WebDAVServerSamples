using System;
using ITHit.WebDAV.Server.Acl;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Privileges defined for this sample which match one-to-one to windows permissions.
    /// </summary>
    public static class ITHitPrivileges
    {
        /// <summary>
        /// Corresponds to windows Write permission.
        /// </summary>
        public static readonly Privilege Write = new Privilege("ithit", "write");

        /// <summary>
        /// Corresponds to windows Read permission.
        /// </summary>
        public static readonly Privilege Read = new Privilege("ithit", "read");

        /// <summary>
        /// Corresponds to windows Modify permission.
        /// </summary>
        public static readonly Privilege Modify = new Privilege("ithit", "modify");

        /// <summary>
        /// Corresponds to windows Delete permission.
        /// </summary>
        public static readonly Privilege Delete = new Privilege("ithit", "delete");

        /// <summary>
        /// Corresponds to windows Create Files/Write Data permission.
        /// </summary>
        public static readonly Privilege CreateFilesWriteData = new Privilege("ithit", "create-files-or-write-data");

        /// <summary>
        /// Corresponds to windows Create Folders/Append Data permission.
        /// </summary>
        public static readonly Privilege CreateFoldersAppendData = 
            new Privilege("ithit", "create-folders-or-append-data");

        /// <summary>
        /// Corresponds to windows Take Ownership permission.
        /// </summary>
        public static readonly Privilege TakeOwnership = new Privilege("ithit", "take-ownership");

        /// <summary>
        /// Corresponds to windows Traverse Folder/ Execute File permission.
        /// </summary>
        public static readonly Privilege TraverseFolderOrExecuteFile = new Privilege("ithit", "traverse");

        /// <summary>
        /// Corresponds to windows Read Extended Attributes permission.
        /// </summary>
        public static readonly Privilege ReadExtendedAttributes = new Privilege("ithit", "read-extended-attributes");

        /// <summary>
        /// Corresponds to windows Write Extended Attributes permission.
        /// </summary>
        public static readonly Privilege WriteExtendedAttributes = new Privilege("ithit", "write-extended-attributes");

        /// <summary>
        /// Corresponds to windows Synchronize permission.
        /// </summary>
        public static readonly Privilege Synchronize = new Privilege("ithit", "synchronize");

        /// <summary>
        /// Corresponds to windows Read Attributes permission.
        /// </summary>
        public static readonly Privilege ReadAttributes = new Privilege("ithit", "read-attributes");

        /// <summary>
        /// Corresponds to windows Write Attributes permission.
        /// </summary>
        public static readonly Privilege WriteAttributes = new Privilege("ithit", "write-attributes");

        /// <summary>
        /// Corresponds to windows Change Permissions permission.
        /// </summary>
        public static readonly Privilege ChangePermissions = new Privilege("ithit", "change-permissions");

        /// <summary>
        /// Corresponds to windows Read Premissions permission.
        /// </summary>
        public static readonly Privilege ReadPermissions = new Privilege("ithit", "read-permissions");

        /// <summary>
        /// Corresponds to windows Read And Execute permission.
        /// </summary>
        public static readonly Privilege ReadAndExecute = new Privilege("ithit", "read-and-execute");

        /// <summary>
        /// Corresponds to windows List Directory/Read Data permission.
        /// </summary>
        public static readonly Privilege ListDirectoryReadData = new Privilege("ithit", "list-directory-read-data");

        /// <summary>
        /// Corresponds to windows Delete Subdirectories And Files permission.
        /// </summary>
        public static readonly Privilege DeleteSubDirectoriesAndFiles =
            new Privilege("ithit", "delete-subdirectories-and-files");
    }
}
