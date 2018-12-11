Imports System
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes

Namespace ExtendedAttributes

    ''' <summary>
    ''' Provides extension methods to read and write extended attributes on file and folders.
    ''' </summary>
    ''' <remarks>This class uses file system to store extended attributes in case of alternative data streams not supported.</remarks>
    Public Class FileSystemExtendedAttribute
        Implements IExtendedAttribute

        ''' <summary>
        ''' Gets path used to store extended attributes data.
        ''' </summary>
        Public ReadOnly Property StoragePath As String

        ''' <summary>
        ''' Gets path directory used as root of files. If set stores attributes in storage relative to it.
        ''' </summary>
        Public ReadOnly Property DataStoragePath As String

        ''' <summary>
        ''' Creates instance of <see cref="FileSystemExtendedAttribute"/> 
        ''' </summary>
        ''' <param name="attrStoragePath">Used as path to store attributes data.</param>
        ''' <param name="dataStoragePath">Used as path to store attributes data path relative of.</param>
        ''' <exception cref="ArgumentNullException">Thrown if <paramref name="attrStoragePath"/>  or <paramref name="dataStoragePath"/>  is <c>null</c> or an empty string.</exception>
        Public Sub New(attrStoragePath As String, dataStoragePath As String)
            If String.IsNullOrEmpty(attrStoragePath) Then Throw New ArgumentNullException(NameOf(attrStoragePath))
            If String.IsNullOrEmpty(dataStoragePath) Then Throw New ArgumentNullException(NameOf(dataStoragePath))
            Me.StoragePath = System.IO.Path.GetFullPath(attrStoragePath)
            Me.DataStoragePath = System.IO.Path.GetFullPath(dataStoragePath)
        End Sub

        ''' <summary>
        ''' Determines whether extended attributes are supported.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        Public Async Function IsExtendedAttributesSupportedAsync(path As String) As Task(Of Boolean) Implements IExtendedAttribute.IsExtendedAttributesSupportedAsync
            Return False
        End Function

        ''' <summary>
        ''' Gets extended attribute or null if attribute or file not found.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <returns>Attribute value or null if attribute or file not found.</returns>
        Public Async Function GetExtendedAttributeAsync(path As String, attribName As String) As Task(Of String) Implements IExtendedAttribute.GetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))
            If String.IsNullOrEmpty(attribName) Then Throw New ArgumentNullException(NameOf(attribName))
            Dim attrPath As String = Me.GetAttrFullPath(path, attribName)
            If Not File.Exists(attrPath) Then Return Nothing
            Using fileStream As FileStream = File.Open(attrPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using reader As StreamReader = New StreamReader(fileStream, Encoding.UTF8)
                    Return Await reader.ReadToEndAsync()
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Gets the file path where attribute files stores.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName"> The attribute name.</param>
        ''' <returns>The full file path relative to <see cref="StoragePath"/>  depends on <see cref="DataStoragePath"/> .</returns>
        Private Function GetAttrFullPath(path As String, attribName As String) As String
            Dim attrRootPath As String = GetAttrRootPath(path)
            Return System.IO.Path.Combine(attrRootPath, attribName)
        End Function

        Private Shared Function GetPathWithoutVolumeSeparator(path As String) As String
            If System.IO.Path.VolumeSeparatorChar = System.IO.Path.DirectorySeparatorChar Then Return path
            Return path.Replace(System.IO.Path.VolumeSeparatorChar.ToString(), String.Empty)
        End Function

        ''' <summary>
        ''' Sets extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        ''' <param name="attribValue">Attribute value.</param>
        Public Async Function SetExtendedAttributeAsync(path As String, attribName As String, attribValue As String) As Task Implements IExtendedAttribute.SetExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))
            If String.IsNullOrEmpty(attribName) Then Throw New ArgumentNullException(NameOf(attribName))
            If String.IsNullOrEmpty(attribValue) Then Throw New ArgumentNullException(NameOf(attribValue))
            Dim attrSubTreePath As String = Me.GetAttrRootPath(path)
            If Not Directory.Exists(attrSubTreePath) Then
                Directory.CreateDirectory(attrSubTreePath)
            End If

            Dim attrPath As String = System.IO.Path.Combine(attrSubTreePath, attribName)
            File.WriteAllText(attrPath, attribValue, Encoding.UTF8)
        End Function

        ''' <summary>
        ''' Gets the directory where attribute files stores.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <returns>The full directory path relative to <see cref="StoragePath"/>  depends on <see cref="DataStoragePath"/> .</returns>
        Private Function GetAttrRootPath(path As String) As String
            If Not String.IsNullOrEmpty(DataStoragePath) Then
                Return System.IO.Path.Combine(Me.StoragePath, GetSubDirectoryPath(DataStoragePath, path))
            End If

            Dim encodedPath As String = GetPathWithoutVolumeSeparator(System.IO.Path.GetFullPath(path))
            encodedPath = encodedPath.TrimStart(System.IO.Path.DirectorySeparatorChar)
            Return System.IO.Path.Combine(Me.StoragePath, encodedPath)
        End Function

        ''' <summary>
        ''' Deletes extended attribute.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        ''' <param name="attribName">Attribute name.</param>
        Public Async Function DeleteExtendedAttributeAsync(path As String, attribName As String) As Task Implements IExtendedAttribute.DeleteExtendedAttributeAsync
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))
            If String.IsNullOrEmpty(attribName) Then Throw New ArgumentNullException(NameOf(attribName))
            Dim attrPath As String = Me.GetAttrFullPath(path, attribName)
            If File.Exists(attrPath) Then File.Delete(attrPath)
        End Function

        ''' <summary>
        ''' Deletes all extended attributes.
        ''' </summary>
        ''' <param name="path">File or folder path.</param>
        Public Async Function DeleteExtendedAttributes(path As String) As Task Implements IExtendedAttribute.DeleteExtendedAttributes
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))
            Dim attrSubTreePath As String = Me.GetAttrRootPath(path)
            If Directory.Exists(attrSubTreePath) Then
                Directory.Delete(attrSubTreePath, True)
            End If
        End Function

        ''' <summary>
        ''' Copies all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Public Async Function CopyExtendedAttributes(sourcePath As String, destinationPath As String) As Task Implements IExtendedAttribute.CopyExtendedAttributes
            If String.IsNullOrEmpty(sourcePath) Then Throw New ArgumentNullException(NameOf(sourcePath))
            If String.IsNullOrEmpty(destinationPath) Then Throw New ArgumentNullException(NameOf(destinationPath))
            Dim sourceSubTreePath As String = Me.GetAttrRootPath(sourcePath)
            If Not Directory.Exists(sourceSubTreePath) Then
                Return
            End If

            Dim destSubTreePath As String = Me.GetAttrRootPath(destinationPath)
            DirectoryCopy(sourceSubTreePath, destSubTreePath)
        End Function

        ''' <summary>
        ''' Moves all extended attributes.
        ''' </summary>
        ''' <param name="sourcePath">The source path. </param>
        ''' <param name="destinationPath">The target pat.</param>
        Public Async Function MoveExtendedAttributes(sourcePath As String, destinationPath As String) As Task Implements IExtendedAttribute.MoveExtendedAttributes
            If String.IsNullOrEmpty(sourcePath) Then Throw New ArgumentNullException(NameOf(sourcePath))
            If String.IsNullOrEmpty(destinationPath) Then Throw New ArgumentNullException(NameOf(destinationPath))
            Dim sourceSubTreePath As String = Me.GetAttrRootPath(sourcePath)
            If Not Directory.Exists(sourceSubTreePath) Then
                Return
            End If

            Dim destSubTreePath As String = Me.GetAttrRootPath(destinationPath)
            Dim parentDirectory As DirectoryInfo = Directory.GetParent(destSubTreePath)
            If Not parentDirectory.Exists Then
                parentDirectory.Create()
            End If

            Directory.Move(sourceSubTreePath, destSubTreePath)
        End Function

        ''' <summary>
        ''' Copies directory and its contents to a new location.
        ''' </summary>
        ''' <param name="sourceDirName">The path of the file or directory to copy.</param>
        ''' <param name="destDirName">The path to the new location for <paramref name="sourceDirName"/> .</param>
        ''' <exception cref="ArgumentNullException">Thrown if <paramref name="sourceDirName"/>  or <paramref name="destDirName"/>  is <c>null</c> or an empty string.</exception>
        ''' <exception cref="DirectoryNotFoundException">Thrown if <paramref name="sourceDirName"/>  does not exists.</exception>
        Private Shared Sub DirectoryCopy(sourceDirName As String, destDirName As String)
            If String.IsNullOrEmpty(sourceDirName) Then Throw New ArgumentNullException(NameOf(sourceDirName))
            If String.IsNullOrEmpty(destDirName) Then Throw New ArgumentNullException(NameOf(destDirName))
            Dim dir As DirectoryInfo = New DirectoryInfo(sourceDirName)
            If Not dir.Exists Then
                Throw New DirectoryNotFoundException("Source directory does not exist or could not be found: " & sourceDirName)
            End If

            Dim dirs As DirectoryInfo() = dir.GetDirectories()
            If Not Directory.Exists(destDirName) Then
                Directory.CreateDirectory(destDirName)
            End If

            Dim files As FileInfo() = dir.GetFiles()
            For Each file As FileInfo In files
                Dim destPath As String = System.IO.Path.Combine(destDirName, file.Name)
                file.CopyTo(destPath, False)
            Next

            For Each subdir As DirectoryInfo In dirs
                Dim destPath As String = Path.Combine(destDirName, subdir.Name)
                DirectoryCopy(subdir.FullName, destPath)
            Next
        End Sub

        ''' <summary>
        ''' Create a sub directory path from one path to another. Paths will be resolved before calculating the difference.
        ''' </summary>
        ''' <param name="relativeTo">The source path the output should sub directory of. This path is always considered to be a directory.</param>
        ''' <param name="path">The destination path.</param>
        ''' <returns>The sub directory path path or <paramref name="path"/>  if the paths don't sub directory of <paramref name="path"/> .</returns>
        ''' <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/>  or <paramref name="path"/>  is <c>null</c> or an empty string.</exception>
        Private Shared Function GetSubDirectoryPath(relativeTo As String, path As String) As String
            If String.IsNullOrEmpty(relativeTo) Then Throw New ArgumentNullException(NameOf(relativeTo))
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))
            Dim fullRelativeTo As String = System.IO.Path.GetFullPath(relativeTo)
            Dim fullPath As String = System.IO.Path.GetFullPath(path)
            If Not fullPath.StartsWith(fullRelativeTo, StringComparison.InvariantCulture) Then
                Return fullPath
            End If

            fullRelativeTo = AppendTrailingSeparator(fullRelativeTo)
            fullPath = AppendTrailingSeparator(fullPath)
            Dim relativePath As String = fullPath.Replace(fullRelativeTo, String.Empty)
            If String.IsNullOrEmpty(relativePath) Then
                Return fullPath
            End If

            Return relativePath.TrimEnd(System.IO.Path.DirectorySeparatorChar)
        End Function

        ''' <summary>
        ''' Adds <see cref="System.IO.Path.DirectorySeparatorChar"/>  to end of string if not exists.
        ''' </summary>
        ''' <param name="fullPath">The string to add.</param>
        ''' <returns>The string with <see cref="System.IO.Path.DirectorySeparatorChar"/>  at the end</returns>
        Private Shared Function AppendTrailingSeparator(fullPath As String) As String
            If Not fullPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) Then
                fullPath += System.IO.Path.DirectorySeparatorChar
            End If

            Return fullPath
        End Function
    End Class
End Namespace
