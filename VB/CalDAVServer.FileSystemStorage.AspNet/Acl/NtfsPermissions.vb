Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.AccessControl
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    Module NtfsPermissions

        Private ReadOnly permissionsTree As IEnumerable(Of SupportedPrivilege)

        Private ReadOnly childPrivilegesSet As Dictionary(Of Privilege, HashSet(Of Privilege))

        ''' <summary>
        ''' Maps our custom permissions to ntfs permissions.
        ''' </summary>
        Private ReadOnly privilegeMap As Dictionary(Of Privilege, FileSystemRights) = New Dictionary(Of Privilege, FileSystemRights) From {{ITHitPrivileges.ChangePermissions, FileSystemRights.ChangePermissions},
                                                                                                                                          {ITHitPrivileges.CreateFilesWriteData, FileSystemRights.CreateFiles},
                                                                                                                                          {ITHitPrivileges.CreateFoldersAppendData, FileSystemRights.CreateDirectories},
                                                                                                                                          {ITHitPrivileges.Delete, FileSystemRights.Delete},
                                                                                                                                          {ITHitPrivileges.DeleteSubDirectoriesAndFiles, FileSystemRights.DeleteSubdirectoriesAndFiles},
                                                                                                                                          {ITHitPrivileges.ListDirectoryReadData, FileSystemRights.ListDirectory},
                                                                                                                                          {ITHitPrivileges.Modify, FileSystemRights.Modify},
                                                                                                                                          {ITHitPrivileges.Read, FileSystemRights.Read},
                                                                                                                                          {ITHitPrivileges.ReadAndExecute, FileSystemRights.ReadAndExecute},
                                                                                                                                          {ITHitPrivileges.ReadAttributes, FileSystemRights.ReadAttributes},
                                                                                                                                          {ITHitPrivileges.ReadExtendedAttributes, FileSystemRights.ReadExtendedAttributes},
                                                                                                                                          {ITHitPrivileges.ReadPermissions, FileSystemRights.ReadPermissions},
                                                                                                                                          {ITHitPrivileges.TakeOwnership, FileSystemRights.TakeOwnership},
                                                                                                                                          {ITHitPrivileges.TraverseFolderOrExecuteFile, FileSystemRights.Traverse},
                                                                                                                                          {ITHitPrivileges.Write, FileSystemRights.Write},
                                                                                                                                          {ITHitPrivileges.WriteAttributes, FileSystemRights.WriteAttributes},
                                                                                                                                          {ITHitPrivileges.WriteExtendedAttributes, FileSystemRights.WriteExtendedAttributes},
                                                                                                                                          {ITHitPrivileges.Synchronize, FileSystemRights.Synchronize},
                                                                                                                                          {Privilege.All, FileSystemRights.FullControl}}

        Sub New()
            permissionsTree = buildPermissionsTree()
            childPrivilegesSet = New Dictionary(Of Privilege, HashSet(Of Privilege))()
            For Each priv As SupportedPrivilege In permissionsTree
                buildChildTreeFor(priv)
            Next
        End Sub

        Private Function buildChildTreeFor(priv As SupportedPrivilege) As HashSet(Of Privilege)
            If childPrivilegesSet.ContainsKey(priv.Privilege) Then Return childPrivilegesSet(priv.Privilege)
            Dim childPrivileges As HashSet(Of Privilege) = New HashSet(Of Privilege)()
            For Each sp As SupportedPrivilege In priv.AggregatedPrivileges
                For Each childPriv As Privilege In buildChildTreeFor(sp)
                    childPrivileges.Add(childPriv)
                Next
            Next

            childPrivileges.Add(priv.Privilege)
            childPrivilegesSet.Add(priv.Privilege, childPrivileges)
            Return childPrivileges
        End Function

        Function ExpandPrivileges(privileges As IEnumerable(Of Privilege)) As HashSet(Of Privilege)
            Dim expandedList As HashSet(Of Privilege) = New HashSet(Of Privilege)()
            For Each rootPrivilege As Privilege In privileges
                Dim list As HashSet(Of Privilege)
                If childPrivilegesSet.TryGetValue(rootPrivilege, list) Then
                    For Each priv As Privilege In list
                        expandedList.Add(priv)
                    Next
                End If
            Next

            Return expandedList
        End Function

        Function MapPrivileges(privileges As IList(Of Privilege), logger As ILogger) As FileSystemRights
            Dim rights As FileSystemRights = 0
            For Each privilege As Privilege In privileges
                Dim r As FileSystemRights
                If privilegeMap.TryGetValue(privilege, r) Then
                    rights = rights Or r
                Else
                    logger.LogError("Cannot map privilege: " & privilege.Name, Nothing)
                    Throw New DavException("Unsupported privilege: " & privilege.Name, DavStatus.FORBIDDEN, SetAclErrorDetails.NotSupporterPrivilege)
                End If
            Next

            Return rights
        End Function

        Function MapFileSystemRights(rights As FileSystemRights) As ICollection(Of Privilege)
            Dim privileges As HashSet(Of Privilege) = New HashSet(Of Privilege)()
            Dim values As FileSystemRights() = [Enum].GetValues(GetType(FileSystemRights)).Cast(Of FileSystemRights)().ToArray()
            For Each right As FileSystemRights In values
                If(rights And right) = right AndAlso Not values.Where(Function(cf) cf <> right AndAlso (cf And right) = right AndAlso (rights And cf) = cf).Any() Then
                    Dim privilege As Privilege = privilegeMap.Where(Function(p) p.Value = right).Select(Function(p) p.Key).First()
                    privileges.Add(privilege)
                End If
            Next

            Return privileges
        End Function

        Function GetPermissionsTree() As IEnumerable(Of SupportedPrivilege)
            Return permissionsTree
        End Function

        Private Function buildPermissionsTree() As IEnumerable(Of SupportedPrivilege)
            Dim davUnlock As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.Unlock, .IsAbstract = True, .Description = "Allows or denies a user to unlock file/folder if he is not the owner of the lock.", .DescriptionLanguage = "en"}
            Dim davReadCurrentUserPivilegeSet As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.ReadCurrentUserPrivilegeSet, .IsAbstract = True, .Description = "Allows or denies reading privileges of current user.", .DescriptionLanguage = "en"}
            Dim traverseFolderExecuteFile As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.TraverseFolderOrExecuteFile, .IsAbstract = False, .Description = "Allows or denies browsing through a folder's subfolders and files and executing files withing folder", .DescriptionLanguage = "en"}
            Dim readExtendedAttributes As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ReadExtendedAttributes, .IsAbstract = False, .Description = "Allows or denies viewing the extended attributes of a file or folder(defined by program)", .DescriptionLanguage = "en"}
            Dim synchronize As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.Synchronize, .IsAbstract = False, .Description = "Synchronize", .DescriptionLanguage = "en"}
            Dim writeExtendedAttributes As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.WriteExtendedAttributes, .IsAbstract = False, .Description = "Allows or denies writing the extended attributes of a file or folder(defined by program)", .DescriptionLanguage = "en"}
            Dim readAttributes As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ReadAttributes, .IsAbstract = False, .Description = "This allows or denies a user to view the standard NTFS attributes of a file or folder.", .DescriptionLanguage = "en"}
            Dim writeAttributes As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.WriteAttributes, .IsAbstract = False, .Description = "This allows or denies the ability to change the attributes of a files or folder", .DescriptionLanguage = "en"}
            Dim delete As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.Delete, .IsAbstract = False, .Description = "Allows or denies the deleting of files and folders.", .DescriptionLanguage = "en"}
            Dim takeOwnership As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.TakeOwnership, .IsAbstract = False, .Description = "This allows or denies a user the ability to take ownership of a file or folder.", .DescriptionLanguage = "en"}
            Dim davWriteProperties As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.WriteProperties, .IsAbstract = True, .Description = "Allows or denies writing properties.", .DescriptionLanguage = "en"}
            Dim createFilesWriteData As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.CreateFilesWriteData, .IsAbstract = False, .Description = "Allows or denies the user the right to create new files in the parent folder.", .DescriptionLanguage = "en"}
            Dim davWriteContent As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.WriteContent, .IsAbstract = True, .Description = "Allows or denies modifying file's content.", .DescriptionLanguage = "en", .AggregatedPrivileges = {createFilesWriteData
                                                                                                                                                                                                                                                                }}
            Dim createFoldersAppendData As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.CreateFoldersAppendData, .IsAbstract = False, .Description = "Allows or denies the user to create new folders in the parent folder.", .DescriptionLanguage = "en"}
            Dim davBind As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.Bind, .IsAbstract = True, .Description = "Allows or denies the user to create new folders or files in the parent folder.", .DescriptionLanguage = "en", .AggregatedPrivileges = {createFoldersAppendData,
                                                                                                                                                                                                                                                                                    createFilesWriteData
                                                                                                                                                                                                                                                                                    }}
            Dim deleteSubfoldersAndFiles As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.DeleteSubDirectoriesAndFiles, .IsAbstract = False, .Description = "Allows or denies the deleting of files and subfolder within the parent folder.", .DescriptionLanguage = "en"}
            Dim davUnbind As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.Unbind, .IsAbstract = True, .Description = "Allows or denies removing child items from collection.", .DescriptionLanguage = "en", .AggregatedPrivileges = {deleteSubfoldersAndFiles
                                                                                                                                                                                                                                                                }}
            Dim changePermissions As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ChangePermissions, .IsAbstract = False, .Description = "Allows or denies the user the ability to change permissions of a files or folder.", .DescriptionLanguage = "en"}
            Dim davWriteAcl As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.WriteAcl, .IsAbstract = True, .Description = "Allows or denies the user the ability to change permissions of a files or folder.", .DescriptionLanguage = "en", .AggregatedPrivileges = {changePermissions
                                                                                                                                                                                                                                                                                               }}
            Dim davWrite As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.Write, .IsAbstract = True, .Description = "Allows or denies locking an item or modifying the content, properties, or membership of a collection.", .DescriptionLanguage = "en", .AggregatedPrivileges = {readAttributes,
                                                                                                                                                                                                                                                                                                             writeAttributes,
                                                                                                                                                                                                                                                                                                             delete,
                                                                                                                                                                                                                                                                                                             takeOwnership,
                                                                                                                                                                                                                                                                                                             davWriteProperties,
                                                                                                                                                                                                                                                                                                             davWriteContent,
                                                                                                                                                                                                                                                                                                             davBind,
                                                                                                                                                                                                                                                                                                             davUnbind,
                                                                                                                                                                                                                                                                                                             davWriteAcl}}
            Dim listFolderReadData As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ListDirectoryReadData, .IsAbstract = False, .Description = "Allows or denies the user to view subfolders and fill names in the parent folder. In addition, it allows or denies the user to view the data within the files in the parent folder or subfolders of that parent.", .DescriptionLanguage = "en"}
            Dim readPermissions As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ReadPermissions, .IsAbstract = False, .Description = "Allows or denies the user the ability to read permissions of a file or folder.", .DescriptionLanguage = "en"}
            Dim davReadAcl As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.ReadAcl, .IsAbstract = True, .Description = "Allows or denies the user the ability to read permissions of a file or folder.", .DescriptionLanguage = "en", .AggregatedPrivileges = {readPermissions}}
            Dim davRead As SupportedPrivilege = New SupportedPrivilege With {.Privilege = Privilege.Read, .IsAbstract = True, .Description = "Allows or denies the user the ability to read content and properties of files/folders.", .DescriptionLanguage = "en", .AggregatedPrivileges = {listFolderReadData,
                                                                                                                                                                                                                                                                                            readAttributes,
                                                                                                                                                                                                                                                                                            davReadAcl,
                                                                                                                                                                                                                                                                                            davReadCurrentUserPivilegeSet
                                                                                                                                                                                                                                                                                            }}
            Dim modify As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.Modify, .IsAbstract = False, .Description = "Allows or denies modifying file's or folder's content.", .DescriptionLanguage = "en", .AggregatedPrivileges = {traverseFolderExecuteFile,
                                                                                                                                                                                                                                                                    readExtendedAttributes,
                                                                                                                                                                                                                                                                    writeExtendedAttributes,
                                                                                                                                                                                                                                                                    synchronize,
                                                                                                                                                                                                                                                                    delete,
                                                                                                                                                                                                                                                                    takeOwnership,
                                                                                                                                                                                                                                                                    davWrite,
                                                                                                                                                                                                                                                                    davRead
                                                                                                                                                                                                                                                                    }}
            Dim readAndExecute As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.ReadAndExecute, .IsAbstract = False, .Description = "Allows or denies the user the ability to read content and properties of files/folders.", .DescriptionLanguage = "en", .AggregatedPrivileges = {traverseFolderExecuteFile,
                                                                                                                                                                                                                                                                                                                    readExtendedAttributes,
                                                                                                                                                                                                                                                                                                                    synchronize,
                                                                                                                                                                                                                                                                                                                    davRead
                                                                                                                                                                                                                                                                                                                    }}
            Dim read As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.Read, .IsAbstract = False, .Description = "Allows or denies reading file or folder's content and properties.", .DescriptionLanguage = "en", .AggregatedPrivileges = {readExtendedAttributes,
                                                                                                                                                                                                                                                                           synchronize,
                                                                                                                                                                                                                                                                           davRead
                                                                                                                                                                                                                                                                           }}
            Dim write As SupportedPrivilege = New SupportedPrivilege With {.Privilege = ITHitPrivileges.Write, .IsAbstract = False, .Description = "Allows or denies modifying file or folder's content and properties.", .DescriptionLanguage = "en", .AggregatedPrivileges = {writeAttributes,
                                                                                                                                                                                                                                                                               writeExtendedAttributes,
                                                                                                                                                                                                                                                                               synchronize,
                                                                                                                                                                                                                                                                               davBind,
                                                                                                                                                                                                                                                                               davReadAcl
                                                                                                                                                                                                                                                                               }}
            Return {New SupportedPrivilege With {.Privilege = Privilege.All, .IsAbstract = False, .Description = "Allows or denies all access to file/folder", .DescriptionLanguage = "en", .AggregatedPrivileges = {davUnlock,
                                                                                                                                                                                                                    modify,
                                                                                                                                                                                                                    readAndExecute,
                                                                                                                                                                                                                    read,
                                                                                                                                                                                                                    write
                                                                                                                                                                                                                    }}}
        End Function
    End Module
End Namespace
