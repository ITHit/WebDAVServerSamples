Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging
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
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/></param>
    Public Sub New(context As DavContext)
        MyBase.New(context, DavLocationFolderPath)
    End Sub

    ''' <summary>
    ''' Retrieves children of this folder: /acl/, /calendars/ and /addressbooks/ folders.
    ''' </summary>
    ''' <param name="propNames">Properties requested by client application for each child.</param>
    ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
    ''' <param name="nResults">The number of items to return.</param>
    ''' <param name="orderProps">List of order properties requested by the client.</param>
    ''' <returns>Children of this folder.</returns>
    Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults)
        ' In this samle we list users folder only. Groups and groups folder is not implemented.
        Return New PageResults(New IHierarchyItemAsync() {New AclFolder(Context), New CalendarsRootFolder(Context)}, Nothing)
    End Function
End Class
