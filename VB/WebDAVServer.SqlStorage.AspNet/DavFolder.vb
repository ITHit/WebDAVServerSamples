Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.Quota
Imports ITHit.WebDAV.Server.Search

''' <summary>
''' Represents folder in webdav repository.
''' </summary>
Public Class DavFolder
    Inherits DavHierarchyItem
    Implements IFolderAsync, IQuotaAsync, ISearchAsync

    ''' <summary>
    ''' Initializes a new instance of the <see cref="DavFolder"/>  class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
    ''' <param name="itemId">Id of this folder.</param>
    ''' <param name="parentId">Id of parent folder.</param>
    ''' <param name="name">Name of this folder.</param>
    ''' <param name="path">Encoded WebDAV path to this folder.</param>
    ''' <param name="created">Date when the folder was created.</param>
    ''' <param name="modified">Date when the folder was modified.</param>
    ''' <param name="fileAttributes">File attributes of the folder (hidden, read-only etc.)</param>
    Public Sub New(context As DavContext,
                  itemId As Guid,
                  parentId As Guid,
                  name As String,
                  path As String,
                  created As DateTime,
                  modified As DateTime, fileAttributes As FileAttributes)
        MyBase.New(context, itemId, parentId, name, path, created, modified, fileAttributes)
    End Sub

    ''' <summary>
    ''' Gets child items of this folder (files or folders).
    ''' </summary>
    ''' <param name="props">
    ''' List of properties which will be requested from children later. We don't use it here.
    ''' </param>
    ''' <returns>Enumerable with files and folders.</returns>
    Public Async Function GetChildrenAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
        Dim command As String = "SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified                    , FileAttributes                   FROM Item
                   WHERE ParentItemId = @Parent"
        Return Await Context.ExecuteItemAsync(Of IHierarchyItemAsync)(Path,
                                                                     command,
                                                                     "@Parent", ItemId)
    End Function

    ''' <summary>
    ''' Creates file with specified name in this folder.
    ''' </summary>
    ''' <param name="name">File name.</param>
    ''' <returns>Instance of <see cref="File"/>  referring to newly created file.</returns>
    Public Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Dim child = Await createChildAsync(name, ItemType.File)
        Await Context.socketService.NotifyRefreshAsync(Path)
        Return CType(child, IFileAsync)
    End Function

    ''' <summary>
    ''' Creates folder with specified name in this folder.
    ''' </summary>
    ''' <param name="name">Name of folder to be created.</param>
    Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Await createChildAsync(name, ItemType.Folder)
        Await Context.socketService.NotifyRefreshAsync(Path)
    End Function

    ''' <summary>
    ''' Copies this folder to another folder with option to rename it.
    ''' </summary>
    ''' <param name="destFolder">Folder to copy this folder to.</param>
    ''' <param name="destName">New name of this folder.</param>
    ''' <param name="deep">Whether children shall be copied.</param>
    ''' <param name="multistatus">Container for errors. We put here errors which occur with
    ''' individual items being copied.</param>
    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync,
                                               destName As String,
                                               deep As Boolean,
                                               multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Dim destDavFolder As DavFolder = TryCast(destFolder, DavFolder)
        If destFolder Is Nothing Then
            Throw New DavException("Destination folder doesn't exist", DavStatus.CONFLICT)
        End If

        If Not Await destDavFolder.ClientHasTokenAsync() Then
            Throw New LockedException("Doesn't have token for destination folder.")
        End If

        If isRecursive(destDavFolder) Then
            Throw New DavException("Cannot copy folder to its subtree", DavStatus.FORBIDDEN)
        End If

        Dim destItem As IHierarchyItemAsync = Await destDavFolder.FindChildAsync(destName)
        If destItem IsNot Nothing Then
            Try
                Await destItem.DeleteAsync(multistatus)
            Catch ex As DavException
                multistatus.AddInnerException(destItem.Path, ex)
                Return
            End Try
        End If

        Dim newDestFolder As DavFolder = Await CopyThisItemAsync(destDavFolder, Nothing, destName)
        ' copy children
        If deep Then
            For Each child As IHierarchyItemAsync In Await GetChildrenAsync(New PropertyName(-1) {})
                Dim dbchild = TryCast(child, DavHierarchyItem)
                Try
                    Await dbchild.CopyToAsync(newDestFolder, child.Name, deep, multistatus)
                Catch ex As DavException
                    multistatus.AddInnerException(dbchild.Path, ex)
                End Try
            Next
        End If

        Await Context.socketService.NotifyRefreshAsync(newDestFolder.Path)
    End Function

    ''' <summary>
    ''' Moves this folder to destination folder with option to rename.
    ''' </summary>
    ''' <param name="destFolder">Folder to copy this folder to.</param>
    ''' <param name="destName">New name of this folder.</param>
    ''' <param name="multistatus">Container for errors. We put here errors occurring while moving
    ''' individual files/folders.</param>
    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Dim destDavFolder As DavFolder = TryCast(destFolder, DavFolder)
        If destFolder Is Nothing Then
            Throw New DavException("Destination folder doesn't exist", DavStatus.CONFLICT)
        End If

        If isRecursive(destDavFolder) Then
            Throw New DavException("Cannot move folder to its subtree", DavStatus.FORBIDDEN)
        End If

        Dim parent As DavFolder = Await GetParentAsync()
        If parent Is Nothing Then
            Throw New DavException("Cannot move root", DavStatus.CONFLICT)
        End If

        If Not Await ClientHasTokenAsync() OrElse Not Await destDavFolder.ClientHasTokenAsync() OrElse Not Await parent.ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Dim destItem As DavHierarchyItem = Await destDavFolder.FindChildAsync(destName)
        Dim newDestFolder As DavFolder
        ' copy this folder
        If destItem IsNot Nothing Then
            If TypeOf destItem Is IFileAsync Then
                Try
                    Await destItem.DeleteAsync(multistatus)
                Catch ex As DavException
                    multistatus.AddInnerException(destItem.Path, ex)
                    Return
                End Try

                newDestFolder = Await CopyThisItemAsync(destDavFolder, Nothing, destName)
            Else
                newDestFolder = TryCast(destItem, DavFolder)
                If newDestFolder Is Nothing Then
                    multistatus.AddInnerException(destItem.Path,
                                                 New DavException("Destionation item is not folder", DavStatus.CONFLICT))
                End If
            End If
        Else
            newDestFolder = Await CopyThisItemAsync(destDavFolder, Nothing, destName)
        End If

        ' move children
        Dim movedAllChildren As Boolean = True
        For Each child As IHierarchyItemAsync In Await GetChildrenAsync(New PropertyName(-1) {})
            Dim dbchild As DavHierarchyItem = TryCast(child, DavHierarchyItem)
            Try
                Await dbchild.MoveToAsync(newDestFolder, child.Name, multistatus)
            Catch ex As DavException
                multistatus.AddInnerException(dbchild.Path, ex)
                movedAllChildren = False
            End Try
        Next

        If movedAllChildren Then
            Await DeleteThisItemAsync(parent)
        End If

        ' Refresh client UI.
        Await Context.socketService.NotifyDeleteAsync(Path)
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(newDestFolder.Path))
    End Function

    ''' <summary>
    ''' Deletes this folder.
    ''' </summary>
    ''' <param name="multistatus">Container for errors.
    ''' If some child file/folder fails to remove we report error in this container.</param>
    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        Dim parent As DavFolder = Await GetParentAsync()
        If parent Is Nothing Then
            Throw New DavException("Cannot delete root.", DavStatus.CONFLICT)
        End If

        If Not Await parent.ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Dim deletedAllChildren As Boolean = True
        For Each child As IHierarchyItemAsync In Await GetChildrenAsync(New PropertyName(-1) {})
            Dim dbchild As DavHierarchyItem = TryCast(child, DavHierarchyItem)
            Try
                Await dbchild.DeleteAsync(multistatus)
            Catch ex As DavException
                multistatus.AddInnerException(dbchild.Path, ex)
                deletedAllChildren = False
            End Try
        Next

        If deletedAllChildren Then
            Await DeleteThisItemAsync(parent)
            Await Context.socketService.NotifyDeleteAsync(Path)
        End If
    End Function

    ''' <summary>
    ''' Returns free bytes available to current user.
    ''' </summary>
    ''' <returns>Free bytes available.</returns>
    Public Async Function GetAvailableBytesAsync() As Task(Of Long) Implements IQuotaAsync.GetAvailableBytesAsync
        'let's assume total space is 5GB.
        Return(5L * 1024 * 1024 * 1024) - Await GetUsedBytesAsync()
    End Function

    ''' <summary>
    ''' Returns used bytes by current user.
    ''' </summary>
    ''' <returns>Number of bytes consumed by files of current user.</returns>
    Public Async Function GetUsedBytesAsync() As Task(Of Long) Implements IQuotaAsync.GetUsedBytesAsync
        Return Await Context.ExecuteScalarAsync(Of Long)("SELECT SUM(DATALENGTH(Content)) FROM Item")
    End Function

    ''' <summary>
    ''' Searches files and folders in current folder using search phrase and options.
    ''' </summary>
    ''' <param name="searchString">A phrase to search.</param>
    ''' <param name="options">Search options.</param>
    ''' <param name="propNames">
    ''' List of properties to retrieve with each item returned by this method. They will be requested by the 
    ''' Engine in <see cref="IHierarchyItemAsync.GetPropertiesAsync(IList{PropertyName}, bool)"/>  call.
    ''' </param>
    ''' <returns>List of <see cref="IHierarchyItemAsync"/>  satisfying search request.</returns>
    Public Async Function SearchAsync(searchString As String, options As SearchOptions, propNames As List(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements ISearchAsync.SearchAsync
        Dim includeSnippet As Boolean = propNames.Any(Function(s) s.Name = SNIPPET)
        Dim condition As String = "Name LIKE @Name"
        ' To enable full-text search, uncoment the code below and follow instructions 
        ' in DB.sql to enable full-text indexing
        Dim commandText As String = [String].Format("SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified
                    , FileAttributes                          
                   FROM Item
                   WHERE ParentItemId = @Parent AND ({0})", condition)
        Dim result As List(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
        Await GetSearchResultsAsync(result, commandText, searchString, includeSnippet)
        Return result
    End Function

    ''' <summary>
    ''' Produces recursive search in current folder.
    ''' </summary>
    ''' <param name="result">A list to add search results to.</param>
    ''' <param name="commandText">SQL command text for search in a folder.</param>
    ''' <param name="searchString">A phrase to search.</param>
    Private Async Function GetSearchResultsAsync(result As List(Of IHierarchyItemAsync), commandText As String, searchString As String, includeSnippet As Boolean) As Task
        ' search this folder
        Dim folderSearchResults As IEnumerable(Of IHierarchyItemAsync) = Await Context.ExecuteItemAsync(Of IHierarchyItemAsync)(Path,
                                                                                                                               commandText,
                                                                                                                               "@Parent", ItemId,
                                                                                                                               "@Name", searchString,
                                                                                                                               "@Content", searchString)
        For Each item As IHierarchyItemAsync In folderSearchResults
            If includeSnippet AndAlso TypeOf item Is DavFile Then TryCast(item, DavFile).Snippet = "Not Implemented"
        Next

        result.AddRange(folderSearchResults)
        ' search children
        For Each item As IHierarchyItemAsync In Await GetChildrenFoldersAsync()
            Dim folder As DavFolder = TryCast(item, DavFolder)
            If folder IsNot Nothing Then Await folder.GetSearchResultsAsync(result, commandText, searchString, includeSnippet)
        Next
    End Function

    ''' <summary>
    ''' Gets the children of current folder (non-recursive).
    ''' </summary>
    ''' <returns>The children folders of current folder.</returns>
    Public Async Function GetChildrenFoldersAsync() As Task(Of IEnumerable(Of IHierarchyItemAsync))
        Dim command As String = "SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified
                    , FileAttributes
                   FROM Item
                   WHERE ParentItemId = @Parent AND ItemType = 3"
        Return Await Context.ExecuteItemAsync(Of IHierarchyItemAsync)(Path,
                                                                     command,
                                                                     "@Parent", ItemId)
    End Function

    ''' <summary>
    ''' Finds file or folder with specified name inside this folder.
    ''' </summary>
    ''' <param name="childName">Name of child to find.</param>
    ''' <returns>Instance of <see cref="DavHierarchyItem"/>  or <c>null</c>.</returns>
    Friend Async Function FindChildAsync(childName As String) As Task(Of DavHierarchyItem)
        Dim commandText As String = "SELECT
                     ItemId
                   , ParentItemId
                   , ItemType
                   , Name
                   , Created
                   , Modified 
                   , FileAttributes  
                  FROM Item
                  WHERE ParentItemId = @Parent
                  AND Name = @Name"
        Dim davHierarchyItems As IList(Of DavHierarchyItem) = Await Context.ExecuteItemAsync(Of DavHierarchyItem)(Path,
                                                                                                                 commandText,
                                                                                                                 "@Parent", ItemId,
                                                                                                                 "@Name", childName)
        Return davHierarchyItems.FirstOrDefault()
    End Function

    ''' <summary>
    ''' Determines whether the client has submitted lock tokens for all locked files in the subtree.
    ''' </summary>
    ''' <returns>Returns <c>true</c> if lock tockens for all locked files in the subtree are submitted.</returns>
    Friend Async Function ClientHasTokenForTreeAsync() As Task(Of Boolean)
        If Not Await ClientHasTokenAsync() Then
            Return False
        End If

        For Each child As IHierarchyItemAsync In Await GetChildrenAsync(New PropertyName(-1) {})
            Dim childFolder As DavFolder = TryCast(child, DavFolder)
            If childFolder IsNot Nothing Then
                If Not Await childFolder.ClientHasTokenForTreeAsync() Then
                    Return False
                End If
            Else
                Dim childItem As DavHierarchyItem = TryCast(child, DavHierarchyItem)
                If Not Await childItem.ClientHasTokenAsync() Then
                    Return False
                End If
            End If
        Next

        Return True
    End Function

    ''' <summary>
    ''' Determines whether <paramref name="destFolder"/>  is inside this folder.
    ''' </summary>
    ''' <param name="destFolder">Folder to test.</param>
    ''' <returns>Returns <c>true</c>if <paramref name="destFolder"/>  is inside this folder.</returns>
    Private Function isRecursive(destFolder As DavFolder) As Boolean
        Return destFolder.Path.StartsWith(Path)
    End Function

    ''' <summary>
    ''' Creates file or folder with specified name inside this folder.
    ''' </summary>
    ''' <param name="name">File/folder name.</param>
    ''' <param name="itemType">Type of item: file or folder.</param>
    ''' <returns>Newly created item.</returns>
    Private Async Function createChildAsync(name As String, itemType As ItemType) As Task(Of DavHierarchyItem)
        Dim newID As Guid = Guid.NewGuid()
        Dim commandText As String = "INSERT INTO Item(
                      ItemId
                    , Name
                    , Created
                    , Modified
                    , ParentItemId
                    , ItemType
                    , TotalContentLength
                    , FileAttributes
                    )
                VALUES(
                     @Identity
                   , @Name
                   , GETUTCDATE()
                   , GETUTCDATE()
                   , @Parent
                   , @ItemType
                   , 0
                   , @FileAttributes
                   )"
        Await Context.ExecuteNonQueryAsync(commandText,
                                          "@Name", name,
                                          "@Parent", ItemId,
                                          "@ItemType", itemType,
                                          "@FileAttributes", (If(itemType = ItemType.Folder, CInt(FileAttributes.Directory), CInt(FileAttributes.Normal))),
                                          "@Identity", newID)
        'UpdateModified(); do not update time for folder as transaction will block concurrent files upload
        Select Case itemType
            Case ItemType.File
                Return New DavFile(Context,
                                  newID,
                                  ItemId,
                                  name,
                                  Path & EncodeUtil.EncodeUrlPart(name),
                                  DateTime.UtcNow,
                                  DateTime.UtcNow, FileAttributes.Normal)
            Case ItemType.Folder
                ' do not need to return created folder
                Return Nothing
            Case Else
                Return Nothing
        End Select
    End Function
End Class
