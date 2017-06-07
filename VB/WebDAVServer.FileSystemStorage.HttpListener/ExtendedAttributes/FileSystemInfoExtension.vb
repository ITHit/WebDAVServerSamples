Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports System.Xml.Serialization
Imports System.Runtime.CompilerServices

Namespace ExtendedAttributes

    Module FileSystemInfoExtension

        ''' <summary>
        ''' Depending on OS holds WindowsExtendedAttribute, OSXExtendedAttribute or LinuxExtendedAttribute class instance.
        ''' </summary>
        Private ReadOnly extendedAttribute As IExtendedAttribute

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

        <Extension()>
        Async Function IsExtendedAttributesSupportedAsync(info As FileSystemInfo) As Task(Of Boolean)
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            Return Await extendedAttribute.IsExtendedAttributesSupportedAsync(info.FullName)
        End Function

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
            Dim lastWriteTimeUtc As DateTime = info.LastWriteTimeUtc
            Await extendedAttribute.SetExtendedAttributeAsync(info.FullName, attribName, serializedValue)
            ' Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc
        End Function

        <Extension()>
        Async Function DeleteExtendedAttributeAsync(info As FileSystemInfo, attribName As String) As Task
            If info Is Nothing Then
                Throw New ArgumentNullException("info")
            End If

            If String.IsNullOrEmpty(attribName) Then
                Throw New ArgumentNullException("attribName")
            End If

            Dim lastWriteTimeUtc As DateTime = info.LastWriteTimeUtc
            Await extendedAttribute.DeleteExtendedAttributeAsync(info.FullName, attribName)
            ' Restore last write time.
            info.LastWriteTimeUtc = lastWriteTimeUtc
        End Function

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
