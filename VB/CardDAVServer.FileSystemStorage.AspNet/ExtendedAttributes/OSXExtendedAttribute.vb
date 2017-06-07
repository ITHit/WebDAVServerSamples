Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports CardDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

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

        Public Async Function IsExtendedAttributesSupportedAsync(path As String) As Task(Of Boolean) Implements IExtendedAttribute.IsExtendedAttributesSupportedAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            Dim attributeCount As Long = ListXAattr(path, Nothing, 0, 0)
            Return attributeCount >= 0
        End Function

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

        Private Shared Function GetMessageForErrno(errno As Integer) As String
            Dim buffer As StringBuilder = New StringBuilder(ErrorMessageBufferMaxSize)
            StrErrorR(errno, buffer, ErrorMessageBufferMaxSize)
            Return buffer.ToString()
        End Function

        <DllImport(libCName, EntryPoint:="getxattr", SetLastError:=True)>
        Shared Private Function GetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, position As Long, options As Integer) As Long
        End Function

        <DllImport(libCName, EntryPoint:="setxattr", SetLastError:=True)>
        Shared Private Function SetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, position As Long, options As Integer) As Long
        End Function

        <DllImport(libCName, EntryPoint:="removexattr", SetLastError:=True)>
        Shared Private Function RemoveXAttr(path As String, attribName As String, options As Integer) As Long
        End Function

        <DllImport(libCName, EntryPoint:="listxattr", SetLastError:=True)>
        Shared Private Function ListXAattr(filePath As String, nameBuffer As StringBuilder, size As Long, options As Integer) As Long
        End Function

        <DllImport(libCName, EntryPoint:="strerror_r", SetLastError:=True)>
        Shared Private Function StrErrorR(code As Integer, buffer As StringBuilder, bufferSize As Integer) As IntPtr
        End Function
    End Class
End Namespace
