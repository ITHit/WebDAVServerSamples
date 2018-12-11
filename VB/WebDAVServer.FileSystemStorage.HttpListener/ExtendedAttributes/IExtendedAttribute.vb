Imports System.Threading.Tasks

Namespace ExtendedAttributes

    Interface IExtendedAttribute

        ''' <summary>
        ''' Determines whether extended attributes are supported.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        Function IsExtendedAttributesSupportedAsync(path As String) As Task(Of Boolean)

        ''' <summary>
        ''' Gets extended attribute or null if attribute or file not found.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>Attribute value or null if attribute or file not found.</returns>
        Function GetExtendedAttributeAsync(path As String, attribName As String) As Task(Of String)

        ''' <summary>
        ''' Sets extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value.</param>
        Function SetExtendedAttributeAsync(path As String, attribName As String, attribValue As String) As Task

        ''' <summary>
        ''' Deletes extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        Function DeleteExtendedAttributeAsync(path As String, attribName As String) As Task

        ''' <summary>
        ''' Deletes all extended attributes.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        Function DeleteExtendedAttributes(path As String) As Task

        ''' <summary>
        ''' Copies all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Function CopyExtendedAttributes(sourcePath As String, destinationPath As String) As Task

        ''' <summary>
        ''' Moves all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Function MoveExtendedAttributes(sourcePath As String, destinationPath As String) As Task

    End Interface
End Namespace
