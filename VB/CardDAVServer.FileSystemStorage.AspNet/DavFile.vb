Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Web
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports CardDAVServer.FileSystemStorage.AspNet.ExtendedAttributes
Imports ITHit.Server

''' <summary>
''' Represents file in WebDAV repository.
''' </summary>
Public Class DavFile
    Inherits DavHierarchyItem
    Implements IFileAsync

    ''' <summary>
    ''' Corresponding <see cref="FileInfo"/> .
    ''' </summary>
    Private ReadOnly fileInfo As FileInfo

    ''' <summary>
    ''' Size of chunks to upload/download.
    ''' (1Mb) buffer size used when reading and writing file content.
    ''' </summary>
    Private Const bufSize As Integer = 1048576

    ''' <summary>
    ''' Value updated every time this file is updated. Used to form Etag.
    ''' </summary>
    Private serialNumber As Integer

    ''' <summary>
    ''' Gets content type.
    ''' </summary>
    Public ReadOnly Property ContentType As String Implements IContentAsync.ContentType
        Get
            Return If(MimeType.GetMimeType(fileSystemInfo.Extension), "application/octet-stream")
        End Get
    End Property

    ''' <summary>
    ''' Gets length of the file.
    ''' </summary>
    Public ReadOnly Property ContentLength As Long Implements IContentAsync.ContentLength
        Get
            Return fileInfo.Length
        End Get
    End Property

    ''' <summary>
    ''' Gets entity tag - string that identifies current state of resource's content.
    ''' </summary>
    ''' <remarks>This property shall return different value if content changes.</remarks>
    Public ReadOnly Property Etag As String Implements IContentAsync.Etag
        Get
            Return String.Format("{0}-{1}", Modified.ToBinary(), Me.serialNumber)
        End Get
    End Property

    ''' <summary>
    ''' Returns file that corresponds to path.
    ''' </summary>
    ''' <param name="context">WebDAV Context.</param>
    ''' <param name="path">Encoded path relative to WebDAV root folder.</param>
    ''' <returns>File instance or null if physical file is not found in file system.</returns>
    Public Shared Async Function GetFileAsync(context As DavContext, path As String) As Task(Of DavFile)
        Dim filePath As String = context.MapPath(path)
        Dim file As FileInfo = New FileInfo(filePath)
        ' This code blocks vulnerability when "%20" folder can be injected into path and file.Exists returns 'true'.
        If Not file.Exists OrElse String.Compare(file.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), filePath, StringComparison.OrdinalIgnoreCase) <> 0 Then
            Return Nothing
        End If

        Dim davFile As DavFile = New DavFile(file, context, path)
        If Await file.HasExtendedAttributeAsync("SerialNumber") Then
            davFile.serialNumber = If(Await file.GetExtendedAttributeAsync(Of Integer?)("SerialNumber"), 0)
        End If

        Return davFile
    End Function

    ''' <summary>
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="file">Corresponding file in the file system.</param>
    ''' <param name="context">WebDAV Context.</param>
    ''' <param name="path">Encoded path relative to WebDAV root folder.</param>
    Protected Sub New(file As FileInfo, context As DavContext, path As String)
        MyBase.New(file, context, path)
        Me.fileInfo = file
    End Sub

    ''' <summary>
    ''' Called when a client is downloading a file. Copies file contents to ouput stream.
    ''' </summary>
    ''' <param name="output">Stream to copy contents to.</param>
    ''' <param name="startIndex">The zero-bazed byte offset in file content at which to begin copying bytes to the output stream.</param>
    ''' <param name="count">The number of bytes to be written to the output stream.</param>
    Public Overridable Async Function ReadAsync(output As Stream, startIndex As Long, count As Long) As Task Implements IContentAsync.ReadAsync
        'Set timeout to maximum value to be able to download large files.
        HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
        If ContainsDownloadParam(context.Request.RawUrl) Then
            AddContentDisposition(Name)
        End If

        Dim buffer As Byte() = New Byte(1048575) {}
        Using fileStream As FileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)
            fileStream.Seek(startIndex, SeekOrigin.Begin)
            Dim bytesRead As Integer
            Dim toRead As Integer = CInt(Math.Min(count, bufSize))
            If toRead <= 0 Then
                Return
            End If

            Try
                bytesRead = Await fileStream.ReadAsync(buffer, 0, toRead)
                While bytesRead > 0 AndAlso count > 0
                    Await output.WriteAsync(buffer, 0, bytesRead)
                    count -= bytesRead
                    bytesRead = Await fileStream.ReadAsync(buffer, 0, toRead)
                End While
            Catch __unusedHttpException1__ As HttpException
                ' The remote host closed the connection (for example Cancel or Pause pressed).
                 End Try
        End Using
    End Function

    ''' <summary>
    ''' Called when a file or its part is being uploaded.
    ''' </summary>
    ''' <param name="content">Stream to read the content of the file from.</param>
    ''' <param name="contentType">Indicates the media type of the file.</param>
    ''' <param name="startIndex">Starting byte in target file
    ''' for which data comes in <paramref name="content"/>  stream.</param>
    ''' <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
    ''' <returns>Whether the whole stream has been written. This result is used by the engine to determine
    ''' if auto checkin shall be performed (if auto versioning is used).</returns>
    Public Overridable Async Function WriteAsync(content As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
        'Set timeout to maximum value to be able to upload large files.
        HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
        If startIndex = 0 AndAlso fileInfo.Length > 0 Then
            Using filestream As FileStream = fileInfo.Open(FileMode.Truncate)
                 End Using
        End If

        Await fileInfo.SetExtendedAttributeAsync("SerialNumber", System.Threading.Interlocked.Increment(Me.serialNumber))
        Using fileStream As FileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)
            If fileStream.Length < startIndex Then
                Throw New DavException("Previous piece of file was not uploaded.", DavStatus.PRECONDITION_FAILED)
            End If

            If Not content.CanRead Then
                Return True
            End If

            fileStream.Seek(startIndex, SeekOrigin.Begin)
            Dim buffer As Byte() = New Byte(1048575) {}
            Dim lastBytesRead As Integer
            Try
                lastBytesRead = Await content.ReadAsync(buffer, 0, bufSize)
                While lastBytesRead > 0
                    Await fileStream.WriteAsync(buffer, 0, lastBytesRead)
                    lastBytesRead = Await content.ReadAsync(buffer, 0, bufSize)
                End While
            Catch __unusedHttpException1__ As HttpException
                ' The remote host closed the connection (for example Cancel or Pause pressed).
                 End Try
        End Using

        Return True
    End Function

    ''' <summary>
    ''' Called when this file is being copied.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New file name.</param>
    ''' <param name="deep">Whether children items shall be copied. Ignored for files.</param>
    ''' <param name="multistatus">Information about items that failed to copy.</param>
    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If targetFolder Is Nothing OrElse Not Directory.Exists(targetFolder.FullPath) Then
            Throw New DavException("Target directory doesn't exist", DavStatus.CONFLICT)
        End If

        Dim newFilePath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        ' If an item with the same name exists - remove it.
        Try
            Dim item As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(targetPath)
            If item IsNot Nothing Then Await item.DeleteAsync(multistatus)
        Catch ex As DavException
            ' Report error with other item to client.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        ' Copy the file togather with all extended attributes (custom props and locks).
        Try
            File.Copy(fileSystemInfo.FullName, newFilePath)
            Dim newFileSystemInfo = New FileInfo(newFilePath)
            If FileSystemInfoExtension.IsUsingFileSystemAttribute Then
                Await fileSystemInfo.CopyExtendedAttributes(newFileSystemInfo)
            End If

            ' Locks should not be copied, delete them.
            If Await fileSystemInfo.HasExtendedAttributeAsync("Locks") Then
                Await newFileSystemInfo.DeleteExtendedAttributeAsync("Locks")
            End If
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            ' Fail
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            Dim parentPath As String = System.IO.Path.GetDirectoryName(Path)
            ex.AddRequiredPrivilege(parentPath, Privilege.Bind)
            Throw ex
        End Try
    End Function

    ''' <summary>
    ''' Called when this file is being moved or renamed.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">New name of this file.</param>
    ''' <param name="multistatus">Information about items that failed to move.</param>
    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If targetFolder Is Nothing OrElse Not Directory.Exists(targetFolder.FullPath) Then
            Throw New DavException("Target directory doesn't exist", DavStatus.CONFLICT)
        End If

        Dim newDirPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        ' If an item with the same name exists in target directory - remove it.
        Try
            Dim item As IHierarchyItemAsync = TryCast(Await context.GetHierarchyItemAsync(targetPath), IHierarchyItemAsync)
            If item IsNot Nothing Then
                Await item.DeleteAsync(multistatus)
            End If
        Catch ex As DavException
            ' Report exception to client and continue with other items by returning from recursion.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        ' Move the file.
        Try
            File.Move(fileSystemInfo.FullName, newDirPath)
            Dim newFileInfo As FileInfo = New FileInfo(newDirPath)
            If FileSystemInfoExtension.IsUsingFileSystemAttribute Then
                Await fileSystemInfo.MoveExtendedAttributes(newFileInfo)
            End If

            ' Locks should not be copied, delete them.
            If Await newFileInfo.HasExtendedAttributeAsync("Locks") Then Await newFileInfo.DeleteExtendedAttributeAsync("Locks")
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            ' Exception occurred with the item for which MoveTo was called - fail the operation.
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(targetPath, Privilege.Bind)
            Dim parentPath As String = System.IO.Path.GetDirectoryName(Path)
            ex.AddRequiredPrivilege(parentPath, Privilege.Unbind)
            Throw ex
        End Try
    End Function

    ''' <summary>
    ''' Called whan this file is being deleted.
    ''' </summary>
    ''' <param name="multistatus">Information about items that failed to delete.</param>
    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        If FileSystemInfoExtension.IsUsingFileSystemAttribute Then
            Await fileSystemInfo.DeleteExtendedAttributes()
        End If

        fileSystemInfo.Delete()
    End Function

    Friend Shared Function ContainsDownloadParam(url As String) As Boolean
        Dim ind As Integer = url.IndexOf("?"c)
        If ind > 0 AndAlso ind < url.Length - 1 Then
            Dim param As String() = url.Substring(ind + 1).Split("&"c)
            Return param.Any(Function(p) p.StartsWith("download"))
        End If

        Return False
    End Function

    ''' <summary>
    ''' Adds Content-Disposition header.
    ''' </summary>
    ''' <param name="name">File name to specified in Content-Disposition header.</param>
    Private Sub AddContentDisposition(name As String)
        ' Content-Disposition header must be generated differently in case if IE and other web browsers.
        If context.Request.UserAgent.Contains("MSIE") Then
            Dim fileName As String = EncodeUtil.EncodeUrlPart(name)
            Dim attachment As String = String.Format("attachment filename=""{0}""", fileName)
            context.Response.AddHeader("Content-Disposition", attachment)
        Else
            context.Response.AddHeader("Content-Disposition", "attachment")
        End If
    End Sub
End Class
