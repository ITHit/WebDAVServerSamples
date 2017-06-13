Imports System
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    ''' <summary>
    ''' Privileges defined for this sample which match one-to-one to windows permissions.
    ''' </summary>
    Module ITHitPrivileges

        ''' <summary>
        ''' Corresponds to windows Write permission.
        ''' </summary>
        Public ReadOnly Write As Privilege = New Privilege("ithit", "write")

        ''' <summary>
        ''' Corresponds to windows Read permission.
        ''' </summary>
        Public ReadOnly Read As Privilege = New Privilege("ithit", "read")

        ''' <summary>
        ''' Corresponds to windows Modify permission.
        ''' </summary>
        Public ReadOnly Modify As Privilege = New Privilege("ithit", "modify")

        ''' <summary>
        ''' Corresponds to windows Delete permission.
        ''' </summary>
        Public ReadOnly Delete As Privilege = New Privilege("ithit", "delete")

        ''' <summary>
        ''' Corresponds to windows Create Files/Write Data permission.
        ''' </summary>
        Public ReadOnly CreateFilesWriteData As Privilege = New Privilege("ithit", "create-files-or-write-data")

        ''' <summary>
        ''' Corresponds to windows Create Folders/Append Data permission.
        ''' </summary>
        Public ReadOnly CreateFoldersAppendData As Privilege = New Privilege("ithit", "create-folders-or-append-data")

        ''' <summary>
        ''' Corresponds to windows Take Ownership permission.
        ''' </summary>
        Public ReadOnly TakeOwnership As Privilege = New Privilege("ithit", "take-ownership")

        ''' <summary>
        ''' Corresponds to windows Traverse Folder/ Execute File permission.
        ''' </summary>
        Public ReadOnly TraverseFolderOrExecuteFile As Privilege = New Privilege("ithit", "traverse")

        ''' <summary>
        ''' Corresponds to windows Read Extended Attributes permission.
        ''' </summary>
        Public ReadOnly ReadExtendedAttributes As Privilege = New Privilege("ithit", "read-extended-attributes")

        ''' <summary>
        ''' Corresponds to windows Write Extended Attributes permission.
        ''' </summary>
        Public ReadOnly WriteExtendedAttributes As Privilege = New Privilege("ithit", "write-extended-attributes")

        ''' <summary>
        ''' Corresponds to windows Synchronize permission.
        ''' </summary>
        Public ReadOnly Synchronize As Privilege = New Privilege("ithit", "synchronize")

        ''' <summary>
        ''' Corresponds to windows Read Attributes permission.
        ''' </summary>
        Public ReadOnly ReadAttributes As Privilege = New Privilege("ithit", "read-attributes")

        ''' <summary>
        ''' Corresponds to windows Write Attributes permission.
        ''' </summary>
        Public ReadOnly WriteAttributes As Privilege = New Privilege("ithit", "write-attributes")

        ''' <summary>
        ''' Corresponds to windows Change Permissions permission.
        ''' </summary>
        Public ReadOnly ChangePermissions As Privilege = New Privilege("ithit", "change-permissions")

        ''' <summary>
        ''' Corresponds to windows Read Premissions permission.
        ''' </summary>
        Public ReadOnly ReadPermissions As Privilege = New Privilege("ithit", "read-permissions")

        ''' <summary>
        ''' Corresponds to windows Read And Execute permission.
        ''' </summary>
        Public ReadOnly ReadAndExecute As Privilege = New Privilege("ithit", "read-and-execute")

        ''' <summary>
        ''' Corresponds to windows List Directory/Read Data permission.
        ''' </summary>
        Public ReadOnly ListDirectoryReadData As Privilege = New Privilege("ithit", "list-directory-read-data")

        ''' <summary>
        ''' Corresponds to windows Delete Subdirectories And Files permission.
        ''' </summary>
        Public ReadOnly DeleteSubDirectoriesAndFiles As Privilege = New Privilege("ithit", "delete-subdirectories-and-files")
    End Module
End Namespace
