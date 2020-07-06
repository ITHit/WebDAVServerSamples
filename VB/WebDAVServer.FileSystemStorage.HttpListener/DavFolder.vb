Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data.OleDb
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes
Imports ITHit.WebDAV.Server.Search
Imports ITHit.WebDAV.Server.ResumableUpload
Imports ITHit.WebDAV.Server.Paging

''' <summary>
''' Folder in WebDAV repository.
''' </summary>
Public Class DavFolder
    Inherits DavHierarchyItem
    Implements IFolderAsync, ISearchAsync, IResumableUploadBase

    ''' <summary>
    ''' Windows Search Provider string.
    ''' </summary>
    Private Shared ReadOnly windowsSearchProvider As String = ConfigurationManager.AppSettings("WindowsSearchProvider")

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
    Public Overridable Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollectionAsync.GetChildrenAsync
        ' Enumerates all child files and folders.
        ' You can filter children items in this implementation and 
        ' return only items that you want to be visible for this 
        ' particular user.
        Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
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
            Dim child As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(childPath)
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
    ''' <returns>The new file.</returns>
    Public Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
        Await RequireHasTokenAsync()
        Dim fileName As String = System.IO.Path.Combine(fileSystemInfo.FullName, name)
        Using stream As FileStream = New FileStream(fileName, FileMode.CreateNew)
             End Using

        Await context.socketService.NotifyRefreshAsync(Path)
        Return CType(Await context.GetHierarchyItemAsync(Path & EncodeUtil.EncodeUrlPart(name)), IFileAsync)
    End Function

    ''' <summary>
    ''' Called when a new folder is being created in this folder.
    ''' </summary>
    ''' <param name="name">Name of the new folder.</param>
    Overridable Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
        Await RequireHasTokenAsync()
        dirInfo.CreateSubdirectory(name)
        Await context.socketService.NotifyRefreshAsync(Path)
    End Function

    ''' <summary>
    ''' Called when this folder is being copied.
    ''' </summary>
    ''' <param name="destFolder">Destination parent folder.</param>
    ''' <param name="destName">New folder name.</param>
    ''' <param name="deep">Whether children items shall be copied.</param>
    ''' <param name="multistatus">Information about child items that failed to copy.</param>
    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
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
                Await targetFolder.CreateFolderAsync(destName)
            End If
        Catch ex As DavException
            ' Continue, but report error to client for the target item.
            multistatus.AddInnerException(targetPath, ex)
        End Try

        ' Copy children.
        Dim createdFolder As IFolderAsync = CType(Await context.GetHierarchyItemAsync(targetPath), IFolderAsync)
        For Each item As DavHierarchyItem In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
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

        Await context.socketService.NotifyRefreshAsync(targetFolder.Path)
    End Function

    ''' <summary>
    ''' Called when this folder is being moved or renamed.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New name of this folder.</param>
    ''' <param name="multistatus">Information about child items that failed to move.</param>
    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Await RequireHasTokenAsync()
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
            Dim item As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(targetPath)
            If item IsNot Nothing Then Await item.DeleteAsync(multistatus)
            Await targetFolder.CreateFolderAsync(destName)
        Catch ex As DavException
            ' Continue the operation but report error with destination path to client.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        ' Move child items.
        Dim movedSuccessfully As Boolean = True
        Dim createdFolder As IFolderAsync = CType(Await context.GetHierarchyItemAsync(targetPath), IFolderAsync)
        For Each item As DavHierarchyItem In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
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

        ' Refresh client UI.
        Await context.socketService.NotifyDeleteAsync(Path)
        Await context.socketService.NotifyRefreshAsync(GetParentPath(targetPath))
    End Function

    ''' <summary>
    ''' Called whan this folder is being deleted.
    ''' </summary>
    ''' <param name="multistatus">Information about items that failed to delete.</param>
    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        Await RequireHasTokenAsync()
        Dim allChildrenDeleted As Boolean = True
        For Each child As IHierarchyItemAsync In(Await GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, New List(Of OrderProperty)())).Page
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
            Await context.socketService.NotifyDeleteAsync(Path)
        End If
    End Function

    ''' <summary>
    ''' Searches files and folders in current folder using search phrase, offset, nResults and options.
    ''' </summary>
    ''' <param name="searchString">A phrase to search.</param>
    ''' <param name="options">Search options.</param>
    ''' <param name="propNames">
    ''' List of properties to retrieve with each item returned by this method. They will be requested by the 
    ''' Engine in <see cref="IHierarchyItemAsync.GetPropertiesAsync(IList{PropertyName}, bool)"/>  call.
    ''' </param>
    ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
    ''' <param name="nResults">The number of items to return.</param>
    ''' <returns>List of <see cref="IHierarchyItemAsync"/>  satisfying search request.</returns>1
    ''' <returns>Items satisfying search request and a total number.</returns>
    Public Async Function SearchAsync(searchString As String, options As SearchOptions, propNames As List(Of PropertyName), offset As Long?, nResults As Long?) As Task(Of PageResults) Implements ISearchAsync.SearchAsync
        Dim includeSnippet As Boolean = propNames.Any(Function(s) s.Name = snippetProperty)
        ' search both in file name and content
        Dim commandText As String = "SELECT System.ItemPathDisplay" & (If(includeSnippet, " ,System.Search.AutoSummary", String.Empty)) & " FROM SystemIndex " & "WHERE scope ='file:@Path' AND (System.ItemNameDisplay LIKE '@Name' OR FREETEXT('""@Content""')) " & "ORDER BY System.Search.Rank DESC"
        commandText = PrepareCommand(commandText,
                                    "@Path", Me.dirInfo.FullName,
                                    "@Name", searchString,
                                    "@Content", searchString)
        Dim foundItems As Dictionary(Of String, String) = New Dictionary(Of String, String)()
        Try
            Using connection As OleDbConnection = New OleDbConnection(windowsSearchProvider)
                Using command As OleDbCommand = New OleDbCommand(commandText, connection)
                    connection.Open()
                    Using reader As OleDbDataReader = command.ExecuteReader()
                        While Await reader.ReadAsync()
                            Dim snippet As String = String.Empty
                            If includeSnippet Then
                                snippet = If(reader.GetValue(1) <> DBNull.Value, reader.GetString(1), Nothing)
                                ' XML does not support control characters or permanently undefined Unicode characters. Removing them from snippet. https:'www.w3.org/TR/xml/#charsets
                                If Not String.IsNullOrEmpty(snippet) AndAlso invalidXmlCharsPattern.IsMatch(snippet) Then
                                    snippet = invalidXmlCharsPattern.Replace(snippet, [String].Empty)
                                End If
                            End If

                            foundItems.Add(reader.GetString(0), snippet)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As OleDbException
            context.Logger.LogError(ex.Message, ex)
            Select Case ex.ErrorCode
                Case -2147217900
                    Throw New DavException("Illegal symbols in search phrase.", DavStatus.CONFLICT)
                Case Else
                    Throw New DavException("Unknown error.", DavStatus.INTERNAL_ERROR)
            End Select

        End Try

        Dim subtreeItems As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
        For Each path As String In foundItems.Keys
            Dim item As IHierarchyItemAsync = TryCast(Await context.GetHierarchyItemAsync(GetRelativePath(path)), IHierarchyItemAsync)
            If item Is Nothing Then
                Continue For
            End If

            If includeSnippet AndAlso TypeOf item Is DavFile Then TryCast(item, DavFile).Snippet = HighlightKeywords(searchString.Trim("%"c), foundItems(path))
            subtreeItems.Add(item)
        Next

        Return New PageResults(If(offset.HasValue AndAlso nResults.HasValue, subtreeItems.Skip(CInt(offset.Value)).Take(CInt(nResults.Value)), subtreeItems), subtreeItems.Count)
    End Function

    ''' <summary>
    ''' Converts path on disk to encoded relative path.
    ''' </summary>
    ''' <param name="filePath">Path returned by Windows Search.</param>
    ''' <remarks>
    ''' The Search.CollatorDSO provider returns "documents" as "my documents". 
    ''' There is no any real solution for this, so to build path we just replace "my documents" manually.
    ''' </remarks>
    ''' <returns>Returns relative encoded path for an item.</returns>
    Private Function GetRelativePath(filePath As String) As String
        Dim itemPath As String = filePath.ToLower().Replace("\my documents\", "\documents\")
        Dim repoPath As String = Me.fileSystemInfo.FullName.ToLower().Replace("\my documents\", "\documents\")
        Dim relPathLength As Integer = itemPath.Substring(repoPath.Length).TrimStart("\"c).Length
        Dim relPath As String = filePath.Substring(filePath.Length - relPathLength)
        Dim encodedParts As IEnumerable(Of String) = relPath.Split("\"c).Select(AddressOf EncodeUtil.EncodeUrlPart)
        Return Me.Path & [String].Join("/", encodedParts.ToArray())
    End Function

    ''' <summary>
    ''' Highlight the search terms in a text.
    ''' </summary>
    ''' <param name="keywords">Search keywords.</param>
    ''' <param name="text">File content.</param>
    Private Shared Function HighlightKeywords(searchTerms As String, text As String) As String
        Dim exp As Regex = New Regex("\b(" & String.Join("|", searchTerms.Split(New Char() {","c, " "c}, StringSplitOptions.RemoveEmptyEntries)) & ")\b",
                                    RegexOptions.IgnoreCase Or RegexOptions.Multiline)
        Return If(Not String.IsNullOrEmpty(text), exp.Replace(text, "<b>$0</b>"), text)
    End Function

    ''' <summary>
    ''' Inserts parameters into the command text.
    ''' </summary>
    ''' <param name="commandText">Command text.</param>
    ''' <param name="prms">Command parameters in pairs: name, value</param>
    ''' <returns>Command text with values inserted.</returns>
    ''' <remarks>
    ''' The ICommandWithParameters interface is not supported by the 'Search.CollatorDSO' provider.
    ''' </remarks>
    Private Function PrepareCommand(commandText As String, ParamArray prms As Object()) As String
        If prms.Length Mod 2 <> 0 Then Throw New ArgumentException("Incorrect number of parameters")
        For i As Integer = 0 To prms.Length - 1 Step 2
            If Not(TypeOf prms(i) Is String) Then Throw New ArgumentException(prms(i) & "is invalid parameter name")
            Dim value As String = CStr(prms(i + 1))
            ' Search.CollatorDSO provider ignores ' and " chars, but we will remove them anyway
            value = value.Replace("""", [String].Empty)
            value = value.Replace("'", [String].Empty)
            commandText = commandText.Replace(CStr(prms(i)), value)
        Next

        Return commandText
    End Function

    ''' <summary>
    ''' Determines whether <paramref name="destFolder"/>  is inside this folder.
    ''' </summary>
    ''' <param name="destFolder">Folder to check.</param>
    ''' <returns>Returns <c>true</c> if <paramref name="destFolder"/>  is inside thid folder.</returns>
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
