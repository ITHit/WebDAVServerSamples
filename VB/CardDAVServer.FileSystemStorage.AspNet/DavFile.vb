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

    Public Shared Async Function GetFileAsync(context As DavContext, path As String) As Task(Of DavFile)
        Dim filePath As String = context.MapPath(path)
        Dim file As FileInfo = New FileInfo(filePath)
        If Not file.Exists OrElse String.Compare(file.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), filePath, StringComparison.OrdinalIgnoreCase) <> 0 Then
            Return Nothing
        End If

        Dim davFile As DavFile = New DavFile(file, context, path)
        davFile.serialNumber = If(Await file.GetExtendedAttributeAsync(Of Integer?)("SerialNumber"), 0)
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
                While bytesRead > 0
                    Await output.WriteAsync(buffer, 0, bytesRead)
                    count -= bytesRead
                    bytesRead = Await fileStream.ReadAsync(buffer, 0, toRead)
                End While
            Catch __unusedHttpException1__ As HttpException
            End Try
        End Using
    End Function

    Public Overridable Async Function WriteAsync(content As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
        'Set timeout to maximum value to be able to upload large files.
        HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
        If startIndex = 0 AndAlso fileInfo.Length > 0 Then
            Using filestream As FileStream = fileInfo.Open(FileMode.Truncate)
            End Using
        End If

        Await fileInfo.SetExtendedAttributeAsync("SerialNumber", Me.serialNumber + 1)
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
                    Await fileStream.FlushAsync()
                    lastBytesRead = Await content.ReadAsync(buffer, 0, bufSize)
                End While
            Catch __unusedHttpException1__ As HttpException
            End Try
        End Using

        Return True
    End Function

    Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If targetFolder Is Nothing OrElse Not Directory.Exists(targetFolder.FullPath) Then
            Throw New DavException("Target directory doesn't exist", DavStatus.CONFLICT)
        End If

        Dim newFilePath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        Try
            Dim item As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(targetPath)
            If item IsNot Nothing Then Await item.DeleteAsync(multistatus)
        Catch ex As DavException
            ' Report error with other item to client.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        Try
            File.Copy(fileSystemInfo.FullName, newFilePath)
            If Await fileSystemInfo.HasExtendedAttributeAsync("Locks") Then Await New FileInfo(newFilePath).DeleteExtendedAttributeAsync("Locks")
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            Dim parentPath As String = System.IO.Path.GetDirectoryName(Path)
            ex.AddRequiredPrivilege(parentPath, Privilege.Bind)
            Throw ex
        End Try
    End Function

    Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Dim targetFolder As DavFolder = CType(destFolder, DavFolder)
        If targetFolder Is Nothing OrElse Not Directory.Exists(targetFolder.FullPath) Then
            Throw New DavException("Target directory doesn't exist", DavStatus.CONFLICT)
        End If

        Dim newDirPath As String = System.IO.Path.Combine(targetFolder.FullPath, destName)
        Dim targetPath As String = targetFolder.Path & EncodeUtil.EncodeUrlPart(destName)
        Try
            Dim item As IHierarchyItemAsync = Await context.GetHierarchyItemAsync(targetPath)
            If item IsNot Nothing Then
                Await item.DeleteAsync(multistatus)
            End If
        Catch ex As DavException
            ' Report exception to client and continue with other items by returning from recursion.
            multistatus.AddInnerException(targetPath, ex)
            Return
        End Try

        Try
            File.Move(fileSystemInfo.FullName, newDirPath)
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(targetPath, Privilege.Bind)
            Dim parentPath As String = System.IO.Path.GetDirectoryName(Path)
            ex.AddRequiredPrivilege(parentPath, Privilege.Unbind)
            Throw ex
        End Try
    End Function

    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
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
        If context.Request.UserAgent.Contains("MSIE") Then
            Dim fileName As String = EncodeUtil.EncodeUrlPart(name)
            Dim attachment As String = String.Format("attachment filename=""{0}""", fileName)
            context.Response.AddHeader("Content-Disposition", attachment)
        Else
            context.Response.AddHeader("Content-Disposition", "attachment")
        End If
    End Sub
End Class
