Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports CalDAVServer.SqlStorage.AspNet.Acl
Imports CalDAVServer.SqlStorage.AspNet.CalDav

''' <summary>
''' Logical folder which contains /acl/, /calendars/ and /addressbooks/ folders.
''' Represents a folder with the following path: [DAVLocation]
''' </summary>
''' <example>
''' [DavLocation]
'''  |-- acl
'''  |-- calendars
'''  |-- addressbooks
''' </example>
Public Class DavLocationFolder
    Inherits LogicalFolder

    ''' <summary>
    ''' Path to this folder.
    ''' </summary>
    ''' <value>Returns first non-root path from DavLocation section from config file or "/" if no DavLocation section is found.</value>
    Public Shared ReadOnly Property DavLocationFolderPath As String
        Get
            Dim davLocationsSection As NameValueCollection = CType(System.Configuration.ConfigurationManager.GetSection("davLocations"), NameValueCollection)
            If davLocationsSection IsNot Nothing Then
                For Each path As String In davLocationsSection.AllKeys
                    If Not String.IsNullOrEmpty(path.Trim("/"c)) Then Return path.TrimEnd("/"c) & "/"c
                Next
            End If

            Return "/"
        End Get
    End Property

    ''' <summary>
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/></param>
    Public Sub New(context As DavContext)
        MyBase.New(context, DavLocationFolderPath)
    End Sub

    Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync))
        Return New IHierarchyItemAsync() {New AclFolder(Context), New CalendarsRootFolder(Context)}
    End Function
End Class
