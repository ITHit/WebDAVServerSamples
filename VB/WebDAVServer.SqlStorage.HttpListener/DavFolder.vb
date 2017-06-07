Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.Search

''' <summary>
''' Represents folder in webdav repository.
''' </summary>
Public Class DavFolder
    Inherits DavHierarchyItem
    Implements IFolderAsync, ISearchAsync

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
                  modified As DateTime)
        MyBase.New(context, itemId, parentId, name, path, created, modified)
    End Sub

    Public Async Function GetChildrenAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
        Dim command As String = "SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified                  FROM Item
                   WHERE ParentItemId = @Parent"
        Return Await Context.ExecuteItemAsync(Of IHierarchyItemAsync)(Path,
                                                                     command,
                                                                     "@Parent", ItemId)
    End Function

    Public Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Dim child = Await createChildAsync(name, ItemType.File)
        Await Context.socketService.NotifyRefreshAsync(Path)
        Return CType(child, IFileAsync)
    End Function

    Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Await createChildAsync(name, ItemType.Folder)
        Await Context.socketService.NotifyRefreshAsync(Path)
    End Function

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

        Await Context.socketService.NotifyDeleteAsync(Path)
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(newDestFolder.Path))
    End Function

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

    Public Async Function SearchAsync(searchString As String, options As SearchOptions, propNames As List(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements ISearchAsync.SearchAsync
        Dim includeSnippet As Boolean = propNames.Any(Function(s) s.Name = SNIPPET)
        Dim condition As String = "Name LIKE @Name"
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

    Private Async Function GetSearchResultsAsync(result As List(Of IHierarchyItemAsync), commandText As String, searchString As String, includeSnippet As Boolean) As Task
        Dim folderSearchResults As IEnumerable(Of IHierarchyItemAsync) = Await Context.ExecuteItemAsync(Of IHierarchyItemAsync)(Path,
                                                                                                                               commandText,
                                                                                                                               "@Parent", ItemId,
                                                                                                                               "@Name", searchString,
                                                                                                                               "@Content", searchString)
        For Each item As IHierarchyItemAsync In folderSearchResults
            If includeSnippet AndAlso TypeOf item Is DavFile Then TryCast(item, DavFile).Snippet = "Not Implemented"
        Next

        result.AddRange(folderSearchResults)
        For Each item As IHierarchyItemAsync In Await GetChildrenFoldersAsync()
            Dim folder As DavFolder = TryCast(item, DavFolder)
            If folder IsNot Nothing Then Await folder.GetSearchResultsAsync(result, commandText, searchString, includeSnippet)
        Next
    End Function

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

    Friend Async Function FindChildAsync(childName As String) As Task(Of DavHierarchyItem)
        Dim commandText As String = "SELECT
                     ItemId
                   , ParentItemId
                   , ItemType
                   , Name
                   , Created
                   , Modified  
                  FROM Item
                  WHERE ParentItemId = @Parent
                  AND Name = @Name"
        Dim davHierarchyItems As IList(Of DavHierarchyItem) = Await Context.ExecuteItemAsync(Of DavHierarchyItem)(Path,
                                                                                                                 commandText,
                                                                                                                 "@Parent", ItemId,
                                                                                                                 "@Name", childName)
        Return davHierarchyItems.FirstOrDefault()
    End Function

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

    Private Function isRecursive(destFolder As DavFolder) As Boolean
        Return destFolder.Path.StartsWith(Path)
    End Function

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
                    )
                VALUES(
                     @Identity
                   , @Name
                   , GETUTCDATE()
                   , GETUTCDATE()
                   , @Parent
                   , @ItemType
                   , 0
                   )"
        Await Context.ExecuteNonQueryAsync(commandText,
                                          "@Name", name,
                                          "@Parent", ItemId,
                                          "@ItemType", itemType,
                                          "@Identity", newID)
        Select Case itemType
            Case ItemType.File
                Return New DavFile(Context,
                                  newID,
                                  ItemId,
                                  name,
                                  Path & EncodeUtil.EncodeUrlPart(name),
                                  DateTime.UtcNow,
                                  DateTime.UtcNow)
            Case ItemType.Folder
                Return Nothing
            Case Else
                Return Nothing
        End Select
    End Function
End Class
