Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav
Imports CalDAVServer.SqlStorage.AspNet.CalDav

''' <summary>
''' Assists in finding folders that contain calendars and address books.
''' </summary>
Public Class Discovery
    Implements ICalendarDiscoveryAsync

    ''' <summary>
    ''' Instance of <see cref="DavContext"/> .
    ''' </summary>
    Protected Context As DavContext

    Public Sub New(context As DavContext)
        Me.Context = context
    End Sub

    ''' <summary>
    ''' Returns list of folders that contain calendars owned by this principal.
    ''' </summary>
    ''' <remarks>This enables calendars discovery owned by current loged-in principal.</remarks>
    Public Async Function GetCalendarHomeSetAsync() As Task(Of IEnumerable(Of IItemCollectionAsync)) Implements ICalendarDiscoveryAsync.GetCalendarHomeSetAsync
        Return {New CalendarsRootFolder(Context)}
    End Function

    ''' <summary>
    ''' Returns <b>true</b> if <b>calendar-home-set</b> feature is enabled, <b>false</b> otherwise.
    ''' </summary>
    ''' <remarks>
    ''' <para>
    ''' In this method you can analyze User-Agent header to find out the client application used for accessing the server
    ''' and enable/disable <b>calendar-home-set</b> feature for specific client. 
    ''' </para>
    ''' <para>
    ''' iOS and OS X does require <b>calendar-home-set</b> feature to be always enabled. On the other hand it may consume extra 
    ''' resources. Some CalDAV clients start immediate synchronization of all calendars found on the server 
    ''' via home-set request. Typically you will always enable home-set for iOS and OS X CalDAV clients, but may disable it for other clients.
    ''' </para>
    ''' </remarks>
    Public ReadOnly Property CalendarHomeSetEnabled As Boolean Implements ICalendarDiscoveryAsync.CalendarHomeSetEnabled
        Get
            Return True
        End Get
    End Property
End Class
