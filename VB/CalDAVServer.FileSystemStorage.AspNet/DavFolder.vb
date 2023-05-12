Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports CalDAVServer.FileSystemStorage.AspNet.ExtendedAttributes
Imports ITHit.WebDAV.Server.Search
Imports ITHit.WebDAV.Server.Paging
Imports CalDAVServer.FileSystemStorage.AspNet.Acl

''' <summary>
''' Folder in WebDAV repository.
''' </summary>
Public Class DavFolder
    Inherits DavHierarchyItem
    Implements IFolder

    ' Control characters and permanently undefined Unicode characters to be removed from search snippet.
    Private Shared ReadOnly invalidXmlCharsPattern As Regex = New Regex("[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]", RegexOptions.IgnoreCase)

    ''' <summary>
    ''' Corresponding instance of <see cref="DirectoryInfo"/> .
    ''' </summary>
    Private ReadOnly dirInfo As DirectoryInfo

    ''' <summary>
    ''' Returns folder that corresponds to path.
    ''' </summary>
    ''' <param name="context">WebDAV Context.</param>
    ''' <param name="path">Encoded path relative to WebDAV root folder.</param>
    ''' <returns>Folder instance or null if physical folder not found in file system.</returns>
    Public Shared Async Function GetFolderAsync(context As DavContext, path As String) As Task(Of DavFolder)
        Dim folderPath As String = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar)
        Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
        ' This code blocks vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'.
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

    ''' <summary>
    ''' Called when children of this folder with paging information are being listed.
    ''' </summary>
    ''' <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
    ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
    ''' <param name="nResults">The number of items to return.</param>
    ''' <param name="orderProps">List of order properties requested by the client.</param>
    ''' <returns>Items requested by the client and a total number of items in this folder.</returns>
    Public Overridable Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollection.GetChildrenAsync
        ' Enumerates all child files and folders.
        ' You can filter children items in this implementation and 
        ' return only items that you want to be visible for this 
        ' particular user.
        Dim children As IList(Of IHierarchyItem) = New List(Of IHierarchyItem)()
        Dim totalItems As Long = 0
        Dim fileInfos As FileSystemInfo() = dirInfo.GetFileSystemInfos()
        totalItems = fileInfos.Length
        ' Apply sorting.
        fileInfos = SortChildren(fileInfos, orderProps)
        ' Apply paging.
        If offset.HasValue AndAlso nResults.HasValue Then
            fileInfos = fileInfos.Skip(CInt(offset.Value)).Take(CInt(nResults.Value)).ToArray()
        End If

        For Each fileInfo As FileSystemInfo In fileInfos
            Dim childPath As String = Path & EncodeUtil.EncodeUrlPart(fileInfo.Name)
            Dim child As IHierarchyItem = Await context.GetHierarchyItemAsync(childPath)
            If child IsNot Nothing Then
                children.Add(child)
            End If
        Next

        Return New PageResults(children, totalItems)
    End Function

    ''' <summary>
    ''' Called when a new file is being created in this folder.
    ''' </summary>
    ''' <param name="name">Name of the new file.</param>
    ''' <param name="content">Stream to read the content of the file from.</param>
    ''' <param name="contentType">Indicates the media type of the file.</param>
    ''' <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
    ''' <returns>The new file.</returns>
    Public Overridable Async Function CreateFileAsync(name As String, content As Stream, contentType As String, totalFileSize As Long) As Task(Of IFile) Implements IFolder.CreateFileAsync
        Dim fileName As String = System.IO.Path.Combine(fileSystemInfo.FullName, name)
        Using stream As FileStream = New FileStream(fileName, FileMode.CreateNew)
             End Using

        Dim file As DavFile = CType(Await context.GetHierarchyItemAsync(Path & EncodeUtil.EncodeUrlPart(name)), DavFile)
        ' write file content
        Await file.WriteInternalAsync(content, contentType, 0, totalFileSize)
        Return file
    End Function

    ''' <summary>
    ''' Called when a new folder is being created in this folder.
    ''' </summary>
    ''' <param name="name">Name of the new folder.</param>
    Overridable Public Async Function CreateFolderAsync(name As String) As Task(Of IFolder) Implements IFolder.CreateFolderAsync
        Await CreateFolderInternalAsync(name)
        Dim folder As DavFolder = CType(Await context.GetHierarchyItemAsync(Path & EncodeUtil.EncodeUrlPart(name)), DavFolder)
        Return folder
    End Function

    ''' <summary>
    ''' Called when a new folder is being created in this folder.
    ''' </summary>
    ''' <param name="name">Name of the new folder.</param>
    Private Async Function CreateFolderInternalAsync(name As String) As Task
        Dim isRoot As Boolean = dirInfo.Parent Is Nothing
        Dim di As DirectoryInfo = If(isRoot, New DirectoryInfo("\\?\" & context.RepositoryPath.TrimEnd(System.IO.Path.DirectorySeparatorChar)), dirInfo)
        di.CreateSubdirectory(name)
    End Function

    ''' <summary>
    ''' Called when this folder is being copied.
    ''' </summary>
    ''' <param name="destFolder">Destination parent folder.</param>
    ''' <param name="destName">New folder name.</param>
    ''' <param name="deep">Whether children items shall be copied.</param>
    ''' <param name="multistatus">Information about child items that failed to copy.</param>
    Public Overrides Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItem.CopyToAsync
        Await CopyToInternalAsync(destFolder, destName, deep, multistatus, 0)
    End Function

    ''' <summary>
    ''' Called when this folder is being copied.
    ''' </summary>
    ''' <param name="destFolder">Destination parent folder.</param>
    ''' <param name="destName">New folder name.</param>
    ''' <param name="deep">Whether children items shall be copied.</param>
    ''' <param name="multistatus">Information about child items that failed to copy.</param>
    ''' <param name="recursionDepth">Recursion depth.</param>
    Public Overrides Async Function CopyToInternalAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException, recursionDepth As Integer) As Task
        If Not(TypeOf destFolder Is DavFolder) Then
            Throw New DavException("Target folder doesn't exist", DavStatus.CONFLICT)
        End If

        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If IsRecursive(targetFolder) Then
            Throw New DavException("Cannot copy to subfolder", DavStatus.FORBIDDEN)
        End If

        Dim newDirLocalPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        ' Create folder at the destination.
        Try
            If Not Directory.Exists(newDirLocalPath) Then
                Await targetFolder.CreateFolderInternalAsync(destName)
            End If
        Catch ex As DavException
            ' Continue, but report error to client for the target item.
            multistatus.AddInnerException(targetPath, ex)
        End Try

        ' Copy children.
        Dim createdFolder As IFolder = CType(Await context.GetHierarchyItemAsync(targetPath), IFolder)
        For Each item As DavHierarchyItem In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
            If Not deep AndAlso TypeOf item Is DavFolder Then
                Continue For
            End If

            Try
                Await item.CopyToInternalAsync(createdFolder, item.Name, deep, multistatus, recursionDepth + 1)
            Catch ex As DavException
                ' If a child item failed to copy we continue but report error to client.
                multistatus.AddInnerException(item.Path, ex)
            End Try
        Next
    End Function

    ''' <summary>
    ''' Called when this folder is being moved or renamed.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New name of this folder.</param>
    ''' <param name="multistatus">Information about child items that failed to move.</param>
    Public Overrides Async Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItem.MoveToAsync
        Await MoveToInternalAsync(destFolder, destName, multistatus, 0)
    End Function

    ''' <summary>
    ''' Called when this folder is being moved or renamed.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New name of this folder.</param>
    ''' <param name="multistatus">Information about child items that failed to move.</param>
    Public Overrides Async Function MoveToInternalAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException, recursionDepth As Integer) As Task
        ' in this function we move item by item, because we want to check if each item is not locked.
        If Not(TypeOf destFolder Is DavFolder) Then
            Throw New DavException("Target folder doesn't exist", DavStatus.CONFLICT)
        End If

        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If IsRecursive(targetFolder) Then
            Throw New DavException("Cannot move folder to its subtree.", DavStatus.FORBIDDEN)
        End If

        Dim newDirPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        Try
            ' Remove item with the same name at destination if it exists.
            Dim item As DavHierarchyItem = TryCast(Await context.GetHierarchyItemAsync(targetPath), DavHierarchyItem)
            If item IsNot Nothing Then Await item.DeleteInternalAsync(multistatus, recursionDepth + 1)
            Await targetFolder.CreateFolderInternalAsync(destName)
        Catch ex As DavException
            ' Continue the operation but report error with destination path to client.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        ' Move child items.
        Dim movedSuccessfully As Boolean = True
        Dim createdFolder As IFolder = CType(Await context.GetHierarchyItemAsync(targetPath), IFolder)
        For Each item As DavHierarchyItem In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
            Try
                Await item.MoveToInternalAsync(createdFolder, item.Name, multistatus, recursionDepth + 1)
            Catch ex As DavException
                ' Continue the operation but report error with child item to client.
                multistatus.AddInnerException(item.Path, ex)
                movedSuccessfully = False
            End Try
        Next

        If movedSuccessfully Then
            Await DeleteInternalAsync(multistatus, recursionDepth + 1)
        End If
    End Function

    ''' <summary>
    ''' Called whan this folder is being deleted.
    ''' </summary>
    ''' <param name="multistatus">Information about items that failed to delete.</param>
    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync
        Await DeleteInternalAsync(multistatus, 0)
    End Function

    ''' <summary>
    ''' Called whan this folder is being deleted.
    ''' </summary>
    ''' <param name="multistatus">Information about items that failed to delete.</param>
    ''' <param name="recursionDepth">Recursion depth.</param>
    Public Overrides Async Function DeleteInternalAsync(multistatus As MultistatusException, recursionDepth As Integer) As Task
        Dim allChildrenDeleted As Boolean = True
        For Each child As DavHierarchyItem In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
            Try
                Await child.DeleteInternalAsync(multistatus, recursionDepth + 1)
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

    ''' <summary>
    ''' Determines whether <paramref name="destFolder"/>  is inside this folder.
    ''' </summary>
    ''' <param name="destFolder">Folder to check.</param>
    ''' <returns>Returns <c>true</c> if <paramref name="destFolder"/>  is inside this folder.</returns>
    Private Function IsRecursive(destFolder As DavFolder) As Boolean
        Return destFolder.Path.StartsWith(Path)
    End Function

    ''' <summary>
    ''' Sorts array of FileSystemInfo according to the specified order.
    ''' </summary>
    ''' <param name="fileInfos">Array of files and folders to sort.</param>
    ''' <param name="orderProps">Sorting order.</param>
    ''' <returns>Sorted list of files and folders.</returns>
    Private Function SortChildren(fileInfos As FileSystemInfo(), orderProps As IList(Of OrderProperty)) As FileSystemInfo()
        If orderProps IsNot Nothing AndAlso orderProps.Count() <> 0 Then
            ' map DAV properties to FileSystemInfo 
            Dim mappedProperties As Dictionary(Of String, String) = New Dictionary(Of String, String)() From {{"displayname", "Name"}, {"getlastmodified", "LastWriteTime"}, {"getcontenttype", "Extension"},
                                                                                                             {"quota-used-bytes", "ContentLength"}, {"is-directory", "IsDirectory"}}
            If orderProps.Count <> 0 Then
                Dim orderedFileInfos As IOrderedEnumerable(Of FileSystemInfo) = fileInfos.OrderBy(Function(p) p.Name)
                Dim index As Integer = 0
                For Each ordProp As OrderProperty In orderProps
                    Dim propertyName As String = mappedProperties(ordProp.Property.Name)
                    Dim sortFunc As Func(Of FileSystemInfo, Object) = Function(p) p.Name
                    Dim propertyInfo As PropertyInfo =(GetType(FileSystemInfo)).GetProperties().FirstOrDefault(Function(p) p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                    If propertyInfo IsNot Nothing Then
                        sortFunc = Function(p) p.GetType().GetProperty(propertyInfo.Name).GetValue(p)
                    ElseIf propertyName = "IsDirectory" Then
                        sortFunc = Function(p) p.IsDirectory()
                    ElseIf propertyName = "ContentLength" Then
                        sortFunc = Function(p) If(TypeOf p Is FileInfo, CType(p, FileInfo).Length, 0)
                    End If

                    If Math.Min(System.Threading.Interlocked.Increment(index), index - 1) = 0 Then
                        If ordProp.Ascending Then orderedFileInfos = fileInfos.OrderBy(sortFunc) Else orderedFileInfos = fileInfos.OrderByDescending(sortFunc)
                    Else
                        If ordProp.Ascending Then orderedFileInfos = orderedFileInfos.ThenBy(sortFunc) Else orderedFileInfos = orderedFileInfos.ThenByDescending(sortFunc)
                    End If
                Next

                fileInfos = orderedFileInfos.ToArray()
            End If
        End If

        Return fileInfos
    End Function
End Class
