Imports System
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Linq
Imports ITHit.Collab
Imports ITHit.Collab.Calendar
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav

Namespace CalDav

    ''' <summary>
    ''' Represents a calendar file on a CalDAV server. Typically contains a single event or to-do in iCalendar format. 
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/[user_name]/[calendar_name]/[file_name].ics.
    ''' </summary>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- calendars
    '''      |-- ...
    '''      |-- [User2]
    '''           |-- [Calendar 1]
    '''           |-- ...
    '''           |-- [Calendar X]
    '''                |-- [File 1.ics]  -- this class
    '''                |-- ...
    '''                |-- [File X.ics]  -- this class
    ''' </example>
    Public Class CalendarFile
        Inherits DavFile
        Implements ICalendarFileAsync

        Public Shared Function GetCalendarFile(context As DavContext, path As String) As CalendarFile
            Dim pattern As String = String.Format("^/?{0}/(?<user_name>[^/]+)/(?<calendar_name>[^/]+)/(?<file_name>[^/]+\.ics)$",
                                                 CalendarsRootFolder.CalendarsRootFolderPath.Trim(New Char() {"/"c}).Replace("/", "/?"))
            If Not Regex.IsMatch(path, pattern) Then Return Nothing
            Dim file As FileInfo = New FileInfo(context.MapPath(path))
            If Not file.Exists Then Return Nothing
            Return New CalendarFile(file, context, path)
        End Function

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CalendarFile"/>  class.
        ''' </summary>
        ''' <param name="file"><see cref="FileInfo"/>  for corresponding object in file system.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        Private Sub New(file As FileInfo, context As DavContext, path As String)
            MyBase.New(file, context, path)
        End Sub

        Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            Dim calendarObjectContent As String = File.ReadAllText(fileSystemInfo.FullName)
            Await MyBase.DeleteAsync(multistatus)
            Dim calendars As IEnumerable(Of IComponent) = New vFormatter().Deserialize(calendarObjectContent)
            Dim calendar As ICalendar2 = TryCast(calendars.First(), ICalendar2)
            calendar.Method = calendar.CreateMethodProp(MethodType.Cancel)
            Await iMipEventSchedulingTransport.NotifyAttendeesAsync(context, calendar)
        End Function

        Public Overrides Async Function WriteAsync(content As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
            Dim result As Boolean = Await MyBase.WriteAsync(content, contentType, startIndex, totalFileSize)
            Dim calendarObjectContent As String = File.ReadAllText(fileSystemInfo.FullName)
            Dim calendars As IEnumerable(Of IComponent) = New vFormatter().Deserialize(calendarObjectContent)
            Dim calendar As ICalendar2 = TryCast(calendars.First(), ICalendar2)
            calendar.Method = calendar.CreateMethodProp(MethodType.Request)
            Await iMipEventSchedulingTransport.NotifyAttendeesAsync(context, calendar)
            Return result
        End Function
    End Class
End Namespace
