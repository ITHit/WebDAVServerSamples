Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.IO
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

''' <summary>
''' Logical folder which contains /acl/, /calendars/ and /addressbooks/ folders.
''' Represents a folder with the following path: [DAVLocation]
''' </summary>
''' <example>
''' [DavLocation]  -- this class
'''  |-- acl
'''  |-- calendars
'''  |-- addressbooks
''' </example>
Public Class DavLocationFolder
    Inherits DavFolder

    ''' <summary>
    ''' Path to this folder.
    ''' </summary>
    ''' <value>Returns first non-root path from DavLocation section from config file or "/" if no DavLocation section is found.</value>
    Public Shared ReadOnly Property DavLocationFolderPath As String
        Get
            Dim davLocationsSection As NameValueCollection = CType(System.Configuration.ConfigurationManager.GetSection("davLocations"), NameValueCollection)
            If davLocationsSection IsNot Nothing Then
                For Each path As String In davLocationsSection.AllKeys
                    ' Typically you will enable WebDAV on site root ('/') to allow CalDAV/CardDAV 
                    ' discovery. We skip site root WebDAV location to find first non-root location.
                    If Not String.IsNullOrEmpty(path.Trim("/"c)) Then Return path.TrimEnd("/"c) & "/"c
                Next
            End If

            ' If no davLocation section is found or no non-root WebDAV location is specified in 
            ' configuration file asume the WebDAV is on web site root.
            Return "/"
        End Get
    End Property

    ''' <summary>
    ''' Returns DavLocationFolder folder if path corresponds to [DavLocation].
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/></param>
    ''' <param name="path">Encoded path relative to WebDAV root.</param>
    ''' <returns>DavLocationFolder instance or null if physical folder not found in file system.</returns>
    Public Shared Function GetDavLocationFolder(context As DavContext, path As String) As DavLocationFolder
        Dim davPath As String = DavLocationFolderPath
        If Not path.Equals(davPath.Trim({"/"c}), StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Dim folderPath As String = context.MapPath(davPath).TrimEnd(System.IO.Path.DirectorySeparatorChar)
        Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
        If Not folder.Exists Then Throw New Exception(String.Format("Can not find folder that corresponds to '{0}' ([DavLocation] folder) in file system.", davPath))
        Return New DavLocationFolder(folder, context, davPath)
    End Function

    ''' <summary>
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="directory">Instance of <see cref="DirectoryInfo"/>  class with information about the folder in file system.</param>
    ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
    ''' <param name="path">Relative to WebDAV root folder path.</param>
    Private Sub New(directory As DirectoryInfo, context As DavContext, path As String)
        MyBase.New(directory, context, path)
    End Sub

    ''' <summary>
    ''' Retrieves children of this folder: /acl/, /calendars/ and /addressbooks/ folders.
    ''' </summary>
    ''' <param name="propNames">Properties requested by client application for each child.</param>
    ''' <returns>Children of this folder.</returns>
    Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync))
        Dim children As List(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
        ' At the upper level we have folder named [DavLocation]/acl/ which stores users and groups.
        ' This is a 'virtual' folder, it does not exist in file system.
        children.Add(New Acl.AclFolder(context))
        ' Get [DavLocation]/calendars/ and [DavLocation]/addressbooks/ folders.
        children.AddRange(Await MyBase.GetChildrenAsync(propNames))
        Return children
    End Function
End Class
