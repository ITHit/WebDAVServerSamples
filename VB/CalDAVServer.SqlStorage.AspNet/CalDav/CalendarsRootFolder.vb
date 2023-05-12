Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.IO
Imports System.Data
Imports System.Data.SqlClient
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Paging

Namespace CalDav

    ''' <summary>
    ''' Folder that contains calendars.
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/
    ''' </summary>
    Public Class CalendarsRootFolder
        Inherits LogicalFolder
        Implements IFolder

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly calendarsRootFolderName As String = "calendars"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared CalendarsRootFolderPath As String = DavLocationFolder.DavLocationFolderPath & calendarsRootFolderName & "/"c

        Public Sub New(context As DavContext)
            MyBase.New(context, CalendarsRootFolderPath)
        End Sub

        ''' <summary>
        ''' Retrieves children of this folder.
        ''' </summary>
        ''' <param name="propNames">Properties requested by client application for each child.</param>
        ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        ''' <param name="nResults">The number of items to return.</param>
        ''' <param name="orderProps">List of order properties requested by the client.</param>
        ''' <returns>Children of this folder.</returns>
        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollection.GetChildrenAsync
            ' Here we list calendars from back-end storage. 
            ' You can filter calendars if requied and return only calendars that user has access to.
            Return New PageResults((Await CalendarFolder.LoadAllAsync(Context)).OrderBy(Function(x) x.Name), Nothing)
        End Function

        Public Function CreateFileAsync(name As String, content As Stream, contentType As String, totalFileSize As Long) As Task(Of IFile) Implements IFolder.CreateFileAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Creates a new calendar.
        ''' </summary>
        ''' <param name="name">Name of the new calendar.</param>
        Public Async Function CreateFolderAsync(name As String) As Task(Of IFolder) Implements IFolder.CreateFolderAsync
            Return Await CalendarFolder.CreateCalendarFolderAsync(Context, name, "")
        End Function
    End Class
End Namespace
