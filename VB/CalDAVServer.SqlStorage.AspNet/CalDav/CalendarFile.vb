Imports System
Imports System.Linq
Imports System.IO
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav
Imports ITHit.Collab
Imports ITHit.Collab.Calendar

Namespace CalDav

    ' +--- Calendar file [UID1].ics ---------+
    ' |                                      |
    ' |  +-- Time zone component ---------+  | -- Time zones are are not stored in DB, they are generated automatically 
    ' |  | TZID: Zone X                   |  |    during serialization, based on TZIDs found in event or to-do.
    ' |  | ...                            |  |
    ' |  +--------------------------------+  |
    ' |                                      |
    ' |  +-- Time zone component ---------+  |
    ' |  | TZID: Zone Y                   |  | -- Time zone IDs could be either IANA (Olson) IDs or System (Windows) IDs.
    ' |  | ...                            |  |
    ' |  +--------------------------------+  |
    ' |  ...                                 |
    ' |                                      |
    ' |                                      |
    ' |  +-- Event component -------------+  | -- Event / do-do components are stored in [cal_EventComponent] table.
    ' |  | UID: [UID1]                    |  |    
    ' |  | RRULE: FREQ=DAILY              |  | 
    ' |  | SUMMARY: Event A               |  |
    ' |  | ...                            |  |
    ' |  +--------------------------------+  |
    ' |                                      |
    ' |  +-- Event component -------------+  | -- In case of recurring events/to-dos there could be more than one component
    ' |  | UID: [UID1]                    |  |    per file. All event/to-do components within a single calendar file share
    ' |  | RECURRENCE-ID: 20151028        |  |    the same UID but have different RECURRENCE-IDs. 
    ' |  | SUMMARY: Instance 5 of Event A |  |    
    ' |  | ...                            |  |    iOS / OS X UIDs are case sensitive (uppercase GUIDs).
    ' |  +--------------------------------+  |    Bynari WebDAV Collaborator for MS Outlook UIDs are over 100 chars in length.
    ' |  ...                                 |
    ' |                                      |
    ' |                                      |
    ' +--------------------------------------+
    ' 
    ' 
    ' 
    '    +-- Event component -------------+
    '    |                                |
    '    | UID: [UID1]                    | 
    '    | SUMMARY: Event A               |
    '    | START: 20151016T080000         |
    '    | RRULE: FREQ=DAILY              |
    '    | ...                            |
    '    |                                |
    '    | EXDATE: 20151018T080000        | -- Recurrence exception dates are stored in [cal_RecurrenceException] table.
    '    | EXDATE: 20151020T080000        |
    '    | ...                            |
    '    |                                |
    '    | ATTENDEE: mail1@server.com     | -- Attendees are stored in [cal_Attendee] table.
    '    | ATTENDEE: mail2@srvr.com       |
    '    | ...                            |
    '    |                                |
    '    | ATTACH: /9j/4VGuf+Sw...        | -- Attachments are stored in [cal_Attachment] table.
    '    | ATTACH: https:'serv/file.docx |
    '    | ...                            |
    '    |                                |
    '    |  +-- Alarm Component -------+  | -- Alarms are stored in [cal_Alarm] table.
    '    |  | ACTION: DISPLAY          |  |
    '    |  | ...                      |  |
    '    |  +--------------------------+  |
    '    |                                |
    '    |  +-- Alarm Component -------+  |
    '    |  | ACTION: EMAIL            |  |
    '    |  | ...                      |  |
    '    |  +--------------------------+  |
    '    |  ...                           |
    '    |                                |
    '    +--------------------------------+
    ''' <summary>
    ''' Represents a calendar file. Every clendar file stores an event or to-do that consists of one or 
    ''' more event / to-do components and time zones descriptions.
    ''' 
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/[CalendarFolderId]/[UID].ics
    ''' </summary>
    Public Class CalendarFile
        Inherits DavHierarchyItem
        Implements ICalendarFileAsync

        ''' <summary>
        ''' Calendar file extension.
        ''' </summary>
        Public Shared Extension As String = ".ics"

        Public Shared Async Function LoadByCalendarFolderIdAsync(context As DavContext, calendarFolderId As Guid, propsToLoad As PropsToLoad) As Task(Of IEnumerable(Of ICalendarFileAsync))
            If propsToLoad <> PropsToLoad.Minimum Then Throw New NotImplementedException("LoadByCalendarFolderIdAsync is implemented only with PropsToLoad.Minimum.")
            Dim sql As String = "SELECT * FROM [cal_CalendarFile] 
                           WHERE [CalendarFolderId] = @CalendarFolderId
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                          ;SELECT [UID], [Summary] FROM [cal_EventComponent] 
                           WHERE [UID] IN (SELECT [UID] FROM [cal_CalendarFile] 
                                           WHERE [CalendarFolderId] = @CalendarFolderId)"
            sql = String.Format(sql, GetScPropsToLoad(propsToLoad))
            Return Await LoadAsync(context, sql,
                                  "@UserId", context.UserId,
                                  "@CalendarFolderId", calendarFolderId)
        End Function

        Public Shared Async Function LoadByUidsAsync(context As DavContext, uids As IEnumerable(Of String), propsToLoad As PropsToLoad) As Task(Of IEnumerable(Of ICalendarFileAsync))
            Dim selectIn As String = String.Join(", ", uids.Select(Function(a) String.Format("'{0}'", a)).ToArray())
            Dim sql As String = "SELECT * FROM [cal_CalendarFile] 
                           WHERE [UID] IN ({1})
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)"
            If propsToLoad = PropsToLoad.All Then
                ' Here we do not select attachments content because it could be very large,
                ' we only set [ContentExists] flag marking that it should be loaded during IContent.ReadAsync call.
                sql += ";SELECT * FROM [cal_EventComponent]      WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_RecurrenceException] WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_Alarm]               WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_Attendee]            WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_CustomProperty]      WHERE [UID] IN ({1})
                         ;SELECT [AttachmentId], [EventComponentId], [UID], [MediaType], [ExternalUrl], 
                             (CASE WHEN [Content] IS NULL THEN 0 ELSE 1 END) AS [ContentExists] 
                                   FROM [cal_Attachment]          WHERE [UID] IN ({1})"
            End If

            sql = String.Format(sql, GetScPropsToLoad(propsToLoad), selectIn)
            Return Await LoadAsync(context, sql, "@UserId", context.UserId)
        End Function

        Private Shared Async Function LoadAsync(context As DavContext, sql As String, ParamArray prms As Object()) As Task(Of IEnumerable(Of ICalendarFileAsync))
            Dim items As IList(Of ICalendarFileAsync) = New List(Of ICalendarFileAsync)()
            Dim stopWatch As Stopwatch = Stopwatch.StartNew()
            Using reader As SqlDataReader = Await context.ExecuteReaderAsync(sql, prms)
                Dim calendarFiles As DataTable = New DataTable()
                calendarFiles.Load(reader)
                Dim eventComponents As DataTable = New DataTable()
                If Not reader.IsClosed Then eventComponents.Load(reader)
                Dim recurrenceExceptions As DataTable = New DataTable()
                If Not reader.IsClosed Then recurrenceExceptions.Load(reader)
                Dim alarms As DataTable = New DataTable()
                If Not reader.IsClosed Then alarms.Load(reader)
                Dim attendees As DataTable = New DataTable()
                If Not reader.IsClosed Then attendees.Load(reader)
                Dim customProperties As DataTable = New DataTable()
                If Not reader.IsClosed Then customProperties.Load(reader)
                Dim attachments As DataTable = New DataTable()
                If Not reader.IsClosed Then attachments.Load(reader)
                stopWatch.Stop()
                context.Engine.Logger.LogDebug(String.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds))
                For Each rowCalendarFile As DataRow In calendarFiles.Rows
                    Dim rowsEventComponents As DataRow() = New DataRow(-1) {}
                    Dim rowsRecurrenceExceptions As DataRow() = New DataRow(-1) {}
                    Dim rowsAlarms As DataRow() = New DataRow(-1) {}
                    Dim rowsAttendees As DataRow() = New DataRow(-1) {}
                    Dim rowsCustomProperties As DataRow() = New DataRow(-1) {}
                    Dim rowsAttachments As DataRow() = New DataRow(-1) {}
                    Dim uid As String = rowCalendarFile.Field(Of String)("UID")
                    Dim filter As String = String.Format("UID = '{0}'", uid)
                    If eventComponents.Columns("UID") IsNot Nothing Then rowsEventComponents = eventComponents.Select(filter)
                    If recurrenceExceptions.Columns("UID") IsNot Nothing Then rowsRecurrenceExceptions = recurrenceExceptions.Select(filter)
                    If alarms.Columns("UID") IsNot Nothing Then rowsAlarms = alarms.Select(filter)
                    If attendees.Columns("UID") IsNot Nothing Then rowsAttendees = attendees.Select(filter)
                    If customProperties.Columns("UID") IsNot Nothing Then rowsCustomProperties = customProperties.Select(filter)
                    If attachments.Columns("UID") IsNot Nothing Then rowsAttachments = attachments.Select(filter)
                    items.Add(New CalendarFile(context, uid, rowCalendarFile, rowsEventComponents, rowsRecurrenceExceptions, rowsAlarms, rowsAttendees, rowsCustomProperties, rowsAttachments))
                Next
            End Using

            Return items
        End Function

        Private Shared Function GetScPropsToLoad(propsToLoad As PropsToLoad) As String
            Select Case propsToLoad
                Case PropsToLoad.None
                    Return "[UID]"
                Case PropsToLoad.Minimum
                    Return "[UID], [Summary]"
                Case PropsToLoad.All
                    Return "*"
            End Select

            Throw New Exception("Should never come here.")
        End Function

        Public Shared Function CreateCalendarFile(context As DavContext, calendarFolderId As Guid) As CalendarFile
            Dim calendarFile As CalendarFile = New CalendarFile(context, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
            calendarFile.calendarFolderId = calendarFolderId
            Return calendarFile
        End Function

        ''' <summary>
        ''' This file UID.
        ''' </summary>
        Private ReadOnly uid As String = Nothing

        ''' <summary>
        ''' Contains data from [cal_CalendarFile] table.
        ''' </summary>
        Private ReadOnly rowCalendarFile As DataRow = Nothing

        ''' <summary>
        ''' Contains event or to-do components from [cal_EventComponent] table.
        ''' </summary>
        Private ReadOnly rowsEventComponents As DataRow() = Nothing

        ''' <summary>
        ''' Contains recurrence days exceptions for this event or to-do from [cal_RecurrenceException] table.
        ''' </summary>
        Private ReadOnly rowsRecurrenceExceptions As DataRow() = Nothing

        ''' <summary>
        ''' Contains alarms for this event or to-do from [cal_Alarm] table.
        ''' </summary>
        Private ReadOnly rowsAlarms As DataRow() = Nothing

        ''' <summary>
        ''' Contains attendees for this event or to-do from [cal_Attendee] table.
        ''' </summary>
        Private ReadOnly rowsAttendees As DataRow() = Nothing

        ''' <summary>
        ''' Contains custom properties and custom parameters for this event/to-do, it's
        ''' alarms, attachments or attendees form [cal_CustomProperty] table.
        ''' </summary>
        Private ReadOnly rowsCustomProperties As DataRow() = Nothing

        ''' <summary>
        ''' Contains attachments for this event or to-do from [cal_Attachment] table. 
        ''' The [cal_Attachment].[Content] field is never populated in this property because it could be very large. 
        ''' Instead the [cal_Attachment].[Content] is read in ReadAsync implementation to reduce memory consumprion.
        ''' </summary>
        Private ReadOnly rowsAttachments As DataRow() = Nothing

        ''' <summary>
        ''' Indicates if this is a newly created event/to-do.
        ''' </summary>
        Private ReadOnly Property isNew As Boolean
            Get
                Return calendarFolderId <> Guid.Empty
            End Get
        End Property

        ''' <summary>
        ''' Used to form unique SQL parameter names.
        ''' </summary>
        Private paramIndex As Integer = 0

        ''' <summary>
        ''' Calendar ID in which the new event or to-do will be created.
        ''' </summary>
        Private calendarFolderId As Guid = Guid.Empty

        ''' <summary>
        ''' Gets display name of the event or to-do. Used for demo purposes only, to be displayed in Ajax File Browser.
        ''' </summary>
        ''' <remarks>CalDAV clients typically never request this property.</remarks>
        Public Overrides ReadOnly Property Name As String Implements IHierarchyItemAsync.Name
            Get
                Return String.Join(", ", rowsEventComponents.Select(Function(x) String.Format("[{0}]", x.Field(Of String)("Summary"))).ToArray())
            End Get
        End Property

        ''' <summary>
        ''' Gets item path.
        ''' </summary>
        ''' <remarks>[DAVLocation]/calendars/[CalendarFolderId]/[UID].ics</remarks>
        Public Overrides ReadOnly Property Path As String Implements IHierarchyItemAsync.Path
            Get
                Dim calendarFolderId As Guid = rowCalendarFile.Field(Of Guid)("CalendarFolderId")
                Dim uid As String = rowCalendarFile.Field(Of String)("UID")
                Return String.Format("{0}{1}/{2}{3}", CalendarsRootFolder.CalendarsRootFolderPath, calendarFolderId, uid, Extension)
            End Get
        End Property

        ''' <summary>
        ''' Gets eTag. Used for synchronization with client application. ETag must change every time the event/to-do is updated.
        ''' </summary>
        Public ReadOnly Property Etag As String Implements IContentAsync.Etag
            Get
                Dim bETag As Byte() = rowCalendarFile.Field(Of Byte())("ETag")
                Return BitConverter.ToUInt64(bETag.Reverse().ToArray(), 0).ToString()
            End Get
        End Property

        ''' <summary>
        ''' Gets item creation date. Must be in UTC.
        ''' </summary>
        Public Overrides ReadOnly Property Created As DateTime Implements IHierarchyItemAsync.Created
            Get
                Return rowCalendarFile.Field(Of DateTime)("CreatedUtc")
            End Get
        End Property

        ''' <summary>
        ''' Gets item modification date. Must be in UTC.
        ''' </summary>
        Public Overrides ReadOnly Property Modified As DateTime Implements IHierarchyItemAsync.Modified
            Get
                Return rowCalendarFile.Field(Of DateTime)("ModifiedUtc")
            End Get
        End Property

        ''' <summary>
        ''' File content length. Typicaly never requested by CalDAV clients.
        ''' </summary>
        ''' <remarks>
        ''' If -1 is returned the chunked response will be generated if possible. The getcontentlength property will not be generated.
        ''' </remarks>
        Public ReadOnly Property ContentLength As Long Implements IContentAsync.ContentLength
            Get
                Return -1
            End Get
        End Property

        ''' <summary>
        ''' File Mime-type/Content-Type.
        ''' </summary>
        Public ReadOnly Property ContentType As String Implements IContentAsync.ContentType
            Get
                Return "text/calendar"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CalendarFile"/>  class from database source. 
        ''' Used when listing folder content and during multi-get requests.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="uid">Calendar file UID.</param>
        ''' <param name="rowCalendarFile">Calendar file info from [cal_CalendarFile] table.</param>
        ''' <param name="rowsEventComponents">List of event components for this event or to-do data from [cal_EventComponent] table.</param>
        ''' <param name="rowsRecurrenceExceptions">List of recurrence days exceptions for this event or to-do from [RecurrenceExceptions] table.</param>
        ''' <param name="rowsAlarms">List of alarms for this event or to-do from [cal_Alarm] table.</param>
        ''' <param name="rowsAttendees">List of attendees for this event or to-do from [cal_Attendee] table.</param>
        ''' <param name="rowsCustomProperties">List of iCalendar custom properties and parameters for this event or to-do from [cal_CustomProperty] table.</param>
        ''' <param name="rowsAttachments">
        ''' List of attachments for this event or to-do from [cal_Attachment] table. 
        ''' The [cal_Attachment].[Content] field shoud be never populated in this property - it could be very large. 
        ''' Instead the [cal_Attachment].[Content] is read in ReadAsync implementation to reduce memory consumprion.
        ''' </param>
        Private Sub New(context As DavContext, uid As String,
                       rowCalendarFile As DataRow, rowsEventComponents As DataRow(), rowsRecurrenceExceptions As DataRow(), rowsAlarms As DataRow(),
                       rowsAttendees As DataRow(), rowsCustomProperties As DataRow(), rowsAttachments As DataRow())
            MyBase.New(context)
            Me.uid = uid
            Me.rowCalendarFile = rowCalendarFile
            Me.rowsEventComponents = rowsEventComponents
            Me.rowsRecurrenceExceptions = rowsRecurrenceExceptions
            Me.rowsAlarms = rowsAlarms
            Me.rowsAttendees = rowsAttendees
            Me.rowsCustomProperties = rowsCustomProperties
            Me.rowsAttachments = rowsAttachments
        End Sub

        Public Async Function WriteAsync(stream As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
            'Set timeout to maximum value to be able to upload iCalendar files with large file attachments.
            System.Web.HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
            Dim iCalendar As String
            Using reader As StreamReader = New StreamReader(stream)
                iCalendar = reader.ReadToEnd()
            End Using

            Dim calendars As IEnumerable(Of IComponent) = New vFormatter().Deserialize(iCalendar)
            Dim calendar As ICalendar2 = TryCast(calendars.First(), ICalendar2)
            Dim components As IEnumerable(Of IEventBase) = calendar.Events.Cast(Of IEventBase)()
            If Not components.Any() Then
                components = calendar.ToDos.Cast(Of IEventBase)()
            End If

            If components Is Nothing Then Throw New DavException("Event or to-do was expected in the input stream, no events or to-dos were found.", DavStatus.UNSUPPORTED_MEDIA_TYPE)
            Dim uid As String = components.First().Uid.Text
            Await WriteCalendarFileAsync(Context, uid, calendarFolderId, isNew)
            For Each component As IEventBase In components
                Dim eventComponentId As Guid = Guid.NewGuid()
                Await WriteEventComponentAsync(Context, component, eventComponentId, uid)
                Await WriteRecurrenceExceptionsAsync(Context, component.ExceptionDateTimes, eventComponentId, uid)
                Await WriteAlarmsAsync(Context, component.Alarms, eventComponentId, uid)
                Await WriteAttendeesAsync(Context, component.Attendees, eventComponentId, uid)
                Await WriteAttachmentsAsync(Context, component.Attachments, eventComponentId, uid)
            Next

            ' Notify attendees that event is created or modified.
            calendar.Method = calendar.CreateMethodProp(MethodType.Request)
            Await iMipEventSchedulingTransport.NotifyAttendeesAsync(Context, calendar)
            Return True
        End Function

        Private Shared Async Function WriteCalendarFileAsync(context As DavContext, uid As String, calendarFolderId As Guid, isNew As Boolean) As Task
            Dim sql As String
            If isNew Then
                sql = "IF EXISTS (SELECT 1 FROM [cal_Access] WHERE [CalendarFolderId]=@CalendarFolderId AND [UserId]=@UserId AND [Write]=1)
                    INSERT INTO [cal_CalendarFile] (
                          [UID]
                        , [CalendarFolderId]
                    ) VALUES (
                          @UID
                        , @CalendarFolderId
                    )"
            Else
                ' We can only update record in [cal_CalendarFile] table.
                ' There is no way to update [cal_EventComponent], [cal_RecurrenceException], [cal_Alarm], [cal_Attendee], 
                ' [cal_Attachment] and [cal_CustomProperty] for existing event, we must delete all records for this UID and recreate.
                ' [ModifiedUtc] field update triggers [ETag] field update which is used for synchronyzation.
                sql = "IF EXISTS (SELECT 1 FROM [cal_CalendarFile]
                        WHERE UID = @UID
                        AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId] = @UserId AND [Write] = 1))
                    BEGIN
                        UPDATE [cal_CalendarFile] SET 
                            [ModifiedUtc] = @ModifiedUtc
                        WHERE [UID] = @UID

                        ; DELETE FROM [cal_EventComponent]      WHERE [UID] = @UID
                        ; DELETE FROM [cal_RecurrenceException] WHERE [UID] = @UID
                        ; DELETE FROM [cal_Alarm]               WHERE [UID] = @UID
                        ; DELETE FROM [cal_Attendee]            WHERE [UID] = @UID
                        ; DELETE FROM [cal_Attachment]          WHERE [UID] = @UID
                        ; DELETE FROM [cal_CustomProperty]      WHERE [UID] = @UID
                    END"
            End If

            If Await context.ExecuteNonQueryAsync(sql,
                                                 "@UID", uid,
                                                 "UserId", context.UserId,
                                                 "@CalendarFolderId", calendarFolderId,
                                                 "@ModifiedUtc", DateTime.UtcNow) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If
        End Function

        Private Async Function WriteEventComponentAsync(context As DavContext, sc As IEventBase, eventComponentId As Guid, uid As String) As Task
            Dim sql As String = "INSERT INTO [cal_EventComponent] (
                          [EventComponentId]
                        , [UID]
                        , [ComponentType]
                        , [DateTimeStampUtc]
                        , [CreatedUtc]
                        , [LastModifiedUtc]
                        , [Summary]
                        , [Description]
                        , [OrganizerEmail]
                        , [OrganizerCommonName]
                        , [Start]
                        , [StartTimeZoneId]
                        , [End]
                        , [EndTimeZoneId]
                        , [Duration]
                        , [AllDay]
                        , [Class]
                        , [Location]
                        , [Priority]
                        , [Sequence]
                        , [Status]
                        , [Categories]
                        , [RecurFrequency]
                        , [RecurInterval]
                        , [RecurUntil]
                        , [RecurCount]
                        , [RecurWeekStart]
                        , [RecurByDay]
                        , [RecurByMonthDay]
                        , [RecurByMonth]
                        , [RecurBySetPos]
                        , [RecurrenceIdDate]
                        , [RecurrenceIdTimeZoneId]
                        , [RecurrenceIdThisAndFuture]
                        , [EventTransparency]
                        , [ToDoCompletedUtc]
                        , [ToDoPercentComplete]
                    ) VALUES (
                          @EventComponentId
                        , @UID
                        , @ComponentType
                        , @DateTimeStampUtc 
                        , @CreatedUtc
                        , @LastModifiedUtc
                        , @Summary
                        , @Description
                        , @OrganizerEmail
                        , @OrganizerCommonName
                        , @Start, @StartTimeZoneId
                        , @End, @EndTimeZoneId
                        , @Duration
                        , @AllDay
                        , @Class
                        , @Location
                        , @Priority
                        , @Sequence
                        , @Status
                        , @Categories
                        , @RecurFrequency
                        , @RecurInterval
                        , @RecurUntil
                        , @RecurCount
                        , @RecurWeekStart
                        , @RecurByDay
                        , @RecurByMonthDay
                        , @RecurByMonth
                        , @RecurBySetPos
                        , @RecurrenceIdDate
                        , @RecurrenceIdTimeZoneId
                        , @RecurrenceIdThisAndFuture
                        , @EventTransparency
                        , @ToDoCompletedUtc
                        , @ToDoPercentComplete
                    )"
            Dim isEvent As Boolean = TypeOf sc Is IEvent
            Dim endProp As ICalDate = If(isEvent, TryCast(sc, IEvent).End, TryCast(sc, IToDo).Due)
            Await context.ExecuteNonQueryAsync(sql,
                                              "@EventComponentId", eventComponentId,
                                              "@UID", uid,                                                                   ' UID value
                                              "@ComponentType", isEvent,
                                              "@DateTimeStampUtc", sc.DateTimeStampUtc?.Value?.DateVal,                                   ' DTSTAMP value
                                              "@CreatedUtc", sc.CreatedUtc?.Value?.DateVal,                                         ' CREATED value
                                              "@LastModifiedUtc", sc.LastModifiedUtc?.Value?.DateVal,                                    ' LAST-MODIFIED value
                                              "@Summary", sc.Summary?.Text,                                                      ' SUMMARY value
                                              "@Description", sc.Description?.Text,                                                  ' DESCRIPTION value
                                              "@OrganizerEmail", sc.Organizer?.Uri?.Replace("mailto:", ""), "@OrganizerCommonName", sc.Organizer?.CommonName,                                              ' ORGANIZER CN param
                                              "@Start", sc.Start?.Value?.DateVal,                                              ' DTSTART value
                                              "@StartTimeZoneId", If(sc.Start?.Value?.DateVal.Kind = DateTimeKind.Utc, TimeZoneInfo.Utc.Id, sc.Start?.TimeZoneId),  ' DTSTART TZID param
                                              "@End", endProp?.Value?.DateVal,                                               ' DTEND or DUE value
                                              "@EndTimeZoneId", If(endProp?.Value?.DateVal.Kind = DateTimeKind.Utc, TimeZoneInfo.Utc.Id, endProp?.TimeZoneId),    ' DTEND or DUE TZID param
                                              "@Duration", CType(sc.Duration?.Value, TimeSpan?)?.Ticks,                                             ' DURATION value
                                              "@AllDay", Not sc.Start?.Value?.Components.HasFlag(DateComponents.Time), "@Class", sc.Class?.Value.Name,                                                  ' CLASS value
                                              "@Location", sc.Location?.Text,                                                     ' LOCATION value
                                              "@Priority", sc.Priority?.Value,                                                    ' PRIORITY value
                                              "@Sequence", sc.Sequence?.Value,                                                    ' SEQUENCE value
                                              "@Status", sc.Status?.Value.Name,                                                 ' STATUS value
                                              "@Categories", ListToString(Of String)(sc.Categories.Select(Function(x) ListToString(Of String)(x.Categories, ",")), ";"), "@RecurFrequency", sc.RecurrenceRule?.Frequency?.ToString(), "@RecurInterval", CType(sc.RecurrenceRule?.Interval, Integer?),                                     ' RRULE INTERVAL value part
                                              "@RecurUntil", sc.RecurrenceRule?.Until?.DateVal,                                     ' RRULE UNTIL value part
                                              "@RecurCount", CType(sc.RecurrenceRule?.Count, Integer?),                                        ' RRULE COUNT value part
                                              "@RecurWeekStart", CType(sc.RecurrenceRule?.WeekStart, DayOfWeek?)?.ToString(), "@RecurByDay", ListToString(Of DayRule)(sc.RecurrenceRule?.ByDay), "@RecurByMonthDay", ListToString(Of Short)(sc.RecurrenceRule?.ByMonthDay), "@RecurByMonth", ListToString(Of UShort)(sc.RecurrenceRule?.ByMonth), "@RecurBySetPos", ListToString(Of Short)(sc.RecurrenceRule?.BySetPos), "@RecurrenceIdDate", sc.RecurrenceId?.Value.DateVal,                                        ' RECURRENCE-ID value
                                              "@RecurrenceIdTimeZoneId", sc.RecurrenceId?.TimeZoneId,                                           ' RECURRENCE-ID TZID param
                                              "@RecurrenceIdThisAndFuture", sc.RecurrenceId?.IsThisAndFuture,                                    ' RECURRENCE-ID RANGE param
                                              "@EventTransparency", TryCast(sc, IEvent)?.Transparency?.IsTransparent,                           ' VEVENT TRANSP value
                                              "@ToDoCompletedUtc", TryCast(sc, IToDo)?.CompletedUtc?.Value?.DateVal,                           ' VTODO COMPLETED value
                                              "@ToDoPercentComplete", TryCast(sc, IToDo)?.PercentComplete?.Value                                 ' VTODO PERCENT-COMPLETE value
                                              )
            Dim customPropsSqlInsert As String
            Dim customPropsParamsInsert As List(Of Object)
            If PrepareSqlCustomPropertiesOfComponentAsync(sc, eventComponentId, uid, customPropsSqlInsert, customPropsParamsInsert) Then
                Await context.ExecuteNonQueryAsync(customPropsSqlInsert, customPropsParamsInsert.ToArray())
            End If
        End Function

        Private Shared Function ListToString(Of T)(arr As IEnumerable(Of T), Optional separator As String = ",") As String
            If(arr Is Nothing) OrElse Not arr.Any() Then Return Nothing
            Return String.Join(Of T)(separator, arr)
        End Function

        Private Shared Async Function WriteRecurrenceExceptionsAsync(context As DavContext, recurrenceExceptions As IPropertyList(Of ICalDateList), eventComponentId As Guid, uid As String) As Task
            Dim sql As String = "INSERT INTO [cal_RecurrenceException] (
                      [EventComponentId]
                    , [UID]
                    , [ExceptionDate]
                    , [TimeZoneId]
                    , [AllDay]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@EventComponentId", eventComponentId,
                                                                                 "@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each dateListProp As ICalDateList In recurrenceExceptions
                For Each [date] As [Date] In dateListProp.Dates
                    If [date] Is Nothing Then Continue For
                    valuesSql.Add(String.Format("(
                      @EventComponentId
                    , @UID
                    , @ExceptionDate{0}
                    , @TimeZoneId{0}
                    , @AllDay{0}
                    )", i))
                    parameters.AddRange(New Object() {"@ExceptionDate" & i, [date].DateVal,                                                                         ' EXDATE value
                                                     "@TimeZoneId" & i, If([date].DateVal.Kind = DateTimeKind.Utc, TimeZoneInfo.Utc.Id, dateListProp.TimeZoneId),' EXDATE TZID param
                                                     "@AllDay" & i, Not [date].Components.HasFlag(DateComponents.Time)})
                Next

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        Private Async Function WriteAlarmsAsync(context As DavContext, alarms As IComponentList(Of IAlarm), eventComponentId As Guid, uid As String) As Task
            Dim sql As String = "INSERT INTO [cal_Alarm] (
                      [AlarmId]
                    , [EventComponentId]
                    , [UID]
                    , [Action]
                    , [TriggerAbsoluteDateTimeUtc]
                    , [TriggerRelativeOffset]
                    , [TriggerRelatedStart]
                    , [Summary]
                    , [Description]
                    , [Duration]
                    , [Repeat]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@EventComponentId", eventComponentId,
                                                                                 "@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each alarm As IAlarm In alarms
                Dim alarmId As Guid = Guid.NewGuid()
                valuesSql.Add(String.Format("(
                      @AlarmId{0}
                    , @EventComponentId
                    , @UID
                    , @Action{0}
                    , @TriggerAbsoluteDateTimeUtc{0}
                    , @TriggerRelativeOffset{0}
                    , @TriggerRelatedStart{0}
                    , @Summary{0}
                    , @Description{0}
                    , @Duration{0}
                    , @Repeat{0}
                    )", i))
                parameters.AddRange(New Object() {"@AlarmId" & i, alarmId,
                                                 "@Action" & i, alarm.Action.Action.Name,                                                        ' Alarm ACTION property
                                                 "@TriggerAbsoluteDateTimeUtc" & i, alarm.Trigger?.AbsoluteDateTimeUtc,                                              ' Alarm TRIGGER property
                                                 "@TriggerRelativeOffset" & i, CType(alarm.Trigger?.RelativeOffset, TimeSpan?)?.Ticks,                                            ' Alarm TRIGGER property
                                                 "@TriggerRelatedStart" & i, If(alarm.Trigger Is Nothing, CType(Nothing, Boolean?), alarm.Trigger.Related = RelatedType.Start),  ' Alarm trigger RELATED param
                                                 "@Summary" & i, alarm.Summary?.Text,                                                             ' Alarm SUMMARY property
                                                 "@Description" & i, alarm.Description?.Text,                                                         ' Alarm DESCRIPTION property
                                                 "@Duration" & i, CType(alarm.Duration?.Value, TimeSpan?)?.Ticks,                                                    ' Alarm DURATION property
                                                 "@Repeat" & i, alarm.Repeat?.Value                                                             ' Alarm REPEAT property
                                                 })
                Dim customPropsSqlInsert As String
                Dim customPropsParamsInsert As List(Of Object)
                If PrepareSqlCustomPropertiesOfComponentAsync(alarm, alarmId, uid, customPropsSqlInsert, customPropsParamsInsert) Then
                    sql += "; " & customPropsSqlInsert
                    parameters.AddRange(customPropsParamsInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        Private Async Function WriteAttendeesAsync(context As DavContext, attendees As IPropertyList(Of IAttendee), eventComponentId As Guid, uid As String) As Task
            Dim sql As String = "INSERT INTO [cal_Attendee] (
                      [AttendeeId]
                    , [EventComponentId]
                    , [UID]
                    , [Email]
                    , [CommonName]
                    , [DirectoryEntryRef]
                    , [Language]
                    , [UserType]
                    , [SentBy]
                    , [DelegatedFrom]
                    , [DelegatedTo]
                    , [Rsvp]
                    , [ParticipationRole]
                    , [ParticipationStatus]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@EventComponentId", eventComponentId,
                                                                                 "@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each attendee As IAttendee In attendees
                valuesSql.Add(String.Format("(
                      @AttendeeId{0}
                    , @EventComponentId
                    , @UID
                    , @Email{0}
                    , @CommonName{0}
                    , @DirectoryEntryRef{0}
                    , @Language{0}
                    , @UserType{0}
                    , @SentBy{0}
                    , @DelegatedFrom{0}
                    , @DelegatedTo{0}
                    , @Rsvp{0}
                    , @ParticipationRole{0}
                    , @ParticipationStatus{0}
                )", i))
                Dim attendeeId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@AttendeeId" & i, attendeeId,
                                                 "@Email" & i, attendee.Uri?.Replace("mailto:", ""), "@CommonName" & i, attendee.CommonName,                     ' Attendee CN parameter
                                                 "@DirectoryEntryRef" & i, attendee.Dir,                            ' Attendee DIR parameter
                                                 "@Language" & i, attendee.Language,                       ' Attendee LANGUAGE parameter
                                                 "@UserType" & i, attendee.UserType?.Name,                 ' Attendee CUTYPE parameter
                                                 "@SentBy" & i, attendee.SentBy,                         ' Attendee SENT-BY parameter
                                                 "@DelegatedFrom" & i, attendee.DelegatedFrom.FirstOrDefault(), "@DelegatedTo" & i, attendee.DelegatedTo.FirstOrDefault(), "@Rsvp" & i, attendee.Rsvp = RsvpType.True,          ' Attendee RSVP parameter
                                                 "@ParticipationRole" & i, attendee.ParticipationRole?.Name,        ' Attendee ROLE parameter
                                                 "@ParticipationStatus" & i, attendee.ParticipationStatus?.Name      ' Attendee PARTSTAT parameter
                                                 })
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("ATTENDEE", attendee.RawProperty, attendeeId, uid, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        Private Async Function WriteAttachmentsAsync(context As DavContext, attachments As IPropertyList(Of IMedia), eventComponentId As Guid, uid As String) As Task
            Dim sqlAttachment As String = "INSERT INTO [cal_Attachment] (
                      [AttachmentId]
                    , [EventComponentId]
                    , [UID]
                    , [MediaType]
                    , [ExternalUrl]
                    , [Content]
                ) VALUES (
                      @AttachmentId
                    , @EventComponentId
                    , @UID
                    , @MediaType
                    , @ExternalUrl
                    , @Content
                )"
            Dim customPropertiesSql As String = ""
            Dim customPropertiesParameters As List(Of Object) = New List(Of Object)()
            For Each attachment As IMedia In attachments
                Dim contentParam As SqlParameter = New SqlParameter("@Content", SqlDbType.VarBinary, -1)
                contentParam.Value = DBNull.Value
                If Not attachment.IsExternal Then
                    Dim content As Byte() = Convert.FromBase64String(attachment.Base64Data)
                    contentParam.Size = content.Length
                    contentParam.Value = content
                End If

                Dim attachmentId As Guid = Guid.NewGuid()
                Await context.ExecuteNonQueryAsync(sqlAttachment,
                                                  "@AttachmentId", attachmentId,
                                                  "@EventComponentId", eventComponentId,
                                                  "@UID", uid,
                                                  "@MediaType", attachment.MediaType,
                                                  "@ExternalUrl", If(attachment.IsExternal, attachment.Uri, Nothing), contentParam
                                                  )
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("ATTACH", attachment.RawProperty, attachmentId, uid, customPropSqlInsert, customPropParametersInsert) Then
                    customPropertiesSql += "; " & customPropSqlInsert
                    customPropertiesParameters.AddRange(customPropParametersInsert)
                End If
            Next

            If Not String.IsNullOrEmpty(customPropertiesSql) Then
                Await context.ExecuteNonQueryAsync(customPropertiesSql, customPropertiesParameters.ToArray())
            End If
        End Function

        Private Function PrepareSqlParamsWriteCustomProperty(propName As String, prop As IRawProperty, parentId As Guid, uid As String, ByRef sql As String, ByRef parameters As List(Of Object)) As Boolean
            sql = "INSERT INTO [cal_CustomProperty] (
                      [ParentId]
                    , [UID]
                    , [PropertyName]
                    , [ParameterName]
                    , [Value]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            parameters = New List(Of Object)()
            Dim origParamsCount As Integer = parameters.Count()
            Dim isCustomProp As Boolean = propName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase)
            Dim paramName As String = Nothing
            If isCustomProp Then
                Dim val As String = prop.RawValue
                valuesSql.Add(String.Format("(
                                  @ParentId{0}
                                , @UID{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                )", paramIndex))
                parameters.AddRange(New Object() {"@ParentId" & paramIndex, parentId,
                                                 "@UID" & paramIndex, uid,       ' added for performance optimization purposes
                                                 "@PropertyName" & paramIndex, propName,
                                                 "@ParameterName" & paramIndex, paramName, ' null is inserted into the ParameterName field to mark prop value
                                                 "@Value" & paramIndex, val
                                                 })
                paramIndex += 1
            End If

            For Each param As Parameter In prop.Parameters
                paramName = param.Name
                If Not isCustomProp AndAlso Not paramName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase) Then Continue For
                For Each value As String In param.Values
                    Dim val As String = value
                    valuesSql.Add(String.Format("(
                                  @ParentId{0}
                                , @UID{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                )", paramIndex))
                    parameters.AddRange(New Object() {"@ParentId" & paramIndex, parentId,
                                                     "@UID" & paramIndex, uid,       ' added for performance optimization purposes
                                                     "@PropertyName" & paramIndex, propName,
                                                     "@ParameterName" & paramIndex, paramName,
                                                     "@Value" & paramIndex, val
                                                     })
                    paramIndex += 1
                Next
            Next

            If origParamsCount < parameters.Count() Then
                sql = String.Format(sql, String.Join(", ", valuesSql.ToArray()))
                Return True
            End If

            Return False
        End Function

        Private Function PrepareSqlCustomPropertiesOfComponentAsync(component As IComponent, parentId As Guid, uid As String, ByRef sql As String, ByRef parameters As List(Of Object)) As Boolean
            sql = ""
            parameters = New List(Of Object)()
            Dim multiProps As String() = New String() {"ATTACH", "ATTENDEE", "EXDATE"}
            For Each pair As KeyValuePair(Of String, IList(Of IRawProperty)) In component.Properties
                If multiProps.Contains(pair.Key.ToUpper()) OrElse (pair.Value.Count <> 1) Then Continue For
                Dim sqlInsert As String
                Dim parametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty(pair.Key, pair.Value.First(), parentId, uid, sqlInsert, parametersInsert) Then
                    sql += "; " & sqlInsert
                    parameters.AddRange(parametersInsert)
                End If
            Next

            Return Not String.IsNullOrEmpty(sql)
        End Function

        Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            Dim cal As ICalendar2 = Await GetCalendarAsync()
            Dim sql As String = "DELETE FROM [cal_CalendarFile] 
                           WHERE UID=@UID
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId AND [Write]=1)"
            If Await Context.ExecuteNonQueryAsync(sql, 
                                                 "@UID", uid,
                                                 "@UserId", Context.UserId) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If

            ' Notify attendees that event is canceled if deletion is successful.
            cal.Method = cal.CreateMethodProp(MethodType.Cancel)
            Await iMipEventSchedulingTransport.NotifyAttendeesAsync(Context, cal)
        End Function

        Public Async Function ReadAsync(output As Stream, startIndex As Long, count As Long) As Task Implements IContentAsync.ReadAsync
            Dim cal As ICalendar2 = Await GetCalendarAsync()
            Call New vFormatter().Serialize(output, cal)
        End Function

        Private Async Function GetCalendarAsync() As Task(Of ICalendar2)
            Dim cal As ICalendar2 = CalendarFactory.CreateCalendar2()
            cal.ProductId = cal.CreateTextProp("-//IT Hit//Collab Lib//EN")
            For Each rowEventComponent As DataRow In rowsEventComponents
                Dim isEvent As Boolean = rowEventComponent.Field(Of Boolean)("ComponentType")
                Dim sc As IEventBase
                If isEvent Then
                    sc = cal.Events.CreateComponent()
                    cal.Events.Add(TryCast(sc, IEvent))
                Else
                    sc = cal.ToDos.CreateComponent()
                    cal.ToDos.Add(TryCast(sc, IToDo))
                End If

                ' Read component properties from previously loaded [cal_EventComponent] rows.
                ReadEventComponent(sc, rowEventComponent, cal)
                Dim eventComponentId As Guid = rowEventComponent.Field(Of Guid)("EventComponentId")
                Dim rowsThisScRecurrenceExceptions As IEnumerable(Of DataRow) = rowsRecurrenceExceptions.Where(Function(x) x.Field(Of Guid)("EventComponentId") = eventComponentId)
                ReadRecurrenceExceptions(sc.ExceptionDateTimes, rowsThisScRecurrenceExceptions, cal)
                Dim rowsThisScAlarms As IEnumerable(Of DataRow) = rowsAlarms.Where(Function(x) x.Field(Of Guid)("EventComponentId") = eventComponentId)
                ReadAlarms(sc.Alarms, rowsThisScAlarms, cal)
                Dim rowsThisScAttendees As IEnumerable(Of DataRow) = rowsAttendees.Where(Function(x) x.Field(Of Guid)("EventComponentId") = eventComponentId)
                ReadAttendees(sc.Attendees, rowsThisScAttendees, cal)
                Dim rowsThisScAttachments As IEnumerable(Of DataRow) = rowsAttachments.Where(Function(x) x.Field(Of Guid)("EventComponentId") = eventComponentId)
                Await ReadAttachmentsAsync(Context, sc.Attachments, rowsThisScAttachments, cal)
            Next

            ' Generate VTIMEZONE components based on TZID parameters.
            cal.AutoGenerateTimeZones = True
            Return cal
        End Function

        ''' <summary>
        ''' Reads data from [cal_EventComponent] row.
        ''' </summary>
        ''' <param name="sc">Event or to-do that will be populated from row paramater.</param>
        ''' <param name="row">Data from [cal_EventComponent] table to populate sc.</param>
        ''' <param name="cal">Calendar object.</param>
        Private Sub ReadEventComponent(sc As IEventBase, row As DataRow, cal As ICalendar2)
            Dim isAllDay As Boolean = row.Field(Of Boolean?)("AllDay").GetValueOrDefault()
            sc.Uid = cal.CreateTextProp(row.Field(Of String)("UID"))
            sc.DateTimeStampUtc = cal.CreateDateProp(row.Field(Of DateTime?)("DateTimeStampUtc"), DateTimeKind.Utc)
            sc.CreatedUtc = cal.CreateDateProp(row.Field(Of DateTime?)("CreatedUtc"), DateTimeKind.Utc)
            sc.LastModifiedUtc = cal.CreateDateProp(row.Field(Of DateTime?)("LastModifiedUtc"), DateTimeKind.Utc)
            sc.Summary = cal.CreateCalTextProp(row.Field(Of String)("Summary"))
            sc.Description = cal.CreateCalTextProp(row.Field(Of String)("Description"))
            sc.Start = cal.CreateCalDateProp(row.Field(Of DateTime?)("Start"), row.Field(Of String)("StartTimeZoneId"), isAllDay)
            sc.Duration = cal.CreateDurationProp(row.Field(Of Long?)("Duration"))
            sc.Class = cal.CreateClassProp(row.Field(Of String)("Class"))
            sc.Location = cal.CreateCalTextProp(row.Field(Of String)("Location"))
            sc.Priority = cal.CreateIntegerProp(row.Field(Of Byte?)("Priority"))
            sc.Sequence = cal.CreateIntegerProp(row.Field(Of Integer?)("Sequence"))
            sc.Status = cal.CreateStatusProp(row.Field(Of String)("Status"))
            sc.Organizer = cal.CreateCalAddressProp(EmailToUri(row.Field(Of String)("OrganizerEmail")), row.Field(Of String)("OrganizerCommonName"))
            ' RECURRENCE-ID property
            sc.RecurrenceId = cal.CreateRecurrenceIdProp(row.Field(Of DateTime?)("RecurrenceIdDate"), row.Field(Of String)("RecurrenceIdTimeZoneId"), isAllDay,                                                                                          ' RECURRENCE-ID DATE or DATE-TIME
                                                        row.Field(Of Boolean?)("RecurrenceIdThisAndFuture").GetValueOrDefault())
            Dim categories As String = TryCast(row.Field(Of String)("Categories"), String)
            If Not String.IsNullOrEmpty(categories) Then
                Dim strCatProp As String() = categories.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                For Each categoryList As String In strCatProp
                    Dim catProp As ICategories = sc.Categories.CreateProperty()
                    catProp.Categories = categoryList.Split({","c}, StringSplitOptions.RemoveEmptyEntries)
                    sc.Categories.Add(catProp)
                Next
            End If

            Dim recurFrequency As String = row.Field(Of String)("RecurFrequency")
            If Not String.IsNullOrEmpty(recurFrequency) Then
                sc.RecurrenceRule = cal.CreateProperty(Of IRecurrenceRule)()
                sc.RecurrenceRule.Frequency = ExtendibleEnum.FromString(Of FrequencyType)(recurFrequency)
                sc.RecurrenceRule.Interval = CType(row.Field(Of Integer?)("RecurInterval"), UInteger?)
                sc.RecurrenceRule.Count = CType(row.Field(Of Integer?)("RecurCount"), UInteger?)
                Dim weekStart As String = row.Field(Of String)("RecurWeekStart")
                If Not String.IsNullOrEmpty(weekStart) Then
                    sc.RecurrenceRule.WeekStart = CType([Enum].Parse(GetType(DayOfWeek), weekStart), DayOfWeek)
                End If

                Dim until As DateTime? = row.Field(Of DateTime?)("RecurUntil")
                If until IsNot Nothing Then
                    ' UNTIL must be in UTC if DTSTART contains time zone or DTSTART is UTC.
                    ' UNTIL must be 'floating' if DTSTART is 'floating'.
                    ' UNTIL must be 'all day' if the DTSTART is 'all day'.
                    ' https:'tools.ietf.org/html/rfc5545#section-3.3.10
                    sc.RecurrenceRule.Until = New [Date](DateTime.SpecifyKind(until.Value,
                                                                             If(sc.Start.Value.DateVal.Kind <> DateTimeKind.Local, DateTimeKind.Utc, DateTimeKind.Local)),
                                                        sc.Start.Value.Components)
                End If

                Dim byDay As String = row.Field(Of String)("RecurByDay")
                If Not String.IsNullOrEmpty(byDay) Then
                    sc.RecurrenceRule.ByDay = byDay.Split(","c).Select(Function(x) DayRule.Parse(x)).ToArray()
                End If

                Dim byMonthDay As String = row.Field(Of String)("RecurByMonthDay")
                If Not String.IsNullOrEmpty(byMonthDay) Then
                    sc.RecurrenceRule.ByMonthDay = byMonthDay.Split(","c).Select(Function(x) Short.Parse(x)).ToArray()
                End If

                Dim byMonth As String = row.Field(Of String)("RecurByMonth")
                If Not String.IsNullOrEmpty(byMonth) Then
                    sc.RecurrenceRule.ByMonth = byMonth.Split(","c).Select(Function(x) UShort.Parse(x)).ToArray()
                End If

                Dim bySetPos As String = row.Field(Of String)("RecurBySetPos")
                If Not String.IsNullOrEmpty(bySetPos) Then
                    sc.RecurrenceRule.BySetPos = bySetPos.Split(","c).Select(Function(x) Short.Parse(x)).ToArray()
                End If
            End If

            If TypeOf sc Is IEvent Then
                Dim vEvent As IEvent = TryCast(sc, IEvent)
                vEvent.End = cal.CreateCalDateProp(row.Field(Of DateTime?)("End"), row.Field(Of String)("EndTimeZoneId"), isAllDay)
                vEvent.Transparency = cal.CreateTransparencyProp(row.Field(Of Boolean?)("EventTransparency"))
            Else
                Dim vToDo As IToDo = TryCast(sc, IToDo)
                vToDo.Due = cal.CreateCalDateProp(row.Field(Of DateTime?)("End"), row.Field(Of String)("EndTimeZoneId"), isAllDay)
                vToDo.CompletedUtc = cal.CreateDateProp(row.Field(Of DateTime?)("ToDoCompletedUtc"), DateTimeKind.Utc)
                vToDo.PercentComplete = cal.CreateIntegerProp(row.Field(Of Byte?)("ToDoPercentComplete"))
            End If

            Dim eventComponentId As Guid = row.Field(Of Guid)("EventComponentId")
            Dim rowsEventCustomProperties As IEnumerable(Of DataRow) = rowsCustomProperties.Where(Function(x) x.Field(Of Guid)("ParentId") = eventComponentId)
            ReadCustomProperties(sc, rowsEventCustomProperties)
        End Sub

        ''' <summary>
        ''' Reads data from [cal_RecurrenceException] rows.
        ''' </summary>
        ''' <param name="recurrenceExceptions">Empty recurrence exceptions dates list that will be populated with data from rowsRecurrenceExceptions parameter.</param>
        ''' <param name="rowsRecurrenceExceptions">Data from [cal_RecurrenceException] table to populate recurrenceExceptions parameter.</param>
        Private Shared Sub ReadRecurrenceExceptions(recurrenceExceptions As IPropertyList(Of ICalDateList), rowsRecurrenceExceptions As IEnumerable(Of DataRow), cal As ICalendar2)
            For Each rowRecurrenceException As DataRow In rowsRecurrenceExceptions
                Dim exdate As ICalDateList = cal.CreateCalDateListProp(New DateTime() {rowRecurrenceException.Field(Of DateTime)("ExceptionDate")}, rowRecurrenceException.Field(Of String)("TimeZoneId"), rowRecurrenceException.Field(Of Boolean?)("AllDay").GetValueOrDefault())
                recurrenceExceptions.Add(exdate)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [cal_Alarm] rows.
        ''' </summary>
        ''' <param name="alarms">Empty alarms list that will be populated with data from rowsAlarms parameter.</param>
        ''' <param name="rowsAlarms">Data from [cal_Alarm] table to populate alarms parameter.</param>
        ''' <param name="cal">Calendar object.</param>
        Private Sub ReadAlarms(alarms As IComponentList(Of IAlarm), rowsAlarms As IEnumerable(Of DataRow), cal As ICalendar2)
            For Each rowAlarm As DataRow In rowsAlarms
                Dim alarm As IAlarm = alarms.CreateComponent()
                alarm.Action = cal.CreateActionProp(rowAlarm.Field(Of String)("Action"))
                alarm.Summary = cal.CreateCalTextProp(rowAlarm.Field(Of String)("Summary"))
                alarm.Description = cal.CreateCalTextProp(rowAlarm.Field(Of String)("Description"))
                alarm.Duration = cal.CreateDurationProp(rowAlarm.Field(Of Long?)("Duration"))
                alarm.Repeat = cal.CreateIntegerProp(rowAlarm.Field(Of Integer?)("Repeat"))
                ' Alarm TRIGGER property
                alarm.Trigger = cal.CreateProperty(Of ITrigger)()
                Dim absolute As DateTime? = rowAlarm.Field(Of DateTime?)("TriggerAbsoluteDateTimeUtc")
                If absolute IsNot Nothing Then
                    alarm.Trigger.AbsoluteDateTimeUtc = DateTime.SpecifyKind(absolute.Value, DateTimeKind.Utc)
                End If

                Dim offset As Long? = rowAlarm.Field(Of Long?)("TriggerRelativeOffset")
                If offset IsNot Nothing Then
                    alarm.Trigger.RelativeOffset = New TimeSpan(offset.Value)
                End If

                Dim related As Boolean? = rowAlarm.Field(Of Boolean?)("TriggerRelatedStart")
                If related IsNot Nothing Then
                    alarm.Trigger.Related = If(related.Value, RelatedType.Start, RelatedType.End)
                End If

                Dim alarmId As Guid = rowAlarm.Field(Of Guid)("AlarmId")
                Dim rowsEventCustomProperties As IEnumerable(Of DataRow) = rowsCustomProperties.Where(Function(x) x.Field(Of Guid)("ParentId") = alarmId)
                ReadCustomProperties(alarm, rowsEventCustomProperties)
                alarms.Add(alarm)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [cal_Attendee] rows.
        ''' </summary>
        ''' <param name="attendees">Empty attendees list that will be populated with data from rowsAttendees parameter.</param>
        ''' <param name="rowsAttendees">Data from [cal_Attendee] table to populate attendees parameter.</param>
        ''' <param name="cal">Calendar object.</param>
        Private Sub ReadAttendees(attendees As IPropertyList(Of IAttendee), rowsAttendees As IEnumerable(Of DataRow), cal As ICalendar2)
            For Each rowAttendee As DataRow In rowsAttendees
                Dim attendee As IAttendee = attendees.CreateProperty()
                attendee.Uri = EmailToUri(rowAttendee.Field(Of String)("Email"))
                attendee.CommonName = rowAttendee.Field(Of String)("CommonName")
                attendee.Dir = rowAttendee.Field(Of String)("DirectoryEntryRef")
                attendee.Language = rowAttendee.Field(Of String)("Language")
                attendee.UserType = StringToEnum(Of CalendarUserType)(rowAttendee.Field(Of String)("UserType"))
                attendee.SentBy = EmailToUri(rowAttendee.Field(Of String)("SentBy"))
                attendee.DelegatedFrom = {EmailToUri(rowAttendee.Field(Of String)("DelegatedFrom"))}
                attendee.DelegatedTo = {EmailToUri(rowAttendee.Field(Of String)("DelegatedTo"))}
                Dim rsvp As Boolean? = rowAttendee.Field(Of Boolean?)("Rsvp")
                If rsvp IsNot Nothing Then
                    attendee.Rsvp = If(rsvp.Value, RsvpType.True, RsvpType.False)
                End If

                attendee.ParticipationRole = StringToEnum(Of ParticipationRoleType)(rowAttendee.Field(Of String)("ParticipationRole"))
                attendee.ParticipationStatus = StringToEnum(Of ParticipationStatusType)(rowAttendee.Field(Of String)("ParticipationStatus"))
                AddParamValues(rowAttendee.Field(Of Guid)("AttendeeId"), attendee.RawProperty)
                attendees.Add(attendee)
            Next
        End Sub

        Private Async Function ReadAttachmentsAsync(context As DavContext, attachments As IPropertyList(Of IMedia), rowsAttachments As IEnumerable(Of DataRow), cal As ICalendar2) As Task
            Dim loadContent As Boolean = rowsAttachments.Any(Function(x)(x.Field(Of Integer)("ContentExists") = 1))
            If loadContent Then
                ' Reading attachments content from database.
                ' Set timeout to maximum value to be able to download iCalendar files with large file attachments.
                System.Web.HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
                Dim eventComponentId As Guid = rowsAttachments.First().Field(Of Guid)("EventComponentId")
                Dim sql As String = "SELECT [AttachmentId], [MediaType], [ExternalUrl], [Content] FROM [cal_Attachment] WHERE [EventComponentId]=@EventComponentId"
                Using reader As SqlDataReader = Await context.ExecuteReaderAsync(CommandBehavior.SequentialAccess, sql, "@EventComponentId", eventComponentId)
                    While Await reader.ReadAsync()
                        Dim attachment As IMedia = attachments.CreateProperty()
                        Dim attachmentId As Guid = Await reader.GetFieldValueAsync(Of Guid)(reader.GetOrdinal("AttachmentId"))
                        Dim ordMediaType As Integer = reader.GetOrdinal("MediaType")
                        If Not Await reader.IsDBNullAsync(ordMediaType) Then
                            attachment.MediaType = Await reader.GetFieldValueAsync(Of String)(ordMediaType)
                        End If

                        Dim ordExternalUrl As Integer = reader.GetOrdinal("ExternalUrl")
                        If Not Await reader.IsDBNullAsync(ordExternalUrl) Then
                            attachment.Uri = Await reader.GetFieldValueAsync(Of String)(ordExternalUrl)
                        End If

                        Dim ordContent As Integer = reader.GetOrdinal("Content")
                        If Not Await reader.IsDBNullAsync(ordContent) Then
                            Using stream As Stream = reader.GetStream(ordContent)
                                Using memory As MemoryStream = New MemoryStream()
                                    Await stream.CopyToAsync(memory)
                                    attachment.Base64Data = Convert.ToBase64String(memory.ToArray())
                                End Using
                            End Using
                        End If

                        AddParamValues(attachmentId, attachment.RawProperty)
                        attachments.Add(attachment)
                    End While
                End Using
            Else
                For Each rowAttachment As DataRow In rowsAttachments
                    Dim attachment As IMedia = attachments.CreateProperty()
                    attachment.MediaType = rowAttachment.Field(Of String)("MediaType")
                    attachment.Uri = rowAttachment.Field(Of String)("ExternalUrl")
                    AddParamValues(rowAttachment.Field(Of Guid)("AttachmentId"), attachment.RawProperty)
                    attachments.Add(attachment)
                Next
            End If
        End Function

        ''' <summary>
        ''' Reads custom properties and parameters from [cal_CustomProperty] table
        ''' and creates them in component passed as a parameter.
        ''' </summary>
        ''' <param name="component">Component where custom properties and parameters will be created.</param>
        ''' <param name="rowsCustomProperies">Custom properties datat from [cal_CustomProperty] table.</param>
        Private Shared Sub ReadCustomProperties(component As IComponent, rowsCustomProperies As IEnumerable(Of DataRow))
            For Each rowCustomProperty As DataRow In rowsCustomProperies
                Dim propertyName As String = rowCustomProperty.Field(Of String)("PropertyName")
                Dim prop As IRawProperty
                If Not component.Properties.ContainsKey(propertyName) Then
                    prop = component.CreateRawProperty()
                    component.AddProperty(propertyName, prop)
                Else
                    prop = component.Properties(propertyName).FirstOrDefault()
                End If

                Dim paramName As String = rowCustomProperty.Field(Of String)("ParameterName")
                Dim value As String = rowCustomProperty.Field(Of String)("Value")
                If paramName Is Nothing Then
                    ' If ParameterName is null the Value contains property value
                    prop.RawValue = value
                Else
                    AddParamValue(prop, paramName, value)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Adds custom parameters to property.
        ''' </summary>
        ''' <param name="propertyId">ID from [cal_Attachment], [cal_Attendee] or [cal_Alarm] tables. Used to find parameters in [CustomProperties] table.</param>
        ''' <param name="prop">Property to add parameters to.</param>
        Private Sub AddParamValues(propertyId As Guid, prop As IRawProperty)
            Dim rowsCustomParams As IEnumerable(Of DataRow) = rowsCustomProperties.Where(Function(x) x.Field(Of Guid)("ParentId") = propertyId)
            For Each rowCustomParam As DataRow In rowsCustomParams
                Dim paramName As String = rowCustomParam.Field(Of String)("ParameterName")
                Dim paramValue As String = rowCustomParam.Field(Of String)("Value")
                AddParamValue(prop, paramName, paramValue)
            Next
        End Sub

        ''' <summary>
        ''' Adds value to property parameter.
        ''' </summary>
        ''' <param name="prop">Property.</param>
        ''' <param name="paramName">Parameter name.</param>
        ''' <param name="paramValue">Parameter value to be added.</param>
        Private Shared Sub AddParamValue(prop As IRawProperty, paramName As String, paramValue As String)
            Dim paramVals As IEnumerable(Of String) = prop.Parameters(paramName)
            Dim paramNewVals As List(Of String) = paramVals.ToList()
            paramNewVals.Add(paramValue)
            ' This call removes any parameters with identical names if any and 
            ' replaces it with a single parameter with a lost of specified values.
            prop.Parameters(paramName) = paramNewVals
        End Sub

        Private Shared Function EmailToUri(email As String) As String
            If email Is Nothing Then Return Nothing
            If email.IndexOf("@"c) > 0 Then Return String.Format("mailto:{0}", email)
            Return email
        End Function

        Private Shared Function StringToEnum(Of T As {ExtendibleEnum, New})(value As String) As T
            If value Is Nothing Then Return Nothing
            Dim res As T
            If Not ExtendibleEnum.TryFromString(Of T)(value, res) Then
                ' If no matching value is found create new ExtendibleEnum or type T 
                ' with specified string value and default numeric value (-1).
                res = New T()
                res.Name = value
            End If

            Return res
        End Function
    End Class
End Namespace
