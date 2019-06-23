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

            Dim attributeCount As Long = ListXAattr(path, New StringBuilder(), 0)
            Return attributeCount <> -1
        End Function

        ''' <summary>
        ''' Checks extended attribute existence.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>True if attribute exist, false otherwise.</returns>
        Public Async Function HasExtendedAttributeAsync(path As String, attribName As String) As Task(Of Boolean) Implements IExtendedAttribute.HasExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentNullException("path")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim attributeSize As Long = GetXAttr(path, userAttributeName, New Byte(-1) {}, 0)
            Dim attributeExists As Boolean = True
            If attributeSize = -1 Then
                If Marshal.GetLastWin32Error() = AttributeNotFoundErrno Then
                    attributeExists = False
                Else
                    ThrowLastException(path, userAttributeName)
                End If
            End If

            Return attributeExists
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

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim attributeSize As Long = GetXAttr(path, userAttributeName, New Byte(-1) {}, 0)
            Dim buffer As Byte() = New Byte(attributeSize - 1) {}
            Dim readedLength As Long = GetXAttr(path, userAttributeName, buffer, attributeSize)
            If readedLength = -1 Then
                ThrowLastException(path, userAttributeName)
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

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim buffer As Byte() = Encoding.UTF8.GetBytes(attribValue)
            Dim result As Long = SetXAttr(path, userAttributeName, buffer, buffer.Length, 0)
            If result = -1 Then
                ThrowLastException(path, userAttributeName)
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

            Dim userAttributeName As String = String.Format(attributeNameFormat, attribName)
            Dim result As Long = RemoveXAttr(path, userAttributeName)
            If result = -1 Then
                ThrowLastException(path, userAttributeName)
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
            ' Init locale structure
            Dim locale As IntPtr = NewLocale(8127, "C", IntPtr.Zero)
            If locale = IntPtr.Zero Then
                Throw New InvalidOperationException("Not able to get locale")
            End If

            ' Get error message for error number
            Dim message As String = Marshal.PtrToStringAnsi(StrErrorL(errno, locale))
            ' Free locale
            FreeLocale(locale)
            Return message
        End Function

        ''' <summary>
        ''' External func getxattr from libc, what returns custom attribute by name.
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="buffer">Buffer to collect attribute value.</param>
        ''' <param name="bufferSize">Buffer size.</param>
        ''' <returns>Attribute value size in bytes, when returning value -1 than some error occurred.</returns>
        <DllImport(libCName, EntryPoint:="getxattr", SetLastError:=True)>
        Shared Private Function GetXAttr(filePath As String, attribName As String, buffer As Byte(), bufferSize As Long) As Long
        End Function

        ''' <summary>
        ''' External func setxattr from libc, sets attribute value for file by name. 
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value</param>
        ''' <param name="size">Attribute value size</param>
        ''' <param name="flags">Flags.</param>
        ''' <returns>Status, when returning value -1 than some error occurred.</returns>
        <DllImport(libCName, EntryPoint:="setxattr", SetLastError:=True)>
        Shared Private Function SetXAttr(filePath As String, attribName As String, attribValue As Byte(), size As Long, flags As Integer) As Long
        End Function

        ''' <summary>
        ''' Removes the extended attribute. 
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>On success, zero is returned. On failure, -1 is returned.</returns>
        <DllImport(libCName, EntryPoint:="removexattr", SetLastError:=True)>
        Shared Private Function RemoveXAttr(path As String, attribName As String) As Long
        End Function

        ''' <summary>
        ''' External func listxattr from libc, what returns list of attributes separated null-terminated string.
        ''' </summary>
        ''' <param name="filePath">File path.</param>
        ''' <param name="nameBuffer">Attribute name.</param>
        ''' <param name="size">Buffer size</param>
        ''' <returns>Attributes bytes array size, when returning value -1 than some error occurred</returns>
        <DllImport(libCName, EntryPoint:="listxattr", SetLastError:=True)>
        Shared Private Function ListXAattr(filePath As String, nameBuffer As StringBuilder, size As Long) As Long
        End Function

        ''' <summary>
        ''' External func newlocale from libc, what initializes locale.
        ''' </summary>
        ''' <param name="mask">Category mask.</param>
        ''' <param name="locale">Locale name.</param>
        ''' <param name="oldLocale">Old locale.</param>
        ''' <returns>Pointer to locale structure.</returns>
        <DllImport(libCName, EntryPoint:="newlocale", SetLastError:=True)>
        Shared Private Function NewLocale(mask As Integer, locale As String, oldLocale As IntPtr) As IntPtr
        End Function

        ''' <summary>
        ''' External func freelocale from libc, what deallocates locale.
        ''' </summary>
        ''' <param name="locale">Locale structure.</param>
        <DllImport(libCName, EntryPoint:="freelocale", SetLastError:=True)>
        Shared Private Sub FreeLocale(locale As IntPtr)
        End Sub

        ''' <summary>
        ''' External func strerror_l from libc, what returns string that describes the error code passed in the argument.
        ''' </summary>
        ''' <param name="code">Error code.</param>
        ''' <param name="locale">Locale to display message in.</param>
        ''' <returns>Localized error message</returns>
        <DllImport(libCName, EntryPoint:="strerror_l", SetLastError:=True)>
        Shared Private Function StrErrorL(code As Integer, locale As IntPtr) As IntPtr
        End Function
    End Class
End Namespace
