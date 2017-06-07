using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Contains helper functions which map Ntfs privileges to WebDAV privileges as well
    /// as methods which return our custom permission descriptions.    
    /// </summary>
    public static class NtfsPermissions
    {
        private static readonly IEnumerable<SupportedPrivilege> permissionsTree;           
        private static readonly Dictionary<Privilege, HashSet<Privilege>> childPrivilegesSet;
        
        /// <summary>
        /// Maps our custom permissions to ntfs permissions.
        /// </summary>
        private static readonly Dictionary<Privilege, FileSystemRights> privilegeMap
            = new Dictionary<Privilege, FileSystemRights>
                  {
                      { ITHitPrivileges.ChangePermissions, FileSystemRights.ChangePermissions },
                      { ITHitPrivileges.CreateFilesWriteData, FileSystemRights.CreateFiles },
                      { ITHitPrivileges.CreateFoldersAppendData, FileSystemRights.CreateDirectories },
                      { ITHitPrivileges.Delete, FileSystemRights.Delete },
                      { ITHitPrivileges.DeleteSubDirectoriesAndFiles, FileSystemRights.DeleteSubdirectoriesAndFiles },
                      { ITHitPrivileges.ListDirectoryReadData, FileSystemRights.ListDirectory },
                      { ITHitPrivileges.Modify, FileSystemRights.Modify },
                      { ITHitPrivileges.Read, FileSystemRights.Read },
                      { ITHitPrivileges.ReadAndExecute, FileSystemRights.ReadAndExecute },
                      { ITHitPrivileges.ReadAttributes, FileSystemRights.ReadAttributes },
                      { ITHitPrivileges.ReadExtendedAttributes, FileSystemRights.ReadExtendedAttributes },
                      { ITHitPrivileges.ReadPermissions, FileSystemRights.ReadPermissions },
                      { ITHitPrivileges.TakeOwnership, FileSystemRights.TakeOwnership },
                      { ITHitPrivileges.TraverseFolderOrExecuteFile, FileSystemRights.Traverse },
                      { ITHitPrivileges.Write, FileSystemRights.Write },
                      { ITHitPrivileges.WriteAttributes, FileSystemRights.WriteAttributes },
                      { ITHitPrivileges.WriteExtendedAttributes, FileSystemRights.WriteExtendedAttributes },
                      { ITHitPrivileges.Synchronize, FileSystemRights.Synchronize },
                      { Privilege.All, FileSystemRights.FullControl },
                  };

        static NtfsPermissions()
        {
            permissionsTree = buildPermissionsTree();
            childPrivilegesSet = new Dictionary<Privilege, HashSet<Privilege>>();
            foreach (SupportedPrivilege priv in permissionsTree)
            {
                buildChildTreeFor(priv);
            }
        }

        private static HashSet<Privilege> buildChildTreeFor(SupportedPrivilege priv)
        {
            if (childPrivilegesSet.ContainsKey(priv.Privilege))
                return childPrivilegesSet[priv.Privilege];

            HashSet<Privilege> childPrivileges = new HashSet<Privilege>();
            foreach (SupportedPrivilege sp in priv.AggregatedPrivileges)
            {
                foreach (Privilege childPriv in buildChildTreeFor(sp))
                {
                    childPrivileges.Add(childPriv);
                }
            }
            childPrivileges.Add(priv.Privilege);
            childPrivilegesSet.Add(priv.Privilege, childPrivileges);

            return childPrivileges;
        }

        /// <summary>
        /// Expands all aggregated privileges into single hash set.
        /// </summary>
        /// <param name="privileges">Privileges to expand.</param>
        /// <returns>All aggregated privileges including passed on as hash set.</returns>
        public static HashSet<Privilege> ExpandPrivileges(IEnumerable<Privilege> privileges)
        {
            HashSet<Privilege> expandedList = new HashSet<Privilege>();
            foreach (Privilege rootPrivilege in privileges)
            {
                HashSet<Privilege> list;
                if (childPrivilegesSet.TryGetValue(rootPrivilege, out list))
                {
                    foreach (Privilege priv in list)
                    {
                        expandedList.Add(priv);
                    }
                }
            }

            return expandedList;
        }

        /// <summary>
        /// Returns <see cref="FileSystemRights"/> mask which corresponds to list of privileges.
        /// </summary>
        /// <param name="privileges">List of <see cref="Privilege"/>.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns><see cref="FileSystemRights"/> mask.</returns>
        public static FileSystemRights MapPrivileges(IList<Privilege> privileges, ILogger logger)
        {
            FileSystemRights rights = 0;
            foreach (Privilege privilege in privileges)
            {
                FileSystemRights r;
                if (privilegeMap.TryGetValue(privilege, out r))
                {
                    rights |= r;
                }
                else
                {
                    logger.LogError("Cannot map privilege: " + privilege.Name, null);
                    throw new DavException("Unsupported privilege: " + privilege.Name, DavStatus.FORBIDDEN, SetAclErrorDetails.NotSupporterPrivilege);
                }
            }

            return rights;
        }

        /// <summary>
        /// Returns list of privileges which correspond to <see cref="FileSystemRights"/> mask.
        /// </summary>
        /// <param name="rights"><see cref="FileSystemRights"/> mask.</param>
        /// <returns>Collection of corresponding privileges.</returns>
        public static ICollection<Privilege> MapFileSystemRights(FileSystemRights rights)
        {
            HashSet<Privilege> privileges = new HashSet<Privilege>();
            FileSystemRights[] values = Enum.GetValues(typeof(FileSystemRights)).Cast<FileSystemRights>().ToArray();
            foreach (FileSystemRights right in values)
            {
                if ((rights & right) == right && 
                    !values.Where(cf => cf != right && (cf & right) == right && (rights & cf) == cf).Any())
                {
                    Privilege privilege = privilegeMap.Where(p => p.Value == right).Select(p => p.Key).First();
                    privileges.Add(privilege);
                }
            }

            return privileges;
        }

       /// <summary>
       /// Returns pre-built tree of available privileges.
       /// </summary>
       /// <returns></returns>
        public static IEnumerable<SupportedPrivilege> GetPermissionsTree()
        {
            return permissionsTree;
        }

        /// <summary>
        /// Returns tree of permissions. Permission can be aggregated into another permissions,
        /// so this is represented as tree (graph).
        /// </summary>
        /// <returns>Root level permissions.</returns>
        private static IEnumerable<SupportedPrivilege> buildPermissionsTree()
        {
            SupportedPrivilege davUnlock =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.Unlock,
                        IsAbstract = true,
                        Description = "Allows or denies a user to unlock file/folder if he is not the owner of the lock.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege davReadCurrentUserPivilegeSet =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.ReadCurrentUserPrivilegeSet,
                        IsAbstract = true,
                        Description = "Allows or denies reading privileges of current user.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege traverseFolderExecuteFile =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.TraverseFolderOrExecuteFile,
                        IsAbstract = false,
                        Description = "Allows or denies browsing through a folder's subfolders and files and executing files withing folder",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege readExtendedAttributes =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ReadExtendedAttributes,
                        IsAbstract = false,
                        Description = "Allows or denies viewing the extended attributes of a file or folder(defined by program)",
                        DescriptionLanguage = "en"
                    };


            SupportedPrivilege synchronize =
                new SupportedPrivilege
                {
                    Privilege = ITHitPrivileges.Synchronize,
                    IsAbstract = false,
                    Description = "Synchronize",
                    DescriptionLanguage = "en"
                };

            SupportedPrivilege writeExtendedAttributes =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.WriteExtendedAttributes,
                        IsAbstract = false,
                        Description = "Allows or denies writing the extended attributes of a file or folder(defined by program)",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege readAttributes =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ReadAttributes,
                        IsAbstract = false,
                        Description = "This allows or denies a user to view the standard NTFS attributes of a file or folder.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege writeAttributes =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.WriteAttributes,
                        IsAbstract = false,
                        Description = "This allows or denies the ability to change the attributes of a files or folder",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege delete =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.Delete,
                        IsAbstract = false,
                        Description = "Allows or denies the deleting of files and folders.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege takeOwnership =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.TakeOwnership,
                        IsAbstract = false,
                        Description = "This allows or denies a user the ability to take ownership of a file or folder.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege davWriteProperties =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.WriteProperties,
                        IsAbstract = true,
                        Description = "Allows or denies writing properties.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege createFilesWriteData =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.CreateFilesWriteData,
                        IsAbstract = false,
                        Description = "Allows or denies the user the right to create new files in the parent folder.",
                        DescriptionLanguage = "en"
                    };

            SupportedPrivilege davWriteContent =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.WriteContent,
                        IsAbstract = true,
                        Description = "Allows or denies modifying file's content.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    createFilesWriteData
                                }
                    };

            SupportedPrivilege createFoldersAppendData =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.CreateFoldersAppendData,
                        IsAbstract = false,
                        Description = "Allows or denies the user to create new folders in the parent folder.",
                        DescriptionLanguage = "en",
                    };

            SupportedPrivilege davBind =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.Bind,
                        IsAbstract = true,
                        Description = "Allows or denies the user to create new folders or files in the parent folder.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    createFoldersAppendData,
                                    createFilesWriteData
                                }
                    };

            SupportedPrivilege deleteSubfoldersAndFiles =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.DeleteSubDirectoriesAndFiles,
                        IsAbstract = false,
                        Description = "Allows or denies the deleting of files and subfolder within the parent folder.",
                        DescriptionLanguage = "en",
                    };

            SupportedPrivilege davUnbind =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.Unbind,
                        IsAbstract = true,
                        Description = "Allows or denies removing child items from collection.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    deleteSubfoldersAndFiles
                                }
                    };

            SupportedPrivilege changePermissions =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ChangePermissions,
                        IsAbstract = false,
                        Description = "Allows or denies the user the ability to change permissions of a files or folder.",
                        DescriptionLanguage = "en",
                    };

            SupportedPrivilege davWriteAcl =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.WriteAcl,
                        IsAbstract = true,
                        Description = "Allows or denies the user the ability to change permissions of a files or folder.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    changePermissions
                                }
                    };

            SupportedPrivilege davWrite =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.Write,
                        IsAbstract = true,
                        Description = "Allows or denies locking an item or modifying the content, properties, or membership of a collection.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    readAttributes,
                                    writeAttributes,
                                    delete,
                                    takeOwnership,
                                    davWriteProperties,
                                    davWriteContent,
                                    davBind,
                                    davUnbind,
                                    davWriteAcl,
                                }
                    };

            SupportedPrivilege listFolderReadData =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ListDirectoryReadData,
                        IsAbstract = false,
                        Description = "Allows or denies the user to view subfolders and fill names in the parent folder. In addition, it allows or denies the user to view the data within the files in the parent folder or subfolders of that parent.",
                        DescriptionLanguage = "en",
                    };

            SupportedPrivilege readPermissions =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ReadPermissions,
                        IsAbstract = false,
                        Description = "Allows or denies the user the ability to read permissions of a file or folder.",
                        DescriptionLanguage = "en",
                    };

            SupportedPrivilege davReadAcl =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.ReadAcl,
                        IsAbstract = true,
                        Description = "Allows or denies the user the ability to read permissions of a file or folder.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    readPermissions,
                                }
                    };

            SupportedPrivilege davRead =
                new SupportedPrivilege
                    {
                        Privilege = Privilege.Read,
                        IsAbstract = true,
                        Description = "Allows or denies the user the ability to read content and properties of files/folders.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    listFolderReadData,
                                    readAttributes,
                                    davReadAcl,
                                    davReadCurrentUserPivilegeSet
                                }
                    };

            SupportedPrivilege modify =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.Modify,
                        IsAbstract = false,
                        Description = "Allows or denies modifying file's or folder's content.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    traverseFolderExecuteFile,
                                    readExtendedAttributes,
                                    writeExtendedAttributes,
                                    synchronize,
                                    delete,
                                    takeOwnership,
                                    davWrite,
                                    davRead
                                }
                    };

            SupportedPrivilege readAndExecute =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.ReadAndExecute,
                        IsAbstract = false,
                        Description = "Allows or denies the user the ability to read content and properties of files/folders.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    traverseFolderExecuteFile,
                                    readExtendedAttributes,
                                    synchronize,
                                    davRead
                                }
                    };

            SupportedPrivilege read =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.Read,
                        IsAbstract = false,
                        Description = "Allows or denies reading file or folder's content and properties.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    readExtendedAttributes,
                                    synchronize,
                                    davRead
                                }
                    };

            SupportedPrivilege write =
                new SupportedPrivilege
                    {
                        Privilege = ITHitPrivileges.Write,
                        IsAbstract = false,
                        Description = "Allows or denies modifying file or folder's content and properties.",
                        DescriptionLanguage = "en",
                        AggregatedPrivileges =
                            new[]
                                {
                                    writeAttributes,
                                    writeExtendedAttributes,
                                    synchronize,
                                    davBind,
                                    davReadAcl
                                }
                    };

            return new[]
                       {
                           new SupportedPrivilege
                               {
                                   Privilege = Privilege.All,
                                   IsAbstract = false,
                                   Description = "Allows or denies all access to file/folder",
                                   DescriptionLanguage = "en",
                                   AggregatedPrivileges =
                                       new[]
                                           {
                                               davUnlock,
                                               modify,
                                               readAndExecute,
                                               read,
                                               write
                                           }
                               }
                       };
        }
    }
}
