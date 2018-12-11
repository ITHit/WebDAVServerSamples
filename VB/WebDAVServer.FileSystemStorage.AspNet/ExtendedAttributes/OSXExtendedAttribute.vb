Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports WebDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

Namespace ExtendedAttributes

    ''' <summary>
    ''' Provides methods for reading and writing extended attributes on files and folders on Mac OS X.
    ''' </summary>
    Public Class OSXExtendedAttribute
        Implements IExtendedAttribute

        ''' <summary>
        ''' Errno for not existing attribute.
        ''' </summary>
        Private Const AttributeNotFoundErrno As Integer = 93

        ''' <summary>
        ''' Max size for error message buffer.
        ''' </summary>
        Private Const ErrorMessageBufferMaxSize As Integer = 255

        ''' <summary>
        ''' Dynamic C library name.
        ''' </summary>
        Private Const libCName As String = "libSystem.dylib"

        ''' <summary>
        ''' Determines whether extended attributes are supported.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <returns>True if extended attributes are supported, false otherwise.</returns>
        ''' <exception cref="ArgumentNullException">Throw when path is null or empty.</exception>
        Public Async Function IsExtendedAttributesSupportedAsync(path As String) As Task(Of Boolean) Implements IExtendedAttribute.IsExtendedAttributesSupportedAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            Dim attributeCount As Long = ListXAattr(path, Nothing, 0, 0)
            Return attributeCount >= 0
        End Function

        ''' <summary>
        ''' Reads extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>Attribute value.</returns>
        ''' <exception cref="ArgumentNullException">Throw when path is null or empty or attribName is null or empty.</exception>
        ''' <exception cref="IOException">Throw when file or attribute is no available.</exception>
        Public Async Function GetExtendedAttributeAsync(path As String, attribName As String) As Task(Of String) Implements IExtendedAttribute.GetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim attributeSize As Long = GetXAttr(path, attribName, Nothing, 0, 0, 0)
            If attributeSize < 0 Then
                If Marshal.GetLastWin32Error() = AttributeNotFoundErrno Then
                    Return Nothing
                End If

                ThrowLastException(path, attribName)
            End If

            Dim buffer As Byte() = New Byte(attributeSize - 1) {}
            Dim readedLength As Long = GetXAttr(path, attribName, buffer, attributeSize, 0, 0)
            If readedLength = -1 Then
                ThrowLastException(path, attribName)
            End If

            Dim attributeValue As String = Encoding.UTF8.GetString(buffer)
            Return attributeValue
        End Function

        ''' <summary>
        ''' Writes extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value.</param>
        ''' <exception cref="ArgumentNullException">Throw when path is null or empty or attribName is null or empty.</exception>
        ''' <exception cref="IOException">Throw when file or attribute is no available.</exception>
        Public Async Function SetExtendedAttributeAsync(path As String, attribName As String, attribValue As String) As Task Implements IExtendedAttribute.SetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim buffer As Byte() = Encoding.UTF8.GetBytes(attribValue)
            Dim result As Long = SetXAttr(path, attribName, buffer, buffer.Length, 0, 0)
            If result = -1 Then
                ThrowLastException(path, attribName)
            End If
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

            Dim result As Long = RemoveXAttr(path, attribName, 0)
            If result = -1 Then
                ThrowLastException(path, attribName)
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
        ''' Throws corresponding exception for last platform api call.
        ''' </summary>
        ''' <param name="fileName">File name.</param>
        ''' <param name="attrName">Attribute name.</param>
        ''' <exception cref="System.IO.IOException"></exception>
        Private Sub ThrowLastException(fileName As String, attrName As String)
            Dim errno As Integer = Marshal.GetLastWin32Error()
            Dim message As String = GetMessageForErrno(errno)
            Throw New IOException(String.Format("[{0}:{1}] {2} Errno {3}", fileName, attrName, message, errno))
        End Sub

        ''' <summary>
        ''' Returns error message that described error number.
        ''' </summary>
        ''' <param name="errno">Error number.</param>
        ''' <returns>Error message</returns>
        Private Shared Function GetMessageForErrno(errno As Integer) As String
            Dim buffer As StringBuilder = New StringBuilder(ErrorMessageBufferMaxSize)
            StrErrorR(errno, buffer, ErrorMessageBufferMaxSize)
            Return buffer.ToString()
        End Function

        ''' <summary>
        ''' External func getxattr from libc, what returns custom attribute by name.
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="buffer">Buffer to collect attribute value.</param>
        ''' <param name="bufferSize">Buffer size.</param>
        ''' <param name="position">Position value.</param>
        ''' <param name="options">Options value.</param>
        ''' <returns>ttribute value size in bytes, when returning value -1 than some error occurred.''' </returns>
        <DllImport(libCName, EntryPoint:="getxattr", SetLastError:=True)>
        Shared Private Function GetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, position As Long, options As Integer) As Long
        End Function

        ''' <summary>
        ''' External func setxattr from libc, sets attribute value for file by name. 
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value</param>
        ''' <param name="size">Attribute value size</param>
        ''' <param name="position">Position value.</param>
        ''' <param name="options">Options value.</param>
        ''' <returns>Status, when returning value -1 than some error occurred.</returns>
        <DllImport(libCName, EntryPoint:="setxattr", SetLastError:=True)>
        Shared Private Function SetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, position As Long, options As Integer) As Long
        End Function

        ''' <summary>
        ''' Removes the extended attribute. 
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="options">Options value.</param>
        ''' <returns>On success, zero is returned. On failure, -1 is returned.</returns>
        <DllImport(libCName, EntryPoint:="removexattr", SetLastError:=True)>
        Shared Private Function RemoveXAttr(path As String, attribName As String, options As Integer) As Long
        End Function

        ''' <summary>
        ''' External func listxattr from libc, what returns list of attributes separated null-terminated string.
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="nameBuffer">Attribute name.</param>
        ''' <param name="size">Buffer size</param>
        ''' <param name="options">Options value.</param>
        ''' <returns>Attributes bytes array size, when returning value -1 than some error occurred</returns>
        <DllImport(libCName, EntryPoint:="listxattr", SetLastError:=True)>
        Shared Private Function ListXAattr(filePath As String, nameBuffer As StringBuilder, size As Long, options As Integer) As Long
        End Function

        ''' <summary>
        ''' External func strerror_r from libc, what returns string that describes the error code passed in the argument.
        ''' </summary>
        ''' <param name="code">Error number.</param>
        ''' <param name="buffer">Destination buffer.</param>
        ''' <param name="bufferSize">Buffer size.</param>
        <DllImport(libCName, EntryPoint:="strerror_r", SetLastError:=True)>
        Shared Private Function StrErrorR(code As Integer, buffer As StringBuilder, bufferSize As Integer) As IntPtr
        End Function
    End Class
End Namespace
