Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports CalDAVServer.FileSystemStorage.AspNet.Acl
Imports CalDAVServer.FileSystemStorage.AspNet.ExtendedAttributes
Imports ITHit.WebDAV.Server.Search

''' <summary>
''' Folder in WebDAV repository.
''' </summary>
Public Class DavFolder
    Inherits DavHierarchyItem
    Implements IFolderAsync

    ''' <summary>
    ''' Corresponding instance of <see cref="DirectoryInfo"/> .
    ''' </summary>
    Private ReadOnly dirInfo As DirectoryInfo

    Public Shared Async Function GetFolderAsync(context As DavContext, path As String) As Task(Of DavFolder)
        Dim folderPath As String = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar)
        Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
        If Not folder.Exists OrElse String.Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) <> 0 Then
            Return Nothing
        End If

        Return New DavFolder(folder, context, path)
    End Function

    ''' <summary>
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="directory">Corresponding folder in the file system.</param>
    ''' <param name="context">WebDAV Context.</param>
    ''' <param name="path">Encoded path relative to WebDAV root folder.</param>
    Protected Sub New(directory As DirectoryInfo, context As DavContext, path As String)
        MyBase.New(directory, context, path.TrimEnd("/"c) & "/")
        dirInfo = directory
    End Sub

    Public Overridable Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
        Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
        Dim fileInfos As FileSystemInfo() = Nothing
        fileInfos = dirInfo.GetFileSystemInfos()
        For Each fileInfo As FileSystemInfo In fileInfos
            Dim childPath As String = Path & EncodeUtil.EncodeUrlPart(fileInfo.Name)
            Dim child As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(childPath)
            If child IsNot Nothing Then
                children.Add(child)
            End If
        Next

        Return children
    End Function

    Public Overridable Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
        Dim fileName As String = System.IO.Path.Combine(fileSystemInfo.FullName, name)
        Using stream As FileStream = New FileStream(fileName, FileMode.CreateNew)
        End Using

        Return CType(Await context.GetHierarchyItemAsync(Path & EncodeUtil.EncodeUrlPart(name)), IFileAsync)
    End Function

    Overridable Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
        dirInfo.CreateSubdirectory(name)
    End Function

    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Dim targetFolder As DavFolder = TryCast(destFolder, DavFolder)
        If targetFolder Is Nothing Then
            Throw New DavException("Target folder doesn't exist", DavStatus.CONFLICT)
        End If

        If IsRecursive(targetFolder) Then
            Throw New DavException("Cannot copy to subfolder", DavStatus.FORBIDDEN)
        End If

        Dim newDirLocalPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        Try
            If Not Directory.Exists(newDirLocalPath) Then
                Await targetFolder.CreateFolderAsync(destName)
            End If
        Catch ex As DavException
            ' Continue, but report error to client for the target item.
            multistatus.AddInnerException(targetPath, ex)
        End Try

        Dim createdFolder As IFolderAsync = CType(Await context.GetHierarchyItemAsync(targetPath), IFolderAsync)
        For Each item As DavHierarchyItem In Await GetChildrenAsync(New PropertyName(-1) {})
            If Not deep AndAlso TypeOf item Is DavFolder Then
                Continue For
            End If

            Try
                Await item.CopyToAsync(createdFolder, item.Name, deep, multistatus)
            Catch ex As DavException
                ' If a child item failed to copy we continue but report error to client.
                multistatus.AddInnerException(item.Path, ex)
            End Try
        Next
    End Function

    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Dim targetFolder As DavFolder = TryCast(destFolder, DavFolder)
        If targetFolder Is Nothing Then
            Throw New DavException("Target folder doesn't exist", DavStatus.CONFLICT)
        End If

        If IsRecursive(targetFolder) Then
            Throw New DavException("Cannot move folder to its subtree.", DavStatus.FORBIDDEN)
        End If

        Dim newDirPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        Try
            Dim item As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(targetPath)
            If item IsNot Nothing Then Await item.DeleteAsync(multistatus)
            Await targetFolder.CreateFolderAsync(destName)
        Catch ex As DavException
            ' Continue the operation but report error with destination path to client.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        Dim movedSuccessfully As Boolean = True
        Dim createdFolder As IFolderAsync = CType(Await context.GetHierarchyItemAsync(targetPath), IFolderAsync)
        For Each item As DavHierarchyItem In Await GetChildrenAsync(New PropertyName(-1) {})
            Try
                Await item.MoveToAsync(createdFolder, item.Name, multistatus)
            Catch ex As DavException
                ' Continue the operation but report error with child item to client.
                multistatus.AddInnerException(item.Path, ex)
                movedSuccessfully = False
            End Try
        Next

        If movedSuccessfully Then
            Await DeleteAsync(multistatus)
        End If
    End Function

    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        Dim allChildrenDeleted As Boolean = True
        For Each child As IHierarchyItemAsync In Await GetChildrenAsync(New PropertyName(-1) {})
            Try
                Await child.DeleteAsync(multistatus)
            Catch ex As DavException
                'continue the operation if a child failed to delete. Tell client about it by adding to multistatus.
                multistatus.AddInnerException(child.Path, ex)
                allChildrenDeleted = False
            End Try
        Next

        If allChildrenDeleted Then
            dirInfo.Delete()
        End If
    End Function

    Private Function IsRecursive(destFolder As DavFolder) As Boolean
        Return destFolder.Path.StartsWith(Path)
    End Function
End Class
