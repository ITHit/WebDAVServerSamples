Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports System.Xml.Serialization
Imports System.Runtime.CompilerServices

Namespace ExtendedAttributes

    ''' <summary>
    ''' Provides extension methods to read and write extended attributes on file and folders.
    ''' </summary>
    ''' <remarks>This class uses file system extended attributes in case of OS X and Linux or NTFS alternative data streams in case of Windows.</remarks>
    Module FileSystemInfoExtension

        ''' <summary>
        ''' Depending on OS holds WindowsExtendedAttribute, OSXExtendedAttribute or LinuxExtendedAttribute class instance.
        ''' </summary>
        Private ReadOnly extendedAttribute As IExtendedAttribute

        ''' <summary>
        ''' Initializes static class members.
        ''' </summary>
        Sub New()
            If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                extendedAttribute = New WindowsExtendedAttribute()
            ElseIf RuntimeInformation.IsOSPlatform(OSPlatform.Linux) Then
                extendedAttribute = New LinuxExtendedAttribute()
            ElseIf RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
                extendedAttribute = New OSXExtendedAttribute()
            Else
                Throw New Exception("Not Supported OS")
            End If
        End Sub

        ''' <summary>
        ''' Determines whether extended attributes are supported.
        ''' </summary>
        ''' <param name="info"><see cref="FileSystemInfo"/>  instance.</param>
        ''' <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        <Extension()>
        Async Function IsExtendedAttributesSupportedAsync(info As FileSystemInfo) As Task(Of Boolean)
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            Return Await extendedAttribute.IsExtendedAttributesSupportedAsync(info.FullName)
        End Function

        ''' <summary>
        ''' Checks extended attribute existence.
        ''' </summary>  
        ''' <param name="info"><see cref="FileSystemInfo"/>  instance.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>True if attribute exist, false otherwise.</returns>
        <Extension()>
        Async Function HasExtendedAttributeAsync(info As FileSystemInfo, attribName As String) As Task(Of Boolean)
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Return Await extendedAttribute.GetExtendedAttributeAsync(info.FullName, attribName) IsNot Nothing
        End Function

        ''' <summary>
        ''' Gets extended attribute or null if attribute or file not found.
        ''' </summary>
        ''' <typeparam name="T">The value will be automatically deserialized to the type specified by this type-parameter.</typeparam>
        ''' <param name="info"><see cref="FileSystemInfo"/>  instance.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>Attribute value or null if attribute or file not found.</returns>
        <Extension()>
        Async Function GetExtendedAttributeAsync(Of T As New)(info As FileSystemInfo, attribName As String) As Task(Of T)
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim attributeValue As String = Await extendedAttribute.GetExtendedAttributeAsync(info.FullName, attribName)
            Return Deserialize(Of T)(attributeValue)
        End Function

        ''' <summary>
        ''' Sets extended attribute.
        ''' </summary>
        ''' <param name="info"><see cref="FileSystemInfo"/>  instance.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value.</param>
        ''' <remarks>Preserves file last modification date.</remarks>
        <Extension()>
        Async Function SetExtendedAttributeAsync(info As FileSystemInfo, attribName As String, attribValue As Object) As Task
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            If attribValue Is Nothing Then
                Throw New ArgumentNullException("attribValue")
            End If

            Dim serializedValue As String = Serialize(attribValue)
            ' As soon as Modified property is using FileSyatemInfo.LastWriteTimeUtc 
            ' we need to preserve it when updating or deleting extended attribute.
            Dim lastWriteTimeUtc As DateTime = info.LastWriteTimeUtc
            Await extendedAttribute.SetExtendedAttributeAsync(info.FullName, attribName, serializedValue)
            ' Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc
        End Function

        ''' <summary>
        ''' Deletes extended attribute.
        ''' </summary>
        ''' <param name="info"><see cref="FileSystemInfo"/>  instance.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <remarks>Preserves file last modification date.</remarks>
        <Extension()>
        Async Function DeleteExtendedAttributeAsync(info As FileSystemInfo, attribName As String) As Task
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            ' As soon as Modified property is using FileSyatemInfo.LastWriteTimeUtc 
            ' we need to preserve it when updating or deleting extended attribute.
            Dim lastWriteTimeUtc As DateTime = info.LastWriteTimeUtc
            Await extendedAttribute.DeleteExtendedAttributeAsync(info.FullName, attribName)
            ' Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc
        End Function

        ''' <summary>
        ''' Serializes object to XML string.
        ''' </summary>
        ''' <param name="data">Object to be serialized.</param>
        ''' <returns>String representation of an object.</returns>
        Private Function Serialize(data As Object) As String
            If data Is Nothing Then
                Throw New ArgumentNullException("data")
            End If

            Dim xmlSerializer As XmlSerializer = New XmlSerializer(data.GetType())
            Dim stringBulder As StringBuilder = New StringBuilder()
            Using stringWriter As StringWriter = New StringWriter(stringBulder,
                                                                 System.Globalization.CultureInfo.InvariantCulture)
                xmlSerializer.Serialize(stringWriter, data)
                Return stringBulder.ToString()
            End Using
        End Function

        ''' <summary>
        ''' Deserializes XML string to an object of a specified type.
        ''' </summary>
        ''' <typeparam name="T"></typeparam>
        ''' <param name="xmlString">XML string to be deserialized.</param>
        ''' <returns>Deserialized object. If xmlString is empty or null returns new empty instance of an object.</returns>
        Private Function Deserialize(Of T As New)(xmlString As String) As T
            If String.IsNullOrEmpty(xmlString) Then
                Return New T()
            End If

            Dim xmlSerializer As XmlSerializer = New XmlSerializer(GetType(T))
            Using reader As StringReader = New StringReader(xmlString)
                Return CType(xmlSerializer.Deserialize(reader), T)
            End Using
        End Function
    End Module
End Namespace
