Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Web
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.ResumableUpload

''' <summary>
''' Represents file in WebDAV repository.
''' </summary>
Public Class DavFile
    Inherits DavHierarchyItem
    Implements IFileAsync, IResumableUploadAsync, IUploadProgressAsync

    ''' <summary>
    ''' Initializes a new instance of the DavFile class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
    ''' <param name="itemId">Item identifier.</param>
    ''' <param name="parentId">Parent identifier.</param>
    ''' <param name="name">Item name.</param>
    ''' <param name="path">Item path (encoded)</param>
    ''' <param name="created">Item creation date.</param>
    ''' <param name="modified">Item modification date.</param>
    ''' <param name="fileAttributes">File attributes for the item.</param>
    Public Sub New(context As DavContext,
                  itemId As Guid,
                  parentId As Guid,
                  name As String,
                  path As String,
                  created As DateTime,
                  modified As DateTime)
        MyBase.New(context, itemId, parentId, name, path, created, modified)
    End Sub

    ''' <summary>
    ''' Gets item's content type.
    ''' </summary>
    Public ReadOnly Property ContentType As String Implements IContentAsync.ContentType
        Get
            Dim itemContentType As String = Nothing
            Dim extIndex As Integer = Name.LastIndexOf("."c)
            If extIndex <> -1 Then
                itemContentType = MimeType.GetMimeType(Name.Substring(extIndex + 1))
            End If

            If String.IsNullOrEmpty(itemContentType) Then
                itemContentType = getDbField(Of String)("ContentType", Nothing)
            End If

            If String.IsNullOrEmpty(itemContentType) Then
                itemContentType = "application/octet-stream"
            End If

            Return itemContentType
        End Get
    End Property

    ''' <summary>
    ''' Gets file length.
    ''' </summary>
    Public ReadOnly Property ContentLength As Long Implements IContentAsync.ContentLength
        Get
            Dim result As Object = Context.ExecuteScalar(Of Object)("SELECT DATALENGTH(Content) FROM Item WHERE ItemId = @ItemId",
                                                                   "@ItemId", ItemId)
            Return If(DBNull.Value.Equals(result), 0, Convert.ToInt64(result))
        End Get
    End Property

    ''' <summary>
    ''' Gets Etag.
    ''' </summary>
    Public ReadOnly Property Etag As String Implements IContentAsync.Etag
        Get
            Dim serialNumber As Integer = getDbField("SerialNumber", 0)
            Return String.Format("{0}-{1}", Modified.ToBinary(), serialNumber)
        End Get
    End Property

    ''' <summary>
    ''' Gets or Sets snippet of file content that matches search conditions.
    ''' </summary>
    Public Property Snippet As String

    ''' <summary>
    ''' Reads file's content from storage (to send to client).
    ''' </summary>
    ''' <param name="output">Stream to read body to.</param>
    ''' <param name="byteStart">Number of first byte to write.</param>
    ''' <param name="count">Number of bytes to be written.</param>
    Public Async Function ReadAsync(output As Stream, byteStart As Long, count As Long) As Task Implements IContentAsync.ReadAsync
        If ContainsDownloadParam(Context.Request.RawUrl) Then
            AddContentDisposition(Context, Name)
        End If

        Using reader As SqlDataReader = Await Context.ExecuteReaderAsync(CommandBehavior.SequentialAccess,
                                                                        "SELECT Content FROM Item WHERE ItemId = @ItemId",
                                                                        "@ItemId", ItemId)
            reader.Read()
            Dim bufSize As Long = 1048576
            Dim buf = New Byte(bufSize - 1) {}
            Dim retval As Long
            Dim startIndex As Long = byteStart
            Try
                While(__InlineAssignHelper(retval, reader.GetBytes(reader.GetOrdinal("Content"),
                                                                  startIndex,
                                                                  buf,
                                                                  0,
                                                                  CInt((If(count > bufSize, bufSize, count)))))) > 0
                    output.Write(buf, 0, CInt(retval))
                    startIndex += retval
                    count -= retval
                End While
            Catch __unusedHttpListenerException1__ As System.Net.HttpListenerException
                ' The remote host closed the connection (for example Cancel or Pause pressed).
                 End Try
        End Using
    End Function

    Friend Shared Function ContainsDownloadParam(url As String) As Boolean
        Dim ind As Integer = url.IndexOf("?"c)
        If ind > 0 AndAlso ind < url.Length - 1 Then
            Dim param As String() = url.Substring(ind + 1).Split("&"c)
            Return param.Any(Function(p) p.StartsWith("download"))
        End If

        Return False
    End Function

    Friend Shared Sub AddContentDisposition(context As DavContext, name As String)
        If context.Request.UserAgent.Contains("MSIE") Then
            Dim fileName = EncodeUtil.EncodeUrlPart(name)
            context.Response.AddHeader("Content-Disposition", "attachment; filename=""" & fileName & """")
        Else
            context.Response.AddHeader("Content-Disposition", "attachment;")
        End If
    End Sub

    ''' <summary>
    ''' Stores file contents to storage (when client updates it).
    ''' </summary>
    ''' <param name="segment">Stream with new file content.</param>
    ''' <param name="contentType">New content type.</param>
    ''' <param name="startIndex">Index of first byte in the file where update shall be applied.</param>
    ''' <param name="totalContentLength">Length of the file after it will be updated with the new content.</param>
    ''' <returns>Boolean value indicating if entire stream was written.</returns>
    Public Async Function WriteAsync(segment As Stream, contentType As String, startIndex As Long, totalContentLength As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
        Await RequireHasTokenAsync()
        Dim commandText As String = "UPDATE Item
                  SET
                      Modified = GETUTCDATE(),
                      TotalContentLength = CASE WHEN @TotalContentLength >= 0 THEN @TotalContentLength ELSE 0 END,
                      ContentType = @ContentType,
                      Content = CASE WHEN @ResetContent = 1 THEN 0x ELSE Content END,
                      SerialNumber = ISNULL(SerialNumber, 0) + 1
                  WHERE ItemId = @ItemId"
        Await Context.ExecuteNonQueryAsync(commandText,
                                          "@ContentType", If(CObj(contentType), DBNull.Value),
                                          "@ItemId", ItemId,
                                          "@TotalContentLength", totalContentLength,
                                          "@ResetContent", If(startIndex = 0, 1, 0))
        Const bufSize As Integer = 1048576
        Dim buffer As Byte() = New Byte(1048575) {}
        Dim bytes As Long = 0
        Dim lastBytesRead As Integer
        Try
            While(__InlineAssignHelper(lastBytesRead, segment.Read(buffer, 0, bufSize))) > 0
                Dim dataParm As SqlParameter = New SqlParameter("@Data", SqlDbType.VarBinary, bufSize)
                Dim offsetParm As SqlParameter = New SqlParameter("@Offset", SqlDbType.Int)
                Dim bytesParm As SqlParameter = New SqlParameter("@Bytes", SqlDbType.Int, bufSize)
                Dim itemIdParm As SqlParameter = New SqlParameter("@ItemId", ItemId)
                dataParm.Value = buffer
                dataParm.Size = lastBytesRead
                offsetParm.Value = bytes + startIndex
                bytesParm.Value = lastBytesRead
                Dim updateItemCommand As String = "UPDATE Item SET
                            LastChunkSaved = GetUtcDate(),
                            Content .WRITE(@Data, @Offset, @Bytes)
                          WHERE ItemId = @ItemId"
                Await Context.ExecuteNonQueryAsync(updateItemCommand,
                                                  dataParm,
                                                  offsetParm,
                                                  bytesParm,
                                                  itemIdParm)
                bytes += lastBytesRead
            End While
        Catch __unusedHttpListenerException1__ As System.Net.HttpListenerException
            ' The remote host closed the connection (for example Cancel or Pause pressed).
             End Try

        Await Context.socketService.NotifyRefreshAsync(GetParentPath(Path))
        Return True
    End Function

    ''' <summary>
    ''' Copies this file to another folder.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New file name in destination folder.</param>
    ''' <param name="deep">Is not used.</param>
    ''' <param name="multistatus">Container for errors with items other than this file.</param>
    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync,
                                               destName As String,
                                               deep As Boolean,
                                               multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Dim destDavFolder As DavFolder = TryCast(destFolder, DavFolder)
        If destFolder Is Nothing Then
            Throw New DavException("Destination folder doesn't exist.", DavStatus.CONFLICT)
        End If

        If Not Await destDavFolder.ClientHasTokenAsync() Then
            Throw New LockedException("Doesn't have token for destination folder.")
        End If

        Dim destItem As DavHierarchyItem = Await destDavFolder.FindChildAsync(destName)
        If destItem IsNot Nothing Then
            Try
                Await destItem.DeleteAsync(multistatus)
            Catch ex As DavException
                multistatus.AddInnerException(destItem.Path, ex)
                Return
            End Try
        End If

        Await CopyThisItemAsync(destDavFolder, Nothing, destName)
        Await Context.socketService.NotifyRefreshAsync(destDavFolder.Path)
    End Function

    ''' <summary>
    ''' Deletes this file.
    ''' </summary>
    ''' <param name="multistatus">Is not used.</param>
    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        Dim parent As DavFolder = Await GetParentAsync()
        If parent Is Nothing Then
            Throw New DavException("Parent is null.", DavStatus.CONFLICT)
        End If

        If Not Await parent.ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        If Not Await ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Await DeleteThisItemAsync(parent)
        Await Context.socketService.NotifyRefreshAsync(parent.Path)
    End Function

    ''' <summary>
    ''' Moves this file to different folder and renames it.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New file name.</param>
    ''' <param name="multistatus">Container for errors with items other than this file.</param>
    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Dim destDavFolder As DavFolder = TryCast(destFolder, DavFolder)
        If destFolder Is Nothing Then
            Throw New DavException("Destination folder doesn't exist.", DavStatus.CONFLICT)
        End If

        Dim parent As DavFolder = Await GetParentAsync()
        If parent Is Nothing Then
            Throw New DavException("Cannot move root.", DavStatus.CONFLICT)
        End If

        If Not Await ClientHasTokenAsync() OrElse Not Await destDavFolder.ClientHasTokenAsync() OrElse Not Await parent.ClientHasTokenAsync() Then
            Throw New LockedException()
        End If

        Dim destItem As DavHierarchyItem = Await destDavFolder.FindChildAsync(destName)
        If destItem IsNot Nothing Then
            Try
                Await destItem.DeleteAsync(multistatus)
            Catch ex As DavException
                multistatus.AddInnerException(destItem.Path, ex)
                Return
            End Try
        End If

        Await MoveThisItemAsync(destDavFolder, destName, parent)
        ' Refresh client UI.
        Await Context.socketService.NotifyRefreshAsync(parent.Path)
        Await Context.socketService.NotifyRefreshAsync(destDavFolder.Path)
    End Function

    ''' <summary>
    ''' Cancels incomplete upload.
    ''' </summary>
    Public Async Function CancelUploadAsync() As Task Implements IResumableUploadAsync.CancelUploadAsync
        Await DeleteAsync(Nothing)
    End Function

    ''' <summary>
    ''' Gets time when last upload piece was saved to disk.
    ''' </summary>
    Public ReadOnly Property LastChunkSaved As DateTime Implements IResumableUploadAsync.LastChunkSaved
        Get
            Return getDbField("LastChunkSaved", DateTime.MinValue)
        End Get
    End Property

    ''' <summary>
    ''' Gets bytes uploaded sofar.
    ''' </summary>
    Public ReadOnly Property BytesUploaded As Long Implements IResumableUploadAsync.BytesUploaded
        Get
            Return ContentLength
        End Get
    End Property

    ''' <summary>
    ''' Gets length of the file which it will have after upload finishes.
    ''' </summary>
    Public ReadOnly Property TotalContentLength As Long Implements IResumableUploadAsync.TotalContentLength
        Get
            Return getDbField(Of Long)("TotalContentLength", -1)
        End Get
    End Property

    ''' <summary>
    ''' Returns instance of <see cref="IResumableUploadAsync"/>  interface for this item.
    ''' </summary>
    ''' <returns>Instance of <see cref="IResumableUploadAsync"/>  interface.</returns>
    Public Async Function GetUploadProgressAsync() As Task(Of IEnumerable(Of IResumableUploadAsync)) Implements IUploadProgressAsync.GetUploadProgressAsync
        Return {Me}
    End Function

    ''' <summary>
    ''' Returns database field of the file.
    ''' </summary>
    ''' <typeparam name="T">Type of field value.</typeparam>
    ''' <param name="columnName">DB columen in which field is stored.</param>
    ''' <param name="defaultValue">Default value to return.</param>
    ''' <returns>Value from database or default value if it is null.</returns>
    Private Function getDbField(Of T)(columnName As String, defaultValue As T) As T
        Dim commandText As String = String.Format("SELECT {0} FROM Item WHERE ItemId = @ItemId", columnName)
        Dim obj As Object = Context.ExecuteScalar(Of Object)(commandText, "@ItemId", ItemId)
        Return If(obj IsNot Nothing, CType(obj, T), defaultValue)
    End Function

    <Obsolete("Please refactor code that uses this function, it is a simple work-around to simulate inline assignment in VB!")>
    Private Shared Function __InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
