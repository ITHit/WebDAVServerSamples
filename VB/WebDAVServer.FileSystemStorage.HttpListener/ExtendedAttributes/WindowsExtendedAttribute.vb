Imports Microsoft.Win32.SafeHandles
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes

Namespace ExtendedAttributes

    ''' <summary>
    ''' Provides methods for reading and writing extended attributes on files and folders on Windows.
    ''' NTFS alternate data streams are used to store attributes.
    ''' </summary>
    Public Class WindowsExtendedAttribute
        Implements IExtendedAttribute

        Private ReadOnly pathFormat As String = "{0}:{1}"

        Private ReadOnly fileSystemAttributeBlockSize As Integer = 262144

        Private Const systemErrorCodeDiskFull As Integer = 112

        ''' <summary>
        ''' Determines whether extended attributes are supported. 
        ''' </summary>
        ''' <param name="checkPath">File or folder path.</param>
        ''' <returns>True if extended attributes are supported, false otherwise.</returns>
        ''' <exception cref="ArgumentNullException">Throw when path is null or empty.</exception>
        ''' <exception cref="COMException">Throw when happens some system exception.</exception>
        Public Async Function IsExtendedAttributesSupportedAsync(checkPath As String) As Task(Of Boolean) Implements IExtendedAttribute.IsExtendedAttributesSupportedAsync
            If String.IsNullOrEmpty(checkPath) Then
                Throw New ArgumentNullException("path")
            End If

            checkPath = Path.GetPathRoot(checkPath)
            If Not checkPath.EndsWith("\") Then checkPath += "\"
            Dim volumeName As StringBuilder = New StringBuilder(261)
            Dim fileSystemName As StringBuilder = New StringBuilder(261)
            Dim volSerialNumber As Integer
            Dim maxFileNameLen As Integer
            Dim fileSystemFlags As Integer
            If Not GetVolumeInformation(GetWin32LongPath(checkPath), volumeName, volumeName.Capacity,
                                       volSerialNumber, maxFileNameLen, fileSystemFlags,
                                       fileSystemName, fileSystemName.Capacity) Then
                ThrowLastError()
            End If

            Return(fileSystemFlags And fileSystemAttributeBlockSize) = fileSystemAttributeBlockSize
        End Function

        ''' <summary>
        ''' Gets extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>Attribute value or null if attribute or file not found.</returns>
        Public Async Function GetExtendedAttributeAsync(path As String, attribName As String) As Task(Of String) Implements IExtendedAttribute.GetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim fullPath As String = String.Format(pathFormat, GetWin32LongPath(path), attribName)
            Using safeHandler As SafeFileHandle = GetSafeHandler(fullPath, FileAccess.Read, FileMode.Open, FileShare.ReadWrite)
                If safeHandler.IsInvalid Then
                    Dim lastError As Integer = Marshal.GetLastWin32Error()
                    If lastError = 2 Then
                        Return Nothing
                    End If
                Else
                    ThrowLastError()
                End If

                Using fileStream As FileStream = Open(safeHandler, FileAccess.Read)
                    Using streamReader As StreamReader = New StreamReader(fileStream)
                        Return Await streamReader.ReadToEndAsync()
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Sets extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value.</param>
        Public Async Function SetExtendedAttributeAsync(path As String, attribName As String, attribValue As String) As Task Implements IExtendedAttribute.SetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            If attribValue Is Nothing Then
                Throw New ArgumentNullException("attribValue")
            End If

            Dim fullPath As String = String.Format(pathFormat, GetWin32LongPath(path), attribName)
            Using safeHandler As SafeFileHandle = GetSafeHandler(fullPath, FileAccess.Write, FileMode.Create, FileShare.Read)
                If safeHandler.IsInvalid Then
                    ThrowLastError()
                End If

                Using fileStream As FileStream = Open(safeHandler, FileAccess.Write)
                    Using streamWriter As StreamWriter = New StreamWriter(fileStream)
                        Await streamWriter.WriteAsync(attribValue)
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Deletes extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        Public Async Function DeleteExtendedAttributeAsync(path As String, attribName As String) As Task Implements IExtendedAttribute.DeleteExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim fullPath As String = String.Format(pathFormat, GetWin32LongPath(path), attribName)
            If Not DeleteFile(fullPath) Then
                ThrowLastError()
            End If
        End Function

        ''' <summary>
        ''' Deletes all extended attributes.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        Public Async Function DeleteExtendedAttributes(path As String) As Task Implements IExtendedAttribute.DeleteExtendedAttributes
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Copies all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Public Async Function CopyExtendedAttributes(sourcePath As String, destinationPath As String) As Task Implements IExtendedAttribute.CopyExtendedAttributes
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Moves all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Public Async Function MoveExtendedAttributes(sourcePath As String, destinationPath As String) As Task Implements IExtendedAttribute.MoveExtendedAttributes
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Gets file handler.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="access">File access.</param>
        ''' <param name="mode">Specifies how the operating system should open a file.</param>
        ''' <param name="share">Sharing mode.</param>
        ''' <returns>A wrapper for a file handle.</returns>
        Private Function GetSafeHandler(path As String, access As FileAccess, mode As FileMode, share As FileShare) As SafeFileHandle
            If mode = FileMode.Append Then
                mode = FileMode.OpenOrCreate
            End If

            Dim accessRights As Integer = GetRights(access)
            Return CreateFile(path, accessRights, share, IntPtr.Zero, mode, 0, IntPtr.Zero)
        End Function

        ''' <summary>
        ''' Gets file stream from file handler.
        ''' </summary>
        ''' <param name="fileHandler">File handler.</param>
        ''' <param name="access">Read, write, or read/write access to a file.</param>
        ''' <returns>A file stream.</returns>
        Private Function Open(fileHandler As SafeFileHandle, access As FileAccess) As FileStream
            If fileHandler.IsInvalid Then
                ThrowLastError()
            End If

            Return New FileStream(fileHandler, access)
        End Function

        ''' <summary>
        ''' Gets file access rights.
        ''' </summary>
        ''' <param name="access">Read, write, or read/write access to a file.</param>
        ''' <returns>An integer representing access rights.</returns>
        Private Function GetRights(access As FileAccess) As Integer
            Select Case access
                Case FileAccess.Read
                    Return Integer.MinValue
                Case FileAccess.Write
                    Return 1073741824
                Case Else
                    Return -1073741824
            End Select
        End Function

        ''' <summary>
        ''' Gets long path for win32.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        Private Function GetWin32LongPath(path As String) As String
            If path.StartsWith("\\?\") Then
                Return path
            End If

            If path.StartsWith("\\") Then
                Return "\\?\UNC\" & path.TrimStart("\"c)
            End If

            Return "\\?\" & path
        End Function

        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Shared Function GetVolumeInformation(lpRootPathName As String, volumeName As StringBuilder, volumeNameBufLen As Integer, ByRef volSerialNumber As Integer, ByRef maxFileNameLen As Integer, ByRef fileSystemFlags As Integer, fileSystemName As StringBuilder, fileSystemNameBufLen As Integer) As Boolean
        End Function

        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Shared Function CreateFile(lpFileName As String, dwDesiredAccess As Integer, dwShareMode As FileShare, lpSecurityAttributes As IntPtr, dwCreationDisposition As FileMode, dwFlagsAndAttributes As Integer, hTemplateFile As IntPtr) As SafeFileHandle
        End Function

        ''' <summary>
        ''' Deletes a file or a stream.
        ''' </summary>
        ''' <param name="path">File or stream path.</param>
        ''' <returns>False if the function failed, nonzero otherwise.</returns>
        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Shared Function DeleteFile(path As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        ''' <summary>
        ''' Throws last system exception.
        ''' </summary>
        Private Shared Sub ThrowLastError()
            Dim lastError As Integer = Marshal.GetLastWin32Error()
            If 0 <> lastError Then
                Dim hr As Integer = Marshal.GetHRForLastWin32Error()
                Select Case lastError
                    Case 32
                        Throw New IOException("Sharing violation")
                    Case 2
                        Throw New FileNotFoundException()
                    Case 3
                        Throw New DirectoryNotFoundException()
                    Case 5
                        Throw New UnauthorizedAccessException()
                    Case 15
                        Throw New DriveNotFoundException()
                    Case 87
                        Throw New IOException()
                    Case 183
                    Case 206
                        Throw New PathTooLongException("Path too long")
                    Case 995
                        Throw New OperationCanceledException()
                End Select

                Throw New IOException()
            End If
        End Sub
    End Class
End Namespace
