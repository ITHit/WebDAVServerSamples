Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace CardDav

    ''' <summary>
    ''' Folder that contains user folders which contain address books.
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/
    ''' </summary>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- addressbooks  -- this class
    '''      |-- [User1]
    '''      |-- ...
    '''      |-- [UserX]
    ''' </example>
    Public Class AddressbooksRootFolder
        Inherits DavFolder

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly addressbooksRootFolderName As String = "addressbooks"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared AddressbooksRootFolderPath As String = String.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, addressbooksRootFolderName)

        ''' <summary>
        ''' Returns address books root folder that corresponds to path or null if path does not correspond to address books root folder.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        ''' <returns>AddressbooksRootFolder instance or null if path does not correspond to this folder.</returns>
        Public Shared Function GetAddressbooksRootFolder(context As DavContext, path As String) As AddressbooksRootFolder
            If Not path.Equals(AddressbooksRootFolderPath, StringComparison.InvariantCultureIgnoreCase) Then Return Nothing
            Dim folder As DirectoryInfo = New DirectoryInfo(context.MapPath(path))
            If Not folder.Exists Then Return Nothing
            Return New AddressbooksRootFolder(folder, context, path)
        End Function

        Private Sub New(directory As DirectoryInfo, context As DavContext, path As String)
            MyBase.New(directory, context, path)
        End Sub

        'If required you can appy some rules, for example prohibit creating files in this folder
        '
        '/// <summary>
        '/// Prohibit creating files in this folder.
        '/// </summary>
        'override public async Task<IFile> CreateFileAsync(string name, Stream content, string contentType, long totalFileSize)
        '{
        'throw new DavException("Creating files in this folder is not implemented.", DavStatus.NOT_IMPLEMENTED);
        '}
        '
        '/// <summary>
        '/// Prohibit creating folders via WebDAV in this folder.
        '/// </summary>
        '/// <remarks>
        '/// New user folders are created during first log-in.
        '/// </remarks>
        'override public async Task<IFolder> CreateFolderAsync(string name)
        '{
        'throw new DavException("Creating sub-folders in this folder is not implemented.", DavStatus.NOT_IMPLEMENTED);
        '}
        '
        '/// <summary>
        '/// Prohibit copying this folder.
        '/// </summary>
        'override public async Task CopyToAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus)
        '{
        'throw new DavException("Copying this folder is not allowed.", DavStatus.NOT_ALLOWED);
        '}
        ''' <summary>
        ''' Prohibit moving or renaming this folder
        ''' </summary>        
        Overrides Public Async Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task
            Throw New DavException("Moving or renaming this folder is not allowed.", DavStatus.NOT_ALLOWED)
        End Function

        ''' <summary>
        ''' Prohibit deleting this folder.
        ''' </summary>        
        Overrides Public Async Function DeleteAsync(multistatus As MultistatusException) As Task
            Throw New DavException("Deleting this folder is not allowed.", DavStatus.NOT_ALLOWED)
        End Function
    End Class
End Namespace
