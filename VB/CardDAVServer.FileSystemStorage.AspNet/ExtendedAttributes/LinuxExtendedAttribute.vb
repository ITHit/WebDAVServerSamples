Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports CardDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

Namespace ExtendedAttributes

    ''' <summary>
    ''' Provides methods for reading and writing extended attributes on files and folders on Linux.
    ''' </summary>
    Public Class LinuxExtendedAttribute
        Implements IExtendedAttribute

        ''' <summary>
        ''' Errno for not existing attribute.
        ''' </summary>
        Private Const AttributeNotFoundErrno As Integer = 61

        ''' <summary>
        ''' Dynamic C library name.
        ''' </summary>
        Private Const libCName As String = "libc.so.6"

        ''' <summary>
        ''' Linux allows stored extended attribute in special namespaces only.
        ''' Extended user attributes.
        ''' http://manpages.ubuntu.com/manpages/wily/man5/attr.5.html
        ''' </summary>
        Private ReadOnly attributeNameFormat As String = "user.{0}"

        Public Async Function IsExtendedAttributesSupportedAsync(path As String) As Task(Of Boolean) Implements IExtendedAttribute.IsExtendedAttributesSupportedAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            Dim attributeCount As Long = ListXAattr(path, Nothing, 0)
            Return attributeCount <> -1
        End Function

        Public Async Function GetExtendedAttributeAsync(path As String, attribName As String) As Task(Of String) Implements IExtendedAttribute.GetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim attributeSize As Long = GetXAttr(path, userAttributeName, Nothing, 0)
            If attributeSize = -1 Then
                If Marshal.GetLastWin32Error() = AttributeNotFoundErrno Then
                    Return Nothing
                End If

                ThrowLastException(path, userAttributeName)
            End If

            Dim buffer As Byte() = New Byte(attributeSize - 1) {}
            Dim readedLength As Long = GetXAttr(path, userAttributeName, buffer, attributeSize)
            If readedLength = -1 Then
                ThrowLastException(path, userAttributeName)
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

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim buffer As Byte() = Encoding.UTF8.GetBytes(attribValue)
            Dim result As Long = SetXAttr(path, userAttributeName, buffer, buffer.Length, 0)
            If result = -1 Then
                ThrowLastException(path, userAttributeName)
            End If
        End Function

        Public Async Function DeleteExtendedAttributeAsync(path As String, attribName As String) As Task Implements IExtendedAttribute.DeleteExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim result As Long = RemoveXAttr(path, userAttributeName)
            If result = -1 Then
                ThrowLastException(path, userAttributeName)
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
            Dim locale As IntPtr = NewLocale(8127, "C", IntPtr.Zero)
            If locale = IntPtr.Zero Then
                Throw New InvalidOperationException("Not able to get locale")
            End If

            Dim message As String = Marshal.PtrToStringAnsi(StrErrorL(errno, locale))
            ' Free locale
            FreeLocale(locale)
            Return message
        End Function

        <DllImport(libCName, EntryPoint:="getxattr", SetLastError:=True)>
        Shared Private Function GetXAttr(filePath As String, attribName As String, buffer As Byte(), bufferSize As Long) As Long
        End Function

        <DllImport(libCName, EntryPoint:="setxattr", SetLastError:=True)>
        Shared Private Function SetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, flags As Integer) As Long
        End Function

        <DllImport(libCName, EntryPoint:="removexattr", SetLastError:=True)>
        Shared Private Function RemoveXAttr(path As String, attribName As String) As Long
        End Function

        <DllImport(libCName, EntryPoint:="listxattr", SetLastError:=True)>
        Shared Private Function ListXAattr(filePath As String, nameBuffer As StringBuilder, size As Long) As Long
        End Function

        <DllImport(libCName, EntryPoint:="newlocale", SetLastError:=True)>
        Shared Private Function NewLocale(mask As Integer, locale As String, oldLocale As IntPtr) As IntPtr
        End Function

        <DllImport(libCName, EntryPoint:="freelocale", SetLastError:=True)>
        Shared Private Sub FreeLocale(locale As IntPtr)
        End Sub

        <DllImport(libCName, EntryPoint:="strerror_l", SetLastError:=True)>
        Shared Private Function StrErrorL(code As Integer, locale As IntPtr) As IntPtr
        End Function
    End Class
End Namespace
