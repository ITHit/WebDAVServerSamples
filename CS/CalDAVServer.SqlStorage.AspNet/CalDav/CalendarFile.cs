using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;


using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;

using ITHit.Collab;
using ITHit.Collab.Calendar;


namespace CalDAVServer.SqlStorage.AspNet.CalDav
{


    // +--- Calendar file [UID1].ics ---------+
    // |                                      |
    // |  +-- Time zone component ---------+  | -- Time zones are are not stored in DB, they are generated automatically 
    // |  | TZID: Zone X                   |  |    during serialization, based on TZIDs found in event or to-do.
    // |  | ...                            |  |
    // |  +--------------------------------+  |
    // |                                      |
    // |  +-- Time zone component ---------+  |
    // |  | TZID: Zone Y                   |  | -- Time zone IDs could be either IANA (Olson) IDs or System (Windows) IDs.
    // |  | ...                            |  |
    // |  +--------------------------------+  |
    // |  ...                                 |
    // |                                      |
    // |                                      |
    // |  +-- Event component -------------+  | -- Event / do-do components are stored in [cal_EventComponent] table.
    // |  | UID: [UID1]                    |  |    
    // |  | RRULE: FREQ=DAILY              |  | 
    // |  | SUMMARY: Event A               |  |
    // |  | ...                            |  |
    // |  +--------------------------------+  |
    // |                                      |
    // |  +-- Event component -------------+  | -- In case of recurring events/to-dos there could be more than one component
    // |  | UID: [UID1]                    |  |    per file. All event/to-do components within a single calendar file share
    // |  | RECURRENCE-ID: 20151028        |  |    the same UID but have different RECURRENCE-IDs. 
    // |  | SUMMARY: Instance 5 of Event A |  |    
    // |  | ...                            |  |    iOS / OS X UIDs are case sensitive (uppercase GUIDs).
    // |  +--------------------------------+  |    Bynari WebDAV Collaborator for MS Outlook UIDs are over 100 chars in length.
    // |  ...                                 |
    // |                                      |
    // |                                      |
    // +--------------------------------------+
    // 
    // 
    // 
    //    +-- Event component -------------+
    //    |                                |
    //    | UID: [UID1]                    | 
    //    | SUMMARY: Event A               |
    //    | START: 20151016T080000         |
    //    | RRULE: FREQ=DAILY              |
    //    | ...                            |
    //    |                                |
    //    | EXDATE: 20151018T080000        | -- Recurrence exception dates are stored in [cal_RecurrenceException] table.
    //    | EXDATE: 20151020T080000        |
    //    | ...                            |
    //    |                                |
    //    | ATTENDEE: mail1@server.com     | -- Attendees are stored in [cal_Attendee] table.
    //    | ATTENDEE: mail2@srvr.com       |
    //    | ...                            |
    //    |                                |
    //    | ATTACH: /9j/4VGuf+Sw...        | -- Attachments are stored in [cal_Attachment] table.
    //    | ATTACH: https://serv/file.docx |
    //    | ...                            |
    //    |                                |
    //    |  +-- Alarm Component -------+  | -- Alarms are stored in [cal_Alarm] table.
    //    |  | ACTION: DISPLAY          |  |
    //    |  | ...                      |  |
    //    |  +--------------------------+  |
    //    |                                |
    //    |  +-- Alarm Component -------+  |
    //    |  | ACTION: EMAIL            |  |
    //    |  | ...                      |  |
    //    |  +--------------------------+  |
    //    |  ...                           |
    //    |                                |
    //    +--------------------------------+

    /// <summary>
    /// Represents a calendar file. Every clendar file stores an event or to-do that consists of one or 
    /// more event / to-do components and time zones descriptions.
    /// 
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/[CalendarFolderId]/[UID].ics
    /// </summary>
    public class CalendarFile : DavHierarchyItem, ICalendarFileAsync
    {
        /// <summary>
        /// Calendar file extension.
        /// </summary>
        public static string Extension = ".ics";

        /// <summary>
        /// Loads calendar files contained in a calendar folder by calendar folder ID.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="calendarFolderId">Calendar for which events or to-dos should be loaded.</param>
        /// <param name="propsToLoad">Specifies which properties should be loaded.</param>
        /// <returns>List of <see cref="ICalendarFileAsync"/> items.</returns>
        public static async Task<IEnumerable<ICalendarFileAsync>> LoadByCalendarFolderIdAsync(DavContext context, Guid calendarFolderId, PropsToLoad propsToLoad)
        {
            // propsToLoad == PropsToLoad.Minimum -> Typical GetChildren call by iOS, Android, eM Client, etc CalDAV clients
            // [Summary] is typically not required in GetChildren call, 
            // they are extracted for demo purposes only, to be displayed in Ajax File Browser.

            // propsToLoad == PropsToLoad.All -> Bynari call, it requires all props in GetChildren call.

            if (propsToLoad != PropsToLoad.Minimum)
                throw new NotImplementedException("LoadByCalendarFolderIdAsync is implemented only with PropsToLoad.Minimum.");

            string sql = @"SELECT * FROM [cal_CalendarFile] 
                           WHERE [CalendarFolderId] = @CalendarFolderId
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                          ;SELECT [UID], [Summary] FROM [cal_EventComponent] 
                           WHERE [UID] IN (SELECT [UID] FROM [cal_CalendarFile] 
                                           WHERE [CalendarFolderId] = @CalendarFolderId)";

            sql = string.Format(sql, GetScPropsToLoad(propsToLoad));
            
            return await LoadAsync(context, sql,
                  "@UserId"             , context.UserId
                , "@CalendarFolderId"   , calendarFolderId);
        }

        /// <summary>
        /// Loads calendar files by list of UIDs.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="uids">File UIDs to load.</param>
        /// <param name="propsToLoad">Specifies which properties should be loaded.</param>
        /// <returns>List of <see cref="ICalendarFileAsync"/> items.</returns>
        public static async Task<IEnumerable<ICalendarFileAsync>> LoadByUidsAsync(DavContext context, IEnumerable<string> uids, PropsToLoad propsToLoad)
        {
            // Get IN clause part with list of file UIDs for SELECT.
            string selectIn = string.Join(", ", uids.Select(a => string.Format("'{0}'", a)).ToArray());

            string sql = @"SELECT * FROM [cal_CalendarFile] 
                           WHERE [UID] IN ({1})
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)";

            if(propsToLoad==PropsToLoad.All)
            {
                // Here we do not select attachments content because it could be very large,
                // we only set [ContentExists] flag marking that it should be loaded during IContent.ReadAsync call.
                sql += @";SELECT * FROM [cal_EventComponent]      WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_RecurrenceException] WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_Alarm]               WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_Attendee]            WHERE [UID] IN ({1})
                         ;SELECT * FROM [cal_CustomProperty]      WHERE [UID] IN ({1})
                         ;SELECT [AttachmentId], [EventComponentId], [UID], [MediaType], [ExternalUrl], 
                             (CASE WHEN [Content] IS NULL THEN 0 ELSE 1 END) AS [ContentExists] 
                                   FROM [cal_Attachment]          WHERE [UID] IN ({1})";
            }

            sql = string.Format(sql, GetScPropsToLoad(propsToLoad), selectIn);

            return await LoadAsync(context, sql, "@UserId", context.UserId);
        }

        /// <summary>
        /// Loads calendar files by SQL.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="sql">SQL that queries [cal_CalendarFile], [cal_EventComponent], etc tables.</param>
        /// <param name="prms">List of SQL parameters.</param>
        /// <returns>List of <see cref="ICalendarFileAsync"/> items.</returns>
        private static async Task<IEnumerable<ICalendarFileAsync>> LoadAsync(DavContext context, string sql, params object[] prms)
        {
            IList<ICalendarFileAsync> items = new List<ICalendarFileAsync>();

            Stopwatch stopWatch = Stopwatch.StartNew();

            using (SqlDataReader reader = await context.ExecuteReaderAsync(sql, prms))
            {
                DataTable calendarFiles = new DataTable();
                calendarFiles.Load(reader);

                DataTable eventComponents = new DataTable();
                if (!reader.IsClosed)
                    eventComponents.Load(reader);

                DataTable recurrenceExceptions = new DataTable();
                if (!reader.IsClosed)
                    recurrenceExceptions.Load(reader);

                DataTable alarms = new DataTable();
                if (!reader.IsClosed)
                    alarms.Load(reader);

                DataTable attendees = new DataTable();
                if (!reader.IsClosed)
                    attendees.Load(reader);

                DataTable customProperties = new DataTable();
                if (!reader.IsClosed)
                    customProperties.Load(reader);

                DataTable attachments = new DataTable();
                if (!reader.IsClosed)
                    attachments.Load(reader);


                stopWatch.Stop();
                context.Engine.Logger.LogDebug(string.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds));


                foreach (DataRow rowCalendarFile in calendarFiles.Rows)
                {
                    DataRow[] rowsEventComponents      = new DataRow[0];
                    DataRow[] rowsRecurrenceExceptions = new DataRow[0];
                    DataRow[] rowsAlarms               = new DataRow[0];
                    DataRow[] rowsAttendees            = new DataRow[0];
                    DataRow[] rowsCustomProperties     = new DataRow[0];
                    DataRow[] rowsAttachments          = new DataRow[0];

                    string uid = rowCalendarFile.Field<string>("UID");

                    string filter = string.Format("UID = '{0}'", uid);

                    if (eventComponents.Columns["UID"] != null)
                        rowsEventComponents = eventComponents.Select(filter);
                    if (recurrenceExceptions.Columns["UID"] != null)
                        rowsRecurrenceExceptions = recurrenceExceptions.Select(filter);
                    if (alarms.Columns["UID"] != null)
                        rowsAlarms = alarms.Select(filter);
                    if (attendees.Columns["UID"] != null)
                        rowsAttendees = attendees.Select(filter);
                    if (customProperties.Columns["UID"] != null)
                        rowsCustomProperties = customProperties.Select(filter);
                    if (attachments.Columns["UID"] != null)
                        rowsAttachments = attachments.Select(filter);

                    items.Add(new CalendarFile(context, uid, rowCalendarFile, rowsEventComponents, rowsRecurrenceExceptions, rowsAlarms, rowsAttendees, rowsCustomProperties, rowsAttachments));
                }
            }

            return items;
        }

        private static string GetScPropsToLoad(PropsToLoad propsToLoad)
        {
            switch (propsToLoad)
            {
                case PropsToLoad.None:
                    return "[UID]";
                case PropsToLoad.Minimum:
                    // [Summary] is typically not required in GetChildren call, 
                    // they are extracted for demo purposes only, to be displayed in Ajax File Browser as a file display name.
                    return "[UID], [Summary]";
                case PropsToLoad.All:
                    return "*";
            }
            throw new Exception("Should never come here.");
        }

        /// <summary>
        /// Creates new calendar file. The actual new [cal_CalendarFile], [cal_EventComponent], etc. records are inserted into the database during <see cref="WriteAsync"/> method call.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param> 
        /// <param name="calendarFolderId">Calendar folder ID to which this calendar file will belong to.</param>
        /// <returns>Instance of <see cref="CalendarFile"/>.</returns>
        public static CalendarFile CreateCalendarFile(DavContext context, Guid calendarFolderId)
        {
            CalendarFile calendarFile = new CalendarFile(context, null, null, null, null, null, null, null, null);
            calendarFile.calendarFolderId = calendarFolderId;
            return calendarFile;
        }

        /// <summary>
        /// This file UID.
        /// </summary>
        private readonly string uid = null;

        /// <summary>
        /// Contains data from [cal_CalendarFile] table.
        /// </summary>
        private readonly DataRow rowCalendarFile = null;

        /// <summary>
        /// Contains event or to-do components from [cal_EventComponent] table.
        /// </summary>
        private readonly DataRow[] rowsEventComponents = null;

        /// <summary>
        /// Contains recurrence days exceptions for this event or to-do from [cal_RecurrenceException] table.
        /// </summary>
        private readonly DataRow[] rowsRecurrenceExceptions = null;

        /// <summary>
        /// Contains alarms for this event or to-do from [cal_Alarm] table.
        /// </summary>
        private readonly DataRow[] rowsAlarms = null;

        /// <summary>
        /// Contains attendees for this event or to-do from [cal_Attendee] table.
        /// </summary>
        private readonly DataRow[] rowsAttendees = null;

        /// <summary>
        /// Contains custom properties and custom parameters for this event/to-do, it's
        /// alarms, attachments or attendees form [cal_CustomProperty] table.
        /// </summary>
        private readonly DataRow[] rowsCustomProperties = null;

        /// <summary>
        /// Contains attachments for this event or to-do from [cal_Attachment] table. 
        /// The [cal_Attachment].[Content] field is never populated in this property because it could be very large. 
        /// Instead the [cal_Attachment].[Content] is read in ReadAsync implementation to reduce memory consumprion.
        /// </summary>
        private readonly DataRow[] rowsAttachments = null;

        /// <summary>
        /// Indicates if this is a newly created event/to-do.
        /// </summary>
        private bool isNew
        {
            get { return calendarFolderId != Guid.Empty; }
        }

        /// <summary>
        /// Used to form unique SQL parameter names.
        /// </summary>
        private int paramIndex = 0;

        /// <summary>
        /// Calendar ID in which the new event or to-do will be created.
        /// </summary>
        private Guid calendarFolderId = Guid.Empty;

        /// <summary>
        /// Gets display name of the event or to-do. Used for demo purposes only, to be displayed in Ajax File Browser.
        /// </summary>
        /// <remarks>CalDAV clients typically never request this property.</remarks>
        public override string Name
        {
            get
            {
                // Show all components summaries contained in this file.
                return string.Join(", ", rowsEventComponents.Select(x => string.Format("[{0}]", x.Field<string>("Summary"))).ToArray());
            }
        }

        /// <summary>
        /// Gets item path.
        /// </summary>
        /// <remarks>[DAVLocation]/calendars/[CalendarFolderId]/[UID].ics</remarks>
        public override string Path
        {
            get
            {
                Guid calendarFolderId = rowCalendarFile.Field<Guid>("CalendarFolderId");
                string uid              = rowCalendarFile.Field<string>("UID");
                return string.Format("{0}{1}/{2}{3}", CalendarsRootFolder.CalendarsRootFolderPath, calendarFolderId, uid, Extension);
            }
        }

        /// <summary>
        /// Gets eTag. Used for synchronization with client application. ETag must change every time the event/to-do is updated.
        /// </summary>
        public string Etag
        {
            get
            {
                byte[] bETag = rowCalendarFile.Field<byte[]>("ETag");
                return BitConverter.ToUInt64(bETag.Reverse().ToArray(), 0).ToString(); // convert timestamp value to number
            }
        }

        /// <summary>
        /// Gets item creation date. Must be in UTC.
        /// </summary>
        public override DateTime Created
        {
            get { return rowCalendarFile.Field<DateTime>("CreatedUtc"); }
        }

        /// <summary>
        /// Gets item modification date. Must be in UTC.
        /// </summary>
        public override DateTime Modified
        {
            get { return rowCalendarFile.Field<DateTime>("ModifiedUtc"); }
        }

        /// <summary>
        /// File content length. Typicaly never requested by CalDAV clients.
        /// </summary>
        /// <remarks>
        /// If -1 is returned the chunked response will be generated if possible. The getcontentlength property will not be generated.
        /// </remarks>
        public long ContentLength
        {
            get { return -1; }
        }

        /// <summary>
        /// File Mime-type/Content-Type.
        /// </summary>
        public string ContentType
        {
	        get { return "text/calendar"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarFile"/> class from database source. 
        /// Used when listing folder content and during multi-get requests.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="uid">Calendar file UID.</param>
        /// <param name="rowCalendarFile">Calendar file info from [cal_CalendarFile] table.</param>
        /// <param name="rowsEventComponents">List of event components for this event or to-do data from [cal_EventComponent] table.</param>
        /// <param name="rowsRecurrenceExceptions">List of recurrence days exceptions for this event or to-do from [RecurrenceExceptions] table.</param>
        /// <param name="rowsAlarms">List of alarms for this event or to-do from [cal_Alarm] table.</param>
        /// <param name="rowsAttendees">List of attendees for this event or to-do from [cal_Attendee] table.</param>
        /// <param name="rowsCustomProperties">List of iCalendar custom properties and parameters for this event or to-do from [cal_CustomProperty] table.</param>
        /// <param name="rowsAttachments">
        /// List of attachments for this event or to-do from [cal_Attachment] table. 
        /// The [cal_Attachment].[Content] field shoud be never populated in this property - it could be very large. 
        /// Instead the [cal_Attachment].[Content] is read in ReadAsync implementation to reduce memory consumprion.
        /// </param>
        private CalendarFile(DavContext context, string uid,
            DataRow rowCalendarFile, DataRow[] rowsEventComponents, DataRow[] rowsRecurrenceExceptions, DataRow[] rowsAlarms,
            DataRow[] rowsAttendees, DataRow[] rowsCustomProperties, DataRow[] rowsAttachments)
            : base(context)
        {
            this.uid                      = uid;
            this.rowCalendarFile          = rowCalendarFile;
            this.rowsEventComponents      = rowsEventComponents;
            this.rowsRecurrenceExceptions = rowsRecurrenceExceptions;
            this.rowsAlarms               = rowsAlarms;
            this.rowsAttendees            = rowsAttendees;
            this.rowsCustomProperties     = rowsCustomProperties;
            this.rowsAttachments          = rowsAttachments;
        }

        /// <summary>
        /// Called when event or to-do is being saved to back-end storage.
        /// </summary>
        /// <param name="stream">Stream containing VCALENDAR, typically with a single VEVENT ot VTODO component.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="startIndex">Starting byte in target file
        /// for which data comes in <paramref name="content"/> stream.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>Whether the whole stream has been written.</returns>
        public async Task<bool> WriteAsync(Stream stream, string contentType, long startIndex, long totalFileSize)
        {
            //Set timeout to maximum value to be able to upload iCalendar files with large file attachments.
            System.Web.HttpContext.Current.Server.ScriptTimeout = int.MaxValue;
            string iCalendar;
            using (StreamReader reader = new StreamReader(stream))
            {
                iCalendar = reader.ReadToEnd();
            }

            // Typically the stream contains a single iCalendar that contains one or more event or to-do components.
            IEnumerable<IComponent> calendars = new vFormatter().Deserialize(iCalendar);
            ICalendar2 calendar = calendars.First() as ICalendar2;

            IEnumerable<IEventBase> components = calendar.Events.Cast<IEventBase>();
            if (!components.Any())
            {
                components = calendar.ToDos.Cast<IEventBase>();
            }

            if (components == null)
                throw new DavException("Event or to-do was expected in the input stream, no events or to-dos were found.", DavStatus.UNSUPPORTED_MEDIA_TYPE);

            // All components inside calendar file has the same UID which is equal to file name.
            string uid = components.First().Uid.Text;

            // Save data to [cal_CalendarFile] table.
            await WriteCalendarFileAsync(Context, uid, calendarFolderId, isNew);

            foreach (IEventBase component in components)
            {
                Guid eventComponentId = Guid.NewGuid();

                // Save data to [cal_EventComponent] table.
                await WriteEventComponentAsync(Context, component, eventComponentId, uid);

                // Save recurrence days exceptions for recurring events and to-dos. 
                await WriteRecurrenceExceptionsAsync(Context, component.ExceptionDateTimes, eventComponentId, uid);

                // Save alarms.
                await WriteAlarmsAsync(Context, component.Alarms, eventComponentId, uid);

                // Save attengees.
                await WriteAttendeesAsync(Context, component.Attendees, eventComponentId, uid);

                // Save attachments.
                await WriteAttachmentsAsync(Context, component.Attachments, eventComponentId, uid);
            }

            // Notify attendees that event is created or modified.
            calendar.Method = calendar.CreateMethodProp(MethodType.Request);
            await iMipEventSchedulingTransport.NotifyAttendeesAsync(Context, calendar);

            return true;
        }

        /// <summary>
        /// Saves data to [cal_CalendarFile] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="uid">File UID to be updated or created.</param>
        /// <param name="calendarFolderId">Calendar folder that contains this file.</param>
        /// <param name="isNew">Flag indicating if this is a new file or file should be updated.</param>
        /// <remarks>
        /// This function deletes records in [cal_EventComponent], [cal_RecurrenceException], [cal_Alarm],
        /// [cal_Attendee], [cal_Attachment] and [cal_CustomProperty] tables if the event or to-do should be updated.
        /// </remarks>
        private static async Task WriteCalendarFileAsync(DavContext context, string uid, Guid calendarFolderId, bool isNew)
        {
            string sql;
            if (isNew)
            {
                sql =
                  @"IF EXISTS (SELECT 1 FROM [cal_Access] WHERE [CalendarFolderId]=@CalendarFolderId AND [UserId]=@UserId AND [Write]=1)
                    INSERT INTO [cal_CalendarFile] (
                          [UID]
                        , [CalendarFolderId]
                    ) VALUES (
                          @UID
                        , @CalendarFolderId
                    )";
            }
            else
            {
                // We can only update record in [cal_CalendarFile] table.
                // There is no way to update [cal_EventComponent], [cal_RecurrenceException], [cal_Alarm], [cal_Attendee], 
                // [cal_Attachment] and [cal_CustomProperty] for existing event, we must delete all records for this UID and recreate.

                // [ModifiedUtc] field update triggers [ETag] field update which is used for synchronyzation.
                sql =
                  @"IF EXISTS (SELECT 1 FROM [cal_CalendarFile]
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
                    END";
            }

            if(await context.ExecuteNonQueryAsync(sql,
                  "@UID"                , uid
                , "UserId"              , context.UserId
                , "@CalendarFolderId"   , calendarFolderId
                , "@ModifiedUtc"        , DateTime.UtcNow) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }
        }

        /// <summary>
        /// Saves data to [cal_EventComponent] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="sc">Event or to-do component to read data from.</param>
        /// <param name="eventComponentId">New event component ID.</param>
        /// <param name="uid">File UID.</param>
        private async Task WriteEventComponentAsync(DavContext context, IEventBase sc, Guid eventComponentId, string uid)
        {
            string sql =
                    @"INSERT INTO [cal_EventComponent] (
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
                    )";


            bool isEvent = sc is IEvent;

            // Get END in case of event or DUE in case of to-do component. 
            ICalDate endProp = isEvent ? (sc as IEvent).End : (sc as IToDo).Due;
            
            await context.ExecuteNonQueryAsync(sql,
                  "@EventComponentId"       , eventComponentId
                , "@UID"                    , uid                                                                   // UID value
                , "@ComponentType"          , isEvent
                , "@DateTimeStampUtc"       , sc.DateTimeStampUtc?.Value?.DateVal                                   // DTSTAMP value
                , "@CreatedUtc"             , sc.CreatedUtc?.Value?.DateVal                                         // CREATED value
                , "@LastModifiedUtc"        , sc.LastModifiedUtc?.Value?.DateVal                                    // LAST-MODIFIED value
                , "@Summary"                , sc.Summary?.Text                                                      // SUMMARY value
                , "@Description"            , sc.Description?.Text                                                  // DESCRIPTION value
                , "@OrganizerEmail"         , sc.Organizer?.Uri?.Replace("mailto:", "")                             // ORGANIZER value
                , "@OrganizerCommonName"    , sc.Organizer?.CommonName                                              // ORGANIZER CN param
                , "@Start"                  , sc.Start?.Value?.DateVal                                              // DTSTART value
                , "@StartTimeZoneId"        , sc.Start?.Value?.DateVal.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc.Id : sc.Start?.TimeZoneId  // DTSTART TZID param
                , "@End"                    , endProp?.Value?.DateVal                                               // DTEND or DUE value
                , "@EndTimeZoneId"          , endProp?.Value?.DateVal.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc.Id : endProp?.TimeZoneId    // DTEND or DUE TZID param
                , "@Duration"               , sc.Duration?.Value?.Ticks                                             // DURATION value
                , "@AllDay"                 , !sc.Start?.Value?.Components.HasFlag(DateComponents.Time) // Check if start contains the time part to determine if this is a all-day event/to-do.
                , "@Class"                  , sc.Class?.Value.Name                                                  // CLASS value
                , "@Location"               , sc.Location?.Text                                                     // LOCATION value
                , "@Priority"               , sc.Priority?.Value                                                    // PRIORITY value
                , "@Sequence"               , sc.Sequence?.Value                                                    // SEQUENCE value
                , "@Status"                 , sc.Status?.Value.Name                                                 // STATUS value
                , "@Categories"             , ListToString<string>(sc.Categories.Select(x => ListToString<string>(x.Categories, ",")), ";") // CATEGORIES value
                , "@RecurFrequency"         , sc.RecurrenceRule?.Frequency?.ToString()                              // RRULE FREQ value part
                , "@RecurInterval"          , (int?)sc.RecurrenceRule?.Interval                                     // RRULE INTERVAL value part
                , "@RecurUntil"             , sc.RecurrenceRule?.Until?.DateVal                                     // RRULE UNTIL value part
                , "@RecurCount"             , (int?)sc.RecurrenceRule?.Count                                        // RRULE COUNT value part
                , "@RecurWeekStart"         , sc.RecurrenceRule?.WeekStart?.ToString()                              // RRULE WKST value part
                , "@RecurByDay"             , ListToString<DayRule>(sc.RecurrenceRule?.ByDay)                       // RRULE BYDAY value part
                , "@RecurByMonthDay"        , ListToString<short>(sc.RecurrenceRule?.ByMonthDay)                    // RRULE BYMONTHDAY value part
                , "@RecurByMonth"           , ListToString<ushort>(sc.RecurrenceRule?.ByMonth)                      // RRULE BYMONTH value part
                , "@RecurBySetPos"          , ListToString<short>(sc.RecurrenceRule?.BySetPos)                      // RRULE BYSETPOS value part
                , "@RecurrenceIdDate"       , sc.RecurrenceId?.Value.DateVal                                        // RECURRENCE-ID value
                , "@RecurrenceIdTimeZoneId" , sc.RecurrenceId?.TimeZoneId                                           // RECURRENCE-ID TZID param
                , "@RecurrenceIdThisAndFuture", sc.RecurrenceId?.IsThisAndFuture                                    // RECURRENCE-ID RANGE param
                , "@EventTransparency"      , (sc as IEvent)?.Transparency?.IsTransparent                           // VEVENT TRANSP value
                , "@ToDoCompletedUtc"       , (sc as IToDo)?.CompletedUtc?.Value?.DateVal                           // VTODO COMPLETED value
                , "@ToDoPercentComplete"    , (sc as IToDo)?.PercentComplete?.Value                                 // VTODO PERCENT-COMPLETE value
                );

            // Save custom properties and parameters of this component to [cal_CustomProperty] table.
            string customPropsSqlInsert;
            List<object> customPropsParamsInsert;
            if (PrepareSqlCustomPropertiesOfComponentAsync(sc, eventComponentId, uid, out customPropsSqlInsert, out customPropsParamsInsert))
            {
                await context.ExecuteNonQueryAsync(customPropsSqlInsert, customPropsParamsInsert.ToArray());
            }
        }

        /// <summary>
        /// Converts <see cref="IEnumerable{T}"/> to string. Returns null if the list is empty.
        /// </summary>
        /// <returns>String that contains elements separated by ','.</returns>
        private static string ListToString<T>(IEnumerable<T> arr, string separator = ",")
        {
            if ((arr == null) || !arr.Any())
                return null;
            return string.Join<T>(separator, arr);
        }

        /// <summary>
        /// Saves data to [cal_RecurrenceException] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="recurrenceExceptions">Event or to-do recurrence exceptions dates to be saved.</param>
        /// <param name="eventComponentId">Event component to associate these recurrence exceptions dates with.</param>
        /// <param name="uid">File UID.</param>
        private static async Task WriteRecurrenceExceptionsAsync(DavContext context, IPropertyList<ICalDateList> recurrenceExceptions, Guid eventComponentId, string uid)
        {
            // Typically CalDAV clients pass a single date value per EXDATE property.

            string sql =
                @"INSERT INTO [cal_RecurrenceException] (
                      [EventComponentId]
                    , [UID]
                    , [ExceptionDate]
                    , [TimeZoneId]
                    , [AllDay]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                  "@EventComponentId", eventComponentId
                , "@UID", uid
            });

            int i = 0;
            foreach (ICalDateList dateListProp in recurrenceExceptions)
            {
                foreach (Date date in dateListProp.Dates)
                {
                    if (date == null)
                        continue; // failed fo parse date

                    valuesSql.Add(string.Format(@"(
                      @EventComponentId
                    , @UID
                    , @ExceptionDate{0}
                    , @TimeZoneId{0}
                    , @AllDay{0}
                    )", i));

                    parameters.AddRange(new object[] {
                  //  "@EventComponentId"
                  //  "@UID" added for performance optimization purposes
                      "@ExceptionDate" +i, date.DateVal                                                                         // EXDATE value
                    , "@TimeZoneId"    +i, date.DateVal.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc.Id : dateListProp.TimeZoneId// EXDATE TZID param
                    , "@AllDay"        +i, !date.Components.HasFlag(DateComponents.Time)                                        // EXDATE DATE or DATE-TIME
                    });
                }
                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [cal_Alarm] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="alarms">List of alarms to be saved.</param>
        /// <param name="eventComponentId">Event component to associate these alarms with.</param>
        /// <param name="uid">File UID.</param>
        private async Task WriteAlarmsAsync(DavContext context, IComponentList<IAlarm> alarms, Guid eventComponentId, string uid)
        {
            string sql =
                @"INSERT INTO [cal_Alarm] (
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
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                  "@EventComponentId", eventComponentId
                , "@UID", uid
            });

            int i = 0;
            foreach (IAlarm alarm in alarms)
            {
                Guid alarmId = Guid.NewGuid();

                valuesSql.Add(string.Format(@"(
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
                    )", i));

                parameters.AddRange(new object[] {
                      "@AlarmId"                    +i, alarmId
                  //, "@EventComponentId"
                  //, "@UID" added for performance optimization purposes
                    , "@Action"                     +i, alarm.Action.Action.Name                                                        // Alarm ACTION property
                    , "@TriggerAbsoluteDateTimeUtc" +i, alarm.Trigger?.AbsoluteDateTimeUtc                                              // Alarm TRIGGER property
                    , "@TriggerRelativeOffset"      +i, alarm.Trigger?.RelativeOffset?.Ticks                                            // Alarm TRIGGER property
                    , "@TriggerRelatedStart"        +i, alarm.Trigger==null ? (bool?)null : alarm.Trigger.Related == RelatedType.Start  // Alarm trigger RELATED param
                    , "@Summary"                    +i, alarm.Summary?.Text                                                             // Alarm SUMMARY property
                    , "@Description"                +i, alarm.Description?.Text                                                         // Alarm DESCRIPTION property
                    , "@Duration"                   +i, alarm.Duration?.Value?.Ticks                                                    // Alarm DURATION property
                    , "@Repeat"                     +i, alarm.Repeat?.Value                                                             // Alarm REPEAT property
                });

                // Create SQL to save custom properties of this component of this component to [cal_CustomProperty] table.
                string customPropsSqlInsert;
                List<object> customPropsParamsInsert;
                if (PrepareSqlCustomPropertiesOfComponentAsync(alarm, alarmId, uid, out customPropsSqlInsert, out customPropsParamsInsert))
                {
                    sql += "; " + customPropsSqlInsert;
                    parameters.AddRange(customPropsParamsInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [cal_Attendee] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="attendees">List of attendees to be saved.</param>
        /// <param name="eventComponentId">Event component to associate these attendees with.</param>
        /// <param name="uid">File UID.</param>
        private async Task WriteAttendeesAsync(DavContext context, IPropertyList<IAttendee> attendees, Guid eventComponentId, string uid)
        {
            string sql =
                @"INSERT INTO [cal_Attendee] (
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
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                  "@EventComponentId", eventComponentId
                , "@UID", uid
            });

            int i = 0;
            foreach (IAttendee attendee in attendees)
            {
                valuesSql.Add(string.Format(@"(
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
                )", i));

                Guid attendeeId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@AttendeeId"         +i, attendeeId
                  //, "@EventComponentId"
                  //, "@UID" added for performance optimization purposes
                    , "@Email"              +i, attendee.Uri?.Replace("mailto:", "")    // Attendee value
                    , "@CommonName"         +i, attendee.CommonName                     // Attendee CN parameter
                    , "@DirectoryEntryRef"  +i, attendee.Dir                            // Attendee DIR parameter
                    , "@Language"           +i, attendee.Language                       // Attendee LANGUAGE parameter
                    , "@UserType"           +i, attendee.UserType?.Name                 // Attendee CUTYPE parameter
                    , "@SentBy"             +i, attendee.SentBy                         // Attendee SENT-BY parameter
                    , "@DelegatedFrom"      +i, attendee.DelegatedFrom.FirstOrDefault() // Attendee DELEGATED-FROM parameter, here we assume only 1 delegator for the sake of simplicity
                    , "@DelegatedTo"        +i, attendee.DelegatedTo.FirstOrDefault()   // Attendee DELEGATED-TO parameter, here we assume only 1 delegatee for the sake of simplicity
                    , "@Rsvp"               +i, attendee.Rsvp == RsvpType.True          // Attendee RSVP parameter
                    , "@ParticipationRole"  +i, attendee.ParticipationRole?.Name        // Attendee ROLE parameter
                    , "@ParticipationStatus"+i, attendee.ParticipationStatus?.Name      // Attendee PARTSTAT parameter
                });

                // Prepare SQL to save custom property parameters to [cal_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("ATTENDEE", attendee.RawProperty, attendeeId, uid, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [cal_Attachment] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="attachments">List of attachments to be saved.</param>
        /// <param name="eventComponentId">Event component to associate these attachments with.</param>
        /// <param name="uid">File UID.</param>
        private async Task WriteAttachmentsAsync(DavContext context, IPropertyList<IMedia> attachments, Guid eventComponentId, string uid)
        {
            // It is recommended to keep attchment size below 256Kb. In case files over 1Mb should 
            // be stored, use SQL FILESTREAM, FileTable or store content in file system.

            string sqlAttachment =
                @"INSERT INTO [cal_Attachment] (
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
                )";

            string customPropertiesSql = "";
            List<object> customPropertiesParameters = new List<object>();

            foreach (IMedia attachment in attachments)
            {
                // To insert NULL to VARBINARY column SqlParameter must be passed with Size=-1 and Value=DBNull.Value.
                SqlParameter contentParam = new SqlParameter("@Content", SqlDbType.VarBinary, -1);
                contentParam.Value = DBNull.Value;

                if (!attachment.IsExternal)
                {
                    byte[] content = Convert.FromBase64String(attachment.Base64Data);
                    contentParam.Size = content.Length;
                    contentParam.Value = content;
                }

                Guid attachmentId = Guid.NewGuid();

                await context.ExecuteNonQueryAsync(sqlAttachment,
                      "@AttachmentId"       , attachmentId
                    , "@EventComponentId"   , eventComponentId
                    , "@UID"                , uid
                    , "@MediaType"          , attachment.MediaType
                    , "@ExternalUrl"        , attachment.IsExternal ? attachment.Uri : null
                    , contentParam
                    );

                // Prepare SQL to save custom property parameters to [].
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("ATTACH", attachment.RawProperty, attachmentId, uid, out customPropSqlInsert, out customPropParametersInsert))
                {
                    customPropertiesSql += "; " + customPropSqlInsert;
                    customPropertiesParameters.AddRange(customPropParametersInsert);
                }
            }

            if (!string.IsNullOrEmpty(customPropertiesSql))
            {
                await context.ExecuteNonQueryAsync(customPropertiesSql, customPropertiesParameters.ToArray());
            }
        }

        /// <summary>
        /// Creates SQL to write custom properties and parameters to [cal_CustomProperty] table.
        /// </summary>
        /// <param name="prop">Raw property to be saved to database.</param>
        /// <param name="parentId">
        /// Parent component ID or parent property ID to which this custom property or parameter belongs to. 
        /// This could be EventComponentId, AlarmId, AttachmentId, AttendeeId.
        /// </param>
        /// <param name="uid">File UID.</param>
        /// <param name="sql">SQL to insert data to DB.</param>
        /// <param name="parameters">SQL parameter values that will be filled by this method.</param>
        /// <returns>True if any custom properies or parameters found, false otherwise.</returns>
        private bool PrepareSqlParamsWriteCustomProperty(string propName, IRawProperty prop, Guid parentId, string uid, out string sql, out List<object> parameters)
        {
            sql =
                @"INSERT INTO [cal_CustomProperty] (
                      [ParentId]
                    , [UID]
                    , [PropertyName]
                    , [ParameterName]
                    , [Value]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            parameters = new List<object>();

            int origParamsCount = parameters.Count();

            bool isCustomProp = propName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase);

            string paramName = null;

            // Save custom prop value.
            if (isCustomProp)
            {
                string val = prop.RawValue;
                valuesSql.Add(string.Format(@"(
                                  @ParentId{0}
                                , @UID{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                )", paramIndex));

                parameters.AddRange(new object[] {
                                  "@ParentId"     +paramIndex, parentId
                                , "@UID"          +paramIndex, uid       // added for performance optimization purposes
                                , "@PropertyName" +paramIndex, propName
                                , "@ParameterName"+paramIndex, paramName // null is inserted into the ParameterName field to mark prop value
                                , "@Value"        +paramIndex, val
                                });
                paramIndex++;
            }

            // Save parameters and their values.
            foreach (Parameter param in prop.Parameters)
            {
                paramName = param.Name;

                // For standard properties we save only custom params (that start with 'X-'). All standard patrams go to their fields in DB.
                // For custom properies we save all params.
                if (!isCustomProp && !paramName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                foreach (string value in param.Values)
                {
                    string val = value;

                    valuesSql.Add(string.Format(@"(
                                  @ParentId{0}
                                , @UID{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                )", paramIndex));

                    parameters.AddRange(new object[] {
                                  "@ParentId"     +paramIndex, parentId
                                , "@UID"          +paramIndex, uid       // added for performance optimization purposes
                                , "@PropertyName" +paramIndex, propName
                                , "@ParameterName"+paramIndex, paramName
                                , "@Value"        +paramIndex, val
                                });
                    paramIndex++;
                }
            }

            if (origParamsCount < parameters.Count())
            {
                sql = string.Format(sql, string.Join(", ", valuesSql.ToArray()));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates SQL to write custom properties and parameters to [cal_CustomProperty] table for specified component.
        /// </summary>
        /// <param name="component">Component to be saved to database.</param>
        /// <param name="parentId">
        /// Parent component ID to which this custom property or parameter belongs to. 
        /// This could be EventComponentId, AlarmId, etc.
        /// </param>
        /// <param name="uid">File UID.</param>
        /// <param name="sql">SQL to insert data to DB.</param>
        /// <param name="parameters">SQL parameter values that will be filled by this method.</param>
        /// <returns>True if any custom properies or parameters found, false otherwise.</returns>
        private bool PrepareSqlCustomPropertiesOfComponentAsync(IComponent component, Guid parentId, string uid, out string sql, out List<object> parameters)
        {
            sql = "";
            parameters = new List<object>();

            // We save only single custom props here, multiple props are saved in other methods.
            string[] multiProps = new string[] { "ATTACH", "ATTENDEE", "EXDATE" };

            // Properties in IComponent.Properties are grouped by name.
            foreach (KeyValuePair<string, IList<IRawProperty>> pair in component.Properties)
            {
                if (multiProps.Contains(pair.Key.ToUpper()) || (pair.Value.Count != 1))
                    continue;

                string sqlInsert;
                List<object> parametersInsert;
                if (PrepareSqlParamsWriteCustomProperty(pair.Key, pair.Value.First(), parentId, uid, out sqlInsert, out parametersInsert))
                {
                    sql += "; " + sqlInsert;
                    parameters.AddRange(parametersInsert);
                }
            }

            return !string.IsNullOrEmpty(sql);
        }

        /// <summary>
        /// Called when client application deletes this file.
        /// </summary>
        /// <param name="multistatus">Error description if case delate failed. Ignored by most clients.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            ICalendar2 cal = await GetCalendarAsync();

            string sql = @"DELETE FROM [cal_CalendarFile] 
                           WHERE UID=@UID
                           AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId AND [Write]=1)";

            if(await Context.ExecuteNonQueryAsync(sql, 
                  "@UID", uid
                , "@UserId", Context.UserId) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }

            // Notify attendees that event is canceled if deletion is successful.
            cal.Method = cal.CreateMethodProp(MethodType.Cancel);
            await iMipEventSchedulingTransport.NotifyAttendeesAsync(Context, cal);
        }

        /// <summary>
        /// Called when the event or to-do must be read from back-end storage.
        /// </summary>
        /// <param name="output">Stream to write event or to-do content.</param>
        /// <param name="startIndex">Index to start reading data from back-end storage. Used for segmented reads, not used by CalDAV clients.</param>
        /// <param name="count">Number of bytes to read. Used for segmented reads, not used by CalDAV clients.</param>
        /// <returns></returns>
        public async Task ReadAsync(Stream output, long startIndex, long count)
        {
            ICalendar2 cal = await GetCalendarAsync();
            new vFormatter().Serialize(output, cal);
        }
        
        /// <summary>
        /// Creates calendar based on loaded data rows. Loads attachments from database if required.
        /// </summary>
        /// <returns>Object that implements <see cref="ICalendar2"/>.</returns>
        private async Task<ICalendar2> GetCalendarAsync()
        {
            ICalendar2 cal = CalendarFactory.CreateCalendar2();
            cal.ProductId = cal.CreateTextProp("-//IT Hit//Collab Lib//EN");

            // Recurrent event or to-do can contain more than one VEVENT/VTODO component in one file.
            foreach (DataRow rowEventComponent in rowsEventComponents)
            {

                // add either event or to-do to the calendar
                bool isEvent = rowEventComponent.Field<bool>("ComponentType");
                IEventBase sc;
                if (isEvent)
                {
                    sc = cal.Events.CreateComponent();
                    cal.Events.Add(sc as IEvent);
                }
                else
                {
                    sc = cal.ToDos.CreateComponent();
                    cal.ToDos.Add(sc as IToDo);
                }

                // Read component properties from previously loaded [cal_EventComponent] rows.
                ReadEventComponent(sc, rowEventComponent, cal);


                Guid eventComponentId = rowEventComponent.Field<Guid>("EventComponentId");

                // Get [cal_RecurrenceException] rows that belong to this event component only and read recurrence exceptions dates.
                IEnumerable<DataRow> rowsThisScRecurrenceExceptions = rowsRecurrenceExceptions.Where(x => x.Field<Guid>("EventComponentId") == eventComponentId);
                ReadRecurrenceExceptions(sc.ExceptionDateTimes, rowsThisScRecurrenceExceptions, cal);

                // Get [cal_Alarm] rows that belong to this event component only and read alarms.
                IEnumerable<DataRow> rowsThisScAlarms = rowsAlarms.Where(x => x.Field<Guid>("EventComponentId") == eventComponentId);
                ReadAlarms(sc.Alarms, rowsThisScAlarms, cal);

                // Get [cal_Attendee] rows that belong to this event component only and read attendees.
                IEnumerable<DataRow> rowsThisScAttendees = rowsAttendees.Where(x => x.Field<Guid>("EventComponentId") == eventComponentId);
                ReadAttendees(sc.Attendees, rowsThisScAttendees, cal);

                // Get [cal_Attachment] rows that belong to this event component only.
                // Read attachments, load [cal_Attachment].[Content] if required.
                IEnumerable<DataRow> rowsThisScAttachments = rowsAttachments.Where(x => x.Field<Guid>("EventComponentId") == eventComponentId);
                await ReadAttachmentsAsync(Context, sc.Attachments, rowsThisScAttachments, cal);
            }

            // Generate VTIMEZONE components based on TZID parameters.
            cal.AutoGenerateTimeZones = true;

            return cal;
        }

        /// <summary>
        /// Reads data from [cal_EventComponent] row.
        /// </summary>
        /// <param name="sc">Event or to-do that will be populated from row paramater.</param>
        /// <param name="row">Data from [cal_EventComponent] table to populate sc.</param>
        /// <param name="cal">Calendar object.</param>
        private void ReadEventComponent(IEventBase sc, DataRow row, ICalendar2 cal)
        {
            bool isAllDay = row.Field<bool?>("AllDay").GetValueOrDefault();

            sc.Uid              = cal.CreateTextProp(row.Field<string>("UID"));                                     // UID property, iOS / OS X UID is case sensitive, Bynari WebDAV Collaborator UID is over 100 chars long
            sc.DateTimeStampUtc = cal.CreateDateProp(row.Field<DateTime?>("DateTimeStampUtc"), DateTimeKind.Utc);   // DTSTAMP property
            sc.CreatedUtc       = cal.CreateDateProp(row.Field<DateTime?>("CreatedUtc"), DateTimeKind.Utc);         // CREATED property
            sc.LastModifiedUtc  = cal.CreateDateProp(row.Field<DateTime?>("LastModifiedUtc"), DateTimeKind.Utc);    // LAST-MODIFIED property
            sc.Summary          = cal.CreateCalTextProp(row.Field<string>("Summary"));                              // SUMMARY property
            sc.Description      = cal.CreateCalTextProp(row.Field<string>("Description"));                          // DESCRIPTION property
            sc.Start            = cal.CreateCalDateProp(row.Field<DateTime?>("Start"), row.Field<string>("StartTimeZoneId"), isAllDay);                 // DTSTART property
            sc.Duration         = cal.CreateDurationProp(row.Field<long?>("Duration"));                             // DURATION property
            sc.Class            = cal.CreateClassProp(row.Field<string>("Class"));                                  // CLASS property
            sc.Location         = cal.CreateCalTextProp(row.Field<string>("Location"));                             // LOCATION property
            sc.Priority         = cal.CreateIntegerProp(row.Field<byte?>("Priority"));                              // PRIORITY property
            sc.Sequence         = cal.CreateIntegerProp(row.Field<int?>("Sequence"));                               // SEQ property
            sc.Status           = cal.CreateStatusProp(row.Field<string>("Status"));                                // STATUS property
            sc.Organizer        = cal.CreateCalAddressProp(EmailToUri(row.Field<string>("OrganizerEmail")), row.Field<string>("OrganizerCommonName"));  // ORGANIZER property

            // RECURRENCE-ID property
            sc.RecurrenceId     = cal.CreateRecurrenceIdProp(
                row.Field<DateTime?>("RecurrenceIdDate")                                                            // RECURRENCE-ID value
                , row.Field<string>("RecurrenceIdTimeZoneId")                                                       // RECURRENCE-ID TZID param
                , isAllDay                                                                                          // RECURRENCE-ID DATE or DATE-TIME
                , row.Field<bool?>("RecurrenceIdThisAndFuture").GetValueOrDefault());                                             // RECURRENCE-ID RANGE param

            // CATEGORIES property list
            string categories = row.Field<string>("Categories") as string;
            if (!string.IsNullOrEmpty(categories))
            {
                string[] strCatProp = categories.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string categoryList in strCatProp)
                {
                    ICategories catProp = sc.Categories.CreateProperty();
                    catProp.Categories = categoryList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    sc.Categories.Add(catProp);
                }
            }

            // RRULE property
            string recurFrequency = row.Field<string>("RecurFrequency");
            if (!string.IsNullOrEmpty(recurFrequency))
            {
                sc.RecurrenceRule = cal.CreateProperty<IRecurrenceRule>();

                sc.RecurrenceRule.Frequency = ExtendibleEnum.FromString<FrequencyType>(recurFrequency);             // FREQ rule part
                sc.RecurrenceRule.Interval  = (uint?)row.Field<int?>("RecurInterval");                              // INTERVAL rule part
                sc.RecurrenceRule.Count     = (uint?)row.Field<int?>("RecurCount");                                 // COUNT rule part

                // WKST rule part
                string weekStart = row.Field<string>("RecurWeekStart");
                if (!string.IsNullOrEmpty(weekStart))
                {
                    sc.RecurrenceRule.WeekStart = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), weekStart);
                }

                // UNTIL rule part
                DateTime? until = row.Field<DateTime?>("RecurUntil");
                if (until != null)
                {
                    // UNTIL must be in UTC if DTSTART contains time zone or DTSTART is UTC.
                    // UNTIL must be 'floating' if DTSTART is 'floating'.
                    // UNTIL must be 'all day' if the DTSTART is 'all day'.
                    // https://tools.ietf.org/html/rfc5545#section-3.3.10
                    sc.RecurrenceRule.Until = new Date(
                        DateTime.SpecifyKind(until.Value,
                        sc.Start.Value.DateVal.Kind != DateTimeKind.Local /*floating*/ ? DateTimeKind.Utc : DateTimeKind.Local),
                        sc.Start.Value.Components);
                }

                // BYDAY rule part
                string byDay = row.Field<string>("RecurByDay");
                if (!string.IsNullOrEmpty(byDay))
                {
                    sc.RecurrenceRule.ByDay = byDay.Split(',').Select(x => DayRule.Parse(x)).ToArray();
                }

                // BYMONTHDAY rule part
                string byMonthDay = row.Field<string>("RecurByMonthDay");
                if (!string.IsNullOrEmpty(byMonthDay))
                {
                    sc.RecurrenceRule.ByMonthDay = byMonthDay.Split(',').Select(x => short.Parse(x)).ToArray();
                }

                // BYMONTH rule part
                string byMonth = row.Field<string>("RecurByMonth");
                if (!string.IsNullOrEmpty(byMonth))
                {
                    sc.RecurrenceRule.ByMonth = byMonth.Split(',').Select(x => ushort.Parse(x)).ToArray();
                }

                // BYSETPOS  rule part
                string bySetPos = row.Field<string>("RecurBySetPos");
                if (!string.IsNullOrEmpty(bySetPos))
                {
                    sc.RecurrenceRule.BySetPos = bySetPos.Split(',').Select(x => short.Parse(x)).ToArray();
                }      
            }

            if (sc is IEvent)
            {
                // Properties specific for events only
                IEvent vEvent = sc as IEvent;
                vEvent.End          = cal.CreateCalDateProp(row.Field<DateTime?>("End"), row.Field<string>("EndTimeZoneId"), isAllDay); // DTEND property
                vEvent.Transparency = cal.CreateTransparencyProp(row.Field<bool?>("EventTransparency"));                                // TRANSP property
            }
            else
            {
                // Properties specific for to-dos only
                IToDo vToDo = sc as IToDo;
                vToDo.Due           = cal.CreateCalDateProp(row.Field<DateTime?>("End"), row.Field<string>("EndTimeZoneId"), isAllDay); // DUE property
                vToDo.CompletedUtc  = cal.CreateDateProp(row.Field<DateTime?>("ToDoCompletedUtc"), DateTimeKind.Utc);                   // COMPLETED property
                vToDo.PercentComplete = cal.CreateIntegerProp(row.Field<byte?>("ToDoPercentComplete"));                                 // PERCENT-COMPLETE
            }

            // Get custom properties and custom parameters
            Guid eventComponentId = row.Field<Guid>("EventComponentId");
            IEnumerable<DataRow> rowsEventCustomProperties = rowsCustomProperties.Where(x => x.Field<Guid>("ParentId") == eventComponentId);
            ReadCustomProperties(sc, rowsEventCustomProperties);
        }

        /// <summary>
        /// Reads data from [cal_RecurrenceException] rows.
        /// </summary>
        /// <param name="recurrenceExceptions">Empty recurrence exceptions dates list that will be populated with data from rowsRecurrenceExceptions parameter.</param>
        /// <param name="rowsRecurrenceExceptions">Data from [cal_RecurrenceException] table to populate recurrenceExceptions parameter.</param>
        private static void ReadRecurrenceExceptions(IPropertyList<ICalDateList> recurrenceExceptions, IEnumerable<DataRow> rowsRecurrenceExceptions, ICalendar2 cal)
        {
            foreach (DataRow rowRecurrenceException in rowsRecurrenceExceptions)
            {
                // EXDATE property
                ICalDateList exdate = cal.CreateCalDateListProp(
                    new DateTime[] { rowRecurrenceException.Field<DateTime>("ExceptionDate") }                          // EXDATE value
                    , rowRecurrenceException.Field<string>("TimeZoneId")                                                // EXDATE TZID param
                    , rowRecurrenceException.Field<bool?>("AllDay").GetValueOrDefault()                                 // EXDATE DATE or DATE-TIME
                    );
                recurrenceExceptions.Add(exdate);
            }
        }

        /// <summary>
        /// Reads data from [cal_Alarm] rows.
        /// </summary>
        /// <param name="alarms">Empty alarms list that will be populated with data from rowsAlarms parameter.</param>
        /// <param name="rowsAlarms">Data from [cal_Alarm] table to populate alarms parameter.</param>
        /// <param name="cal">Calendar object.</param>
        private void ReadAlarms(IComponentList<IAlarm> alarms, IEnumerable<DataRow> rowsAlarms, ICalendar2 cal)
        {
            foreach (DataRow rowAlarm in rowsAlarms)
            {
                IAlarm alarm = alarms.CreateComponent();                                            // VALARM component

                alarm.Action        = cal.CreateActionProp(rowAlarm.Field<string>("Action"));       // Alarm ACTION property
                alarm.Summary       = cal.CreateCalTextProp(rowAlarm.Field<string>("Summary"));     // Alarm SUMMARY property
                alarm.Description   = cal.CreateCalTextProp(rowAlarm.Field<string>("Description")); // Alarm DESCRIPTION property
                alarm.Duration      = cal.CreateDurationProp(rowAlarm.Field<long?>("Duration"));    // Alarm DURATION property
                alarm.Repeat        = cal.CreateIntegerProp(rowAlarm.Field<int?>("Repeat"));        // Alarm REPEAT property

                // Alarm TRIGGER property
                alarm.Trigger = cal.CreateProperty<ITrigger>();

                DateTime? absolute = rowAlarm.Field<DateTime?>("TriggerAbsoluteDateTimeUtc");
                if (absolute != null)
                {
                    alarm.Trigger.AbsoluteDateTimeUtc = DateTime.SpecifyKind(absolute.Value, DateTimeKind.Utc);
                }

                long? offset = rowAlarm.Field<long?>("TriggerRelativeOffset");
                if (offset != null)
                {
                    alarm.Trigger.RelativeOffset = new TimeSpan(offset.Value);
                }

                // Alarm trigger RELATED param
                bool? related = rowAlarm.Field<bool?>("TriggerRelatedStart");
                if (related != null)
                {
                    alarm.Trigger.Related = related.Value ? RelatedType.Start : RelatedType.End;
                }

                // Get custom properties and custom parameters
                Guid alarmId = rowAlarm.Field<Guid>("AlarmId");
                IEnumerable<DataRow> rowsEventCustomProperties = rowsCustomProperties.Where(x => x.Field<Guid>("ParentId") == alarmId);
                ReadCustomProperties(alarm, rowsEventCustomProperties);

                alarms.Add(alarm);
            }
        }

        /// <summary>
        /// Reads data from [cal_Attendee] rows.
        /// </summary>
        /// <param name="attendees">Empty attendees list that will be populated with data from rowsAttendees parameter.</param>
        /// <param name="rowsAttendees">Data from [cal_Attendee] table to populate attendees parameter.</param>
        /// <param name="cal">Calendar object.</param>
        private void ReadAttendees(IPropertyList<IAttendee> attendees, IEnumerable<DataRow> rowsAttendees, ICalendar2 cal)
        {
            foreach (DataRow rowAttendee in rowsAttendees)
            {                
                IAttendee attendee = attendees.CreateProperty();                                                // ATTENDEE property

                attendee.Uri            = EmailToUri(rowAttendee.Field<string>("Email"));                       // Attendee value
                attendee.CommonName     = rowAttendee.Field<string>("CommonName");                              // Attendee CN parameter
                attendee.Dir            = rowAttendee.Field<string>("DirectoryEntryRef");                       // Attendee DIR parameter
                attendee.Language       = rowAttendee.Field<string>("Language");                                // Attendee LANGUAGE parameter
                attendee.UserType       = StringToEnum<CalendarUserType>(rowAttendee.Field<string>("UserType"));// Attendee CUTYPE parameter
                attendee.SentBy         = EmailToUri(rowAttendee.Field<string>("SentBy"));                      // Attendee SENT-BY parameter
                attendee.DelegatedFrom  = new[] { EmailToUri(rowAttendee.Field<string>("DelegatedFrom")) };     // Attendee DELEGATED-FROM parameter, here we assume only 1 delegator for the sake of simplicity
                attendee.DelegatedTo    = new[] { EmailToUri(rowAttendee.Field<string>("DelegatedTo")) };       // Attendee DELEGATED-TO parameter, here we assume only 1 delegatee for the sake of simplicity

                // Attendee RSVP parameter
                bool? rsvp = rowAttendee.Field<bool?>("Rsvp");
                if (rsvp != null)
                {
                    attendee.Rsvp       = rsvp.Value ? RsvpType.True : RsvpType.False;
                }

                attendee.ParticipationRole   = StringToEnum<ParticipationRoleType>(rowAttendee.Field<string>("ParticipationRole"));     // Attendee ROLE parameter
                attendee.ParticipationStatus = StringToEnum<ParticipationStatusType>(rowAttendee.Field<string>("ParticipationStatus")); // Attendee PARTSTAT parameter

                AddParamValues(rowAttendee.Field<Guid>("AttendeeId"), attendee.RawProperty); // Add custom parameters from [cal_CustomProperty] table.

                attendees.Add(attendee);
            }
        }

        /// <summary>
        /// Reads data from [cal_Attachment] rows. Loads [cal_Attachment].[Content] if required.
        /// </summary>
        /// <param name="attachments">Empty attachments list that will be populated with data from rowsAttachments parameter.</param>
        /// <param name="rowsAttachments">Data from [cal_Attachment] table to populate attachments parameter.</param>
        /// <param name="cal">Calendar object.</param>
        private async Task ReadAttachmentsAsync(DavContext context, IPropertyList<IMedia> attachments, IEnumerable<DataRow> rowsAttachments, ICalendar2 cal)
        {
            // Find if any attachments content should be read from datatbase.
            bool loadContent = rowsAttachments.Any(x => (x.Field<int>("ContentExists") == 1));

            if (loadContent)
            {
                // Reading attachments content from database.

                // Set timeout to maximum value to be able to download iCalendar files with large file attachments.
                System.Web.HttpContext.Current.Server.ScriptTimeout = int.MaxValue;
                
                Guid eventComponentId = rowsAttachments.First().Field<Guid>("EventComponentId");
                string sql = "SELECT [AttachmentId], [MediaType], [ExternalUrl], [Content] FROM [cal_Attachment] WHERE [EventComponentId]=@EventComponentId";

                using (SqlDataReader reader = await context.ExecuteReaderAsync(CommandBehavior.SequentialAccess, sql, "@EventComponentId", eventComponentId))
                {
                    while(await reader.ReadAsync())
                    {
                        IMedia attachment = attachments.CreateProperty();                   // ATTACH property
                        Guid attachmentId = await reader.GetFieldValueAsync<Guid>(reader.GetOrdinal("AttachmentId"));

                        // Attachment FMTTYPE parameter
                        int ordMediaType = reader.GetOrdinal("MediaType");
                        if (!await reader.IsDBNullAsync(ordMediaType))
                        {
                            attachment.MediaType = await reader.GetFieldValueAsync<string>(ordMediaType);
                        }

                        // Attachment value as URL
                        int ordExternalUrl = reader.GetOrdinal("ExternalUrl");
                        if (!await reader.IsDBNullAsync(ordExternalUrl))
                        {
                            attachment.Uri = await reader.GetFieldValueAsync<string>(ordExternalUrl);
                        }
                                     
                        // Attachment value as inline content
                        int ordContent = reader.GetOrdinal("Content");
                        if (!await reader.IsDBNullAsync(ordContent))
                        {
                            using (Stream stream = reader.GetStream(ordContent))
                            {
                                using (MemoryStream memory = new MemoryStream())
                                {
                                    await stream.CopyToAsync(memory);
                                    attachment.Base64Data = Convert.ToBase64String(memory.ToArray());
                                }
                            }
                        }

                        AddParamValues(attachmentId, attachment.RawProperty); // Add custom parameters from [cal_CustomProperty] table.

                        attachments.Add(attachment);
                    }
                }
            }
            else
            {
                // Attachments contain only URLs to external files.
                foreach (DataRow rowAttachment in rowsAttachments)
                {
                    IMedia attachment = attachments.CreateProperty();                   // ATTACH property

                    attachment.MediaType = rowAttachment.Field<string>("MediaType");    // Attachment FMTTYPE parameter
                    attachment.Uri       = rowAttachment.Field<string>("ExternalUrl");  // Attachment value

                    AddParamValues(rowAttachment.Field<Guid>("AttachmentId"), attachment.RawProperty); // Add custom parameters from [cal_CustomProperty] table.

                    attachments.Add(attachment);
                }
            }
        }

        /// <summary>
        /// Reads custom properties and parameters from [cal_CustomProperty] table
        /// and creates them in component passed as a parameter.
        /// </summary>
        /// <param name="component">Component where custom properties and parameters will be created.</param>
        /// <param name="rowsCustomProperies">Custom properties datat from [cal_CustomProperty] table.</param>
        private static void ReadCustomProperties(IComponent component, IEnumerable<DataRow> rowsCustomProperies)
        {
            foreach (DataRow rowCustomProperty in rowsCustomProperies)
            {
                string propertyName = rowCustomProperty.Field<string>("PropertyName");

                IRawProperty prop;
                if (!component.Properties.ContainsKey(propertyName))
                {
                    prop = component.CreateRawProperty();
                    component.AddProperty(propertyName, prop);
                }
                else
                {
                    prop = component.Properties[propertyName].FirstOrDefault();
                }

                string paramName = rowCustomProperty.Field<string>("ParameterName");
                string value = rowCustomProperty.Field<string>("Value");
                if (paramName == null)
                {
                    // If ParameterName is null the Value contains property value
                    prop.RawValue = value;
                }
                else
                {
                    AddParamValue(prop, paramName, value);
                }
            }
        }

        /// <summary>
        /// Adds custom parameters to property.
        /// </summary>
        /// <param name="propertyId">ID from [cal_Attachment], [cal_Attendee] or [cal_Alarm] tables. Used to find parameters in [CustomProperties] table.</param>
        /// <param name="prop">Property to add parameters to.</param>
        private void AddParamValues(Guid propertyId, IRawProperty prop)
        {
            IEnumerable<DataRow> rowsCustomParams = rowsCustomProperties.Where(x => x.Field<Guid>("ParentId") == propertyId);
            foreach (DataRow rowCustomParam in rowsCustomParams)
            {
                string paramName = rowCustomParam.Field<string>("ParameterName");
                string paramValue = rowCustomParam.Field<string>("Value");
                AddParamValue(prop, paramName, paramValue);
            }
        }

        /// <summary>
        /// Adds value to property parameter.
        /// </summary>
        /// <param name="prop">Property.</param>
        /// <param name="paramName">Parameter name.</param>
        /// <param name="paramValue">Parameter value to be added.</param>
        private static void AddParamValue(IRawProperty prop, string paramName, string paramValue)
        {
            // There could be parameters with identical name withing one property.

            // This call returns all values from all properties with specified name.
            IEnumerable<string> paramVals = prop.Parameters[paramName];

            // Add value.
            List<string> paramNewVals = paramVals.ToList();
            paramNewVals.Add(paramValue);

            // This call removes any parameters with identical names if any and 
            // replaces it with a single parameter with a lost of specified values.
            prop.Parameters[paramName] = paramNewVals;
        }

        /// <summary>
        /// Adds "mailto:" schema to e-mail address if "@" is found. If null is passed returns null.
        /// </summary>
        /// <param name="email">E-mail.</param>
        /// <returns>E-mail string with "mailto:" schema.</returns>
        private static string EmailToUri(string email)
        {
            if (email == null)
                return null;

            if (email.IndexOf('@') > 0)
                return string.Format("mailto:{0}", email);

            return email;
        }

        /// <summary>
        /// Converts string to <see cref="ExtendibleEnum"/> of spcified type. Returns <b>null</b> if <b>null</b> is passed. 
        /// If no matching string value is found the <see cref="ExtendibleEnum.Name"/> is set to passed parameter <b>value</b> and <see cref="ExtendibleEnum.Number"/> is set to -1.
        /// </summary>
        /// <typeparam name="T">Type to convert to.</typeparam>
        /// <param name="value">String to convert from.</param>
        /// <returns><see cref="ExtendibleEnum"/> of type <b>T</b> or <b>null</b> if <b>null</b> is passed as a parameter.</returns>
        private static T StringToEnum<T>(string value) where T : ExtendibleEnum, new()
        {
            if (value == null)
                return null;

            T res;
            if (!ExtendibleEnum.TryFromString<T>(value, out res))
            {
                // If no matching value is found create new ExtendibleEnum or type T 
                // with specified string value and default numeric value (-1).
                res = new T();
                res.Name = value;
            }

            return res;
        }
    }
}
