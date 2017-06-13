Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.MicrosoftExtensions

''' <summary>
''' Base class for items like files, folders, versions etc.
''' </summary>
Public MustInherit Class DavHierarchyItem
    Implements IHierarchyItemAsync, ILockAsync, IMsItemAsync

    ''' <summary>
    ''' Property name to return text anound search phrase.
    ''' </summary>
    Friend Const SNIPPET As String = "snippet"

    Protected Property Context As DavContext

    Public Property ItemId As Guid

    Public Property Name As String Implements IHierarchyItemAsync.Name

    Public Property Path As String Implements IHierarchyItemAsync.Path

    Public Property Created As DateTime Implements IHierarchyItemAsync.Created

    Public Property Modified As DateTime Implements IHierarchyItemAsync.Modified

    Protected Property ParentId As Guid

    Private fileAttributes As FileAttributes

    Public Sub New(context As DavContext,
                  itemId As Guid,
                  parentId As Guid,
                  name As String,
                  path As String,
                  created As DateTime,
                  modified As DateTime, fileAttributes As FileAttributes)
        Me.Context = context
        Me.ItemId = itemId
        Me.ParentId = parentId
        Me.Name = name
        Me.Path = path
        Me.Created = created
        Me.Modified = modified
        Me.fileAttributes = fileAttributes
    End Sub

    Public Async Function GetParentAsync() As Task(Of DavFolder)
        Dim parts As String() = Path.Trim("/"c).Split("/"c)
        Dim parentParentPath As String = "/"
        If parts.Length >= 2 Then
            parentParentPath = String.Join("/", parts, 0, parts.Length - 2) & "/"
            Dim command As String = "SELECT 
                     ItemID
                   , ParentItemId
                   , ItemType
                   , Name
                   , Created
                   , Modified, FileAttributes                  
                  FROM Item
                  WHERE ItemId = @ItemId"
            Dim davFolders As IList(Of DavFolder) = Await Context.ExecuteItemAsync(Of DavFolder)(parentParentPath,
                                                                                                command,
                                                                                                "@ItemId", ParentId)
            Return davFolders.FirstOrDefault()
        Else
            Return TryCast(Await Context.getRootFolderAsync(), DavFolder)
        End If
    End Function

    Public MustOverride Function CopyToAsync(destFolder As IItemCollectionAsync,
                                            destName As String,
                                            deep As Boolean,
                                            multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync

    Public MustOverride Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync

    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync

    Public Async Function GetPropertiesAsync(names As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
        Dim requestedPropVals As IList(Of PropertyValue) = New List(Of PropertyValue)()
        Dim propVals As IList(Of PropertyValue) = Await Context.ExecutePropertyValueAsync("SELECT Name, Namespace, PropVal FROM Property WHERE ItemID = @ItemID",
                                                                                         "@ItemID", ItemId)
        Dim snippetProperty As PropertyName = names.FirstOrDefault(Function(s) s.Name = SNIPPET)
        If allprop Then
            requestedPropVals = propVals
        Else
            For Each p As PropertyValue In propVals
                If names.Contains(p.QualifiedName) Then
                    requestedPropVals.Add(p)
                End If
            Next
        End If

        If snippetProperty.Name = SNIPPET AndAlso TypeOf Me Is DavFile Then requestedPropVals.Add(New PropertyValue(snippetProperty, TryCast(Me, DavFile).Snippet))
        Return requestedPropVals
    End Function

    Public Overridable Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue),
                                                           delProps As IList(Of PropertyName),
                                                           multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
        Await RequireHasTokenAsync()
        For Each p As PropertyValue In setProps
            ' Microsoft Mini-redirector may update file creation date, modification date and access time passing properties:
            ' <Win32CreationTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:15:34 GMT</Win32CreationTime>
            ' <Win32LastModifiedTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastModifiedTime>
            ' <Win32LastAccessTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastAccessTime>
            ' In this case update creation and modified date in your storage or do not save this properties at all, otherwise 
            ' Windows Explorer will display creation and modification date from this props and it will differ from the values 
            ' in the Created and Modified fields in your storage 
            If p.QualifiedName.Namespace = "urn:schemas-microsoft-com:" Then
                If p.QualifiedName.Name = "Win32CreationTime" Then
                    Await SetDbFieldAsync("Created", DateTime.Parse(p.Value, New System.Globalization.CultureInfo("en-US")).ToUniversalTime())
                ElseIf p.QualifiedName.Name = "Win32LastModifiedTime" Then
                    Await SetDbFieldAsync("Modified", DateTime.Parse(p.Value, New System.Globalization.CultureInfo("en-US")).ToUniversalTime())
                End If
            Else
                Await SetPropertyAsync(p)
            End If
        Next

        For Each p As PropertyName In delProps
            Await RemovePropertyAsync(p.Name, p.Namespace)
        Next

        ' You should not update modification date/time here. Mac OS X Finder expects that properties update do not change the file modification date.
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(Path))
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
        Dim propNames As IList(Of PropertyName) = New List(Of PropertyName)()
        For Each propName As PropertyValue In Await GetPropertiesAsync(New PropertyName(-1) {}, True)
            propNames.Add(propName.QualifiedName)
        Next

        Return propNames
    End Function

    Protected Async Function RequireHasTokenAsync() As Task
        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If
    End Function

    Public Async Function LockAsync(level As LockLevel, isDeep As Boolean, timeout As TimeSpan?, owner As String) As Task(Of LockResult) Implements ILockAsync.LockAsync
        If Await ItemHasLockAsync(level = LockLevel.Shared) Then
            Throw New LockedException()
        End If

        If isDeep Then
            ' check if no items are locked in this subtree
            Await FindLocksDownAsync(Me, level = LockLevel.Shared)
        End If

        If Not timeout.HasValue OrElse timeout = TimeSpan.MaxValue Then
            ' If timeout is absent or infinity timeout requested,
            ' grant 5 minute lock.
            timeout = TimeSpan.FromMinutes(5)
        End If

        ' We store expiration time in UTC. If server/database is moved 
        ' to other time zone the locks expiration time is always correct.
        Dim expires As DateTime = DateTime.UtcNow + timeout.Value
        Dim token As String = Guid.NewGuid().ToString()
        Dim insertLockCommand As String = "INSERT INTO Lock (ItemID,Token,Shared,Deep,Expires,Owner)
                   VALUES(@ItemID, @Token, @Shared, @Deep, @Expires, @Owner)"
        Await Context.ExecuteNonQueryAsync(insertLockCommand,
                                          "@ItemID", ItemId,
                                          "@Token", token,
                                          "@Shared", level = LockLevel.Shared,
                                          "@Deep", isDeep,
                                          "@Expires", expires,
                                          "@Owner", owner)
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(Path))
        Return New LockResult(token, timeout.Value)
    End Function

    Public Async Function RefreshLockAsync(token As String, timeout As TimeSpan?) As Task(Of RefreshLockResult) Implements ILockAsync.RefreshLockAsync
        Dim activeLocks As IEnumerable(Of LockInfo) = Await GetActiveLocksAsync()
        Dim l As LockInfo = activeLocks.FirstOrDefault(Function(al) al.Token = token)
        If l Is Nothing Then
            Throw New DavException("The lock doesn't exist", DavStatus.PRECONDITION_FAILED)
        End If

        If Not timeout.HasValue OrElse timeout = TimeSpan.MaxValue Then
            ' If timeout is absent or infinity timeout requested,
            ' grant 5 minute lock.
            l.TimeOut = TimeSpan.FromMinutes(5)
        Else
            ' Otherwise use new timeout.
            l.TimeOut = timeout.Value
        End If

        Dim expires As DateTime = DateTime.UtcNow + CType(l.TimeOut, TimeSpan)
        Await Context.ExecuteNonQueryAsync("UPDATE Lock SET Expires = @Expires WHERE Token = @Token",
                                          "@Expires", expires,
                                          "@Token", token)
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(Path))
        Return New RefreshLockResult(l.Level, l.IsDeep, CType(l.TimeOut, TimeSpan), l.Owner)
    End Function

    Public Async Function UnlockAsync(lockToken As String) As Task Implements ILockAsync.UnlockAsync
        Dim activeLocks As IEnumerable(Of LockInfo) = Await GetActiveLocksAsync()
        If activeLocks.All(Function(al) al.Token <> lockToken) Then
            Throw New DavException("This lock token doesn't correspond to any lock", DavStatus.PRECONDITION_FAILED)
        End If

        ' remove lock from existing item
        Await Context.ExecuteNonQueryAsync("DELETE FROM Lock WHERE Token = @Token",
                                          "@Token", lockToken)
        Await Context.socketService.NotifyRefreshAsync(GetParentPath(Path))
    End Function

    Public Async Function GetActiveLocksAsync() As Task(Of IEnumerable(Of LockInfo)) Implements ILockAsync.GetActiveLocksAsync
        Dim entryId As Guid = ItemId
        Dim l As List(Of LockInfo) = New List(Of LockInfo)()
        l.AddRange(GetLocks(entryId, False))
        While True
            entryId = Await Context.ExecuteScalarAsync(Of Guid)("SELECT ParentItemId FROM Item WHERE ItemId = @ItemId",
                                                               "@ItemId", entryId)
            If entryId = Guid.Empty Then
                Exit While
            End If

            l.AddRange(GetLocks(entryId, True))
        End While

        Return l
    End Function

    Protected Async Function SetPropertyAsync(prop As PropertyValue) As Task
        Dim selectCommand As String = "SELECT Count(*) FROM Property
                  WHERE ItemID = @ItemID AND Name = @Name AND Namespace = @Namespace"
        Dim count As Integer = Await Context.ExecuteScalarAsync(Of Integer)(selectCommand,
                                                                           "@ItemID", ItemId,
                                                                           "@Name", prop.QualifiedName.Name,
                                                                           "@Namespace", prop.QualifiedName.Namespace)
        ' insert
        If count = 0 Then
            Dim insertCommand As String = "INSERT INTO Property(ItemID, Name, Namespace, PropVal)
                                          VALUES(@ItemID, @Name, @Namespace, @PropVal)"
            Await Context.ExecuteNonQueryAsync(insertCommand,
                                              "@PropVal", prop.Value,
                                              "@ItemID", ItemId,
                                              "@Name", prop.QualifiedName.Name,
                                              "@Namespace", prop.QualifiedName.Namespace)
        Else
            ' update
            Dim command As String = "UPDATE Property
                      SET PropVal = @PropVal
                      WHERE ItemID = @ItemID AND Name = @Name AND Namespace = @Namespace"
            Await Context.ExecuteNonQueryAsync(command,
                                              "@PropVal", prop.Value,
                                              "@ItemID", ItemId,
                                              "@Name", prop.QualifiedName.Name,
                                              "@Namespace", prop.QualifiedName.Namespace)
        End If
    End Function

    Protected Async Function RemovePropertyAsync(name As String, ns As String) As Task
        Dim command As String = "DELETE FROM Property
                              WHERE ItemID = @ItemID
                              AND Name = @Name
                              AND Namespace = @Namespace"
        Await Context.ExecuteNonQueryAsync(command,
                                          "@ItemID", ItemId,
                                          "@Name", name,
                                          "@Namespace", ns)
    End Function

    Friend Async Function CopyThisItemAsync(destFolder As DavFolder, destItem As DavHierarchyItem, destName As String) As Task(Of DavFolder)
        ' returns created folder, if any, otherwise null
        Dim createdFolder As DavFolder = Nothing
        Dim destID As Guid
        If destItem Is Nothing Then
            ' copy item
            Dim commandText As String = "INSERT INTO Item(
                           ItemId
                         , Name
                         , Created
                         , Modified
                         , ParentItemId
                         , ItemType
                         , Content
                         , ContentType
                         , SerialNumber
                         , TotalContentLength
                         , LastChunkSaved
                         , FileAttributes
                         )
                      SELECT
                           @Identity
                         , @Name
                         , GETUTCDATE()
                         , GETUTCDATE()
                         , @Parent
                         , ItemType
                         , Content
                         , ContentType
                         , SerialNumber
                         , TotalContentLength
                         , LastChunkSaved
                         , FileAttributes
                      FROM Item
                      WHERE ItemId = @ItemId"
            destID = Guid.NewGuid()
            Await Context.ExecuteNonQueryAsync(commandText,
                                              "@Name", destName,
                                              "@Parent", destFolder.ItemId,
                                              "@ItemId", ItemId,
                                              "@Identity", destID)
            Await destFolder.UpdateModifiedAsync()
            If TypeOf Me Is IFolderAsync Then
                createdFolder = New DavFolder(Context,
                                             destID,
                                             destFolder.ItemId,
                                             destName,
                                             destFolder.Path & EncodeUtil.EncodeUrlPart(destName) & "/",
                                             DateTime.UtcNow,
                                             DateTime.UtcNow, fileAttributes)
            End If
        Else
            ' update existing destination
            destID = destItem.ItemId
            Dim commandText As String = "UPDATE Item SET
                                       Modified = GETUTCDATE()
                                       , ItemType = src.ItemType
                                       , ContentType = src.ContentType
                                       FROM (SELECT * FROM Item WHERE ItemId=@SrcID) src
                                       WHERE Item.ItemId=@DestID"
            Await Context.ExecuteNonQueryAsync(commandText,
                                              "@SrcID", ItemId,
                                              "@DestID", destID)
            ' remove old properties from the destination
            Await Context.ExecuteNonQueryAsync("DELETE FROM Property WHERE ItemID = @ItemID",
                                              "@ItemID", destID)
        End If

        ' copy properties
        Dim command As String = "INSERT INTO Property(ItemID, Name, Namespace, PropVal)
                  SELECT @DestID, Name, Namespace, PropVal
                  FROM Property
                  WHERE ItemID = @SrcID"
        Await Context.ExecuteNonQueryAsync(command,
                                          "@SrcID", ItemId,
                                          "@DestID", destID)
        Return createdFolder
    End Function

    Friend Async Function MoveThisItemAsync(destFolder As DavFolder, destName As String, parent As DavFolder) As Task
        Dim command As String = "UPDATE Item SET
                     Name = @Name,
                     ParentItemId = @Parent
                  WHERE ItemId = @ItemID"
        Await Context.ExecuteNonQueryAsync(command,
                                          "@ItemID", ItemId,
                                          "@Name", destName,
                                          "@Parent", destFolder.ItemId)
        Await parent.UpdateModifiedAsync()
        Await destFolder.UpdateModifiedAsync()
    End Function

    Friend Async Function DeleteThisItemAsync() As Task
        Await DeleteThisItemAsync(Await GetParentAsync())
    End Function

    Friend Async Function DeleteThisItemAsync(parentFolder As DavFolder) As Task
        Await Context.ExecuteNonQueryAsync("DELETE FROM Item WHERE ItemId = @ItemID",
                                          "@ItemID", ItemId)
        If parentFolder IsNot Nothing Then
            Await parentFolder.UpdateModifiedAsync()
        End If
    End Function

    Private Function GetLocks(itemId As Guid, onlyDeep As Boolean) As List(Of LockInfo)
        If onlyDeep Then
            Dim command As String = "SELECT Token, Shared, Deep, Expires, Owner
                      FROM Lock
                      WHERE ItemID = @ItemID AND Deep = @Deep AND (Expires IS NULL OR Expires > GetUtcDate())"
            Return Context.ExecuteLockInfo(command,
                                          "@ItemID", itemId,
                                          "@Deep", True)
        End If

        Dim selectCommand As String = "SELECT Token, Shared, Deep, Expires, Owner
                 FROM Lock
                 WHERE ItemID = @ItemID AND (Expires IS NULL OR Expires > GetUtcDate())"
        Return Context.ExecuteLockInfo(selectCommand,
                                      "@ItemID", itemId)
    End Function

    Friend Async Function ClientHasTokenAsync() As Task(Of Boolean)
        Dim activeLocks As IEnumerable(Of LockInfo) = Await GetActiveLocksAsync()
        Dim itemLocks As List(Of LockInfo) = activeLocks.ToList()
        If itemLocks.Count = 0 Then
            Return True
        End If

        Dim clientLockTokens As IList(Of String) = Context.Request.ClientLockTokens
        Return itemLocks.Any(Function(il) clientLockTokens.Contains(il.Token))
    End Function

    Protected Async Function ItemHasLockAsync(skipShared As Boolean) As Task(Of Boolean)
        Dim activeLocks As IEnumerable(Of LockInfo) = Await GetActiveLocksAsync()
        Dim locks As List(Of LockInfo) = activeLocks.ToList()
        If locks.Count = 0 Then
            Return False
        End If

        Return Not skipShared OrElse locks.Any(Function(l) l.Level <> LockLevel.Shared)
    End Function

    Protected Shared Async Function FindLocksDownAsync(root As IHierarchyItemAsync, skipShared As Boolean) As Task
        Dim folder As IFolderAsync = TryCast(root, IFolderAsync)
        If folder IsNot Nothing Then
            For Each child As IHierarchyItemAsync In Await folder.GetChildrenAsync(New PropertyName(-1) {})
                Dim dbchild As DavHierarchyItem = TryCast(child, DavHierarchyItem)
                If Await dbchild.ItemHasLockAsync(skipShared) Then
                    Dim mex As MultistatusException = New MultistatusException()
                    mex.AddInnerException(dbchild.Path, New LockedException())
                    Throw mex
                End If

                Await FindLocksDownAsync(child, skipShared)
            Next
        End If
    End Function

    Friend Async Function UpdateModifiedAsync() As Task
        Await Context.ExecuteNonQueryAsync("UPDATE Item SET Modified = GETUTCDATE() WHERE ItemId = @ItemId",
                                          "@ItemId", ItemId)
    End Function

    Protected ReadOnly Property CurrentUserName As String
        Get
            Return If(Context.User IsNot Nothing, Context.User.Identity.Name, String.Empty)
        End Get
    End Property

    Protected Sub SetDbField(Of T)(columnName As String, value As T)
        Dim commandText As String = String.Format("UPDATE Item SET {0} = @value WHERE ItemId = @ItemId", columnName)
        Context.ExecuteNonQuery(commandText,
                               "@value", value,
                               "@ItemId", ItemId)
    End Sub

    Protected Async Function SetDbFieldAsync(Of T)(columnName As String, value As T) As Task
        Dim commandText As String = String.Format("UPDATE Item SET {0} = @value WHERE ItemId = @ItemId", columnName)
        Await Context.ExecuteNonQueryAsync(commandText,
                                          "@value", value,
                                          "@ItemId", ItemId)
    End Function

    Public Async Function GetFileAttributesAsync() As Task(Of FileAttributes) Implements IMsItemAsync.GetFileAttributesAsync
        Return fileAttributes
    End Function

    Public Async Function SetFileAttributesAsync(value As FileAttributes) As Task Implements IMsItemAsync.SetFileAttributesAsync
        Await SetDbFieldAsync("FileAttributes", CInt(value))
    End Function

    ''' <summary>
    ''' Gets element's parent path. 
    ''' </summary>
    ''' <param name="path">Element's path.</param>
    ''' <returns>Path to parent element.</returns>
    Protected Shared Function GetParentPath(path As String) As String
        Dim parentPath As String = $"/{path.Trim("/"c)}"
        Dim index As Integer = parentPath.LastIndexOf("/")
        parentPath = parentPath.Substring(0, index)
        Return parentPath
    End Function
End Class
