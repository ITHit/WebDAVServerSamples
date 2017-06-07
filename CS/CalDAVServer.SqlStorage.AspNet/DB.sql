
USE [master]
GO

CREATE DATABASE WebDav
GO

USE [WebDav]
GO


/****** Object:  Table [dbo].[cal_Alarm] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_Alarm](
	[AlarmId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_Alarm_AlarmId]  DEFAULT (newid()),
	[EventComponentId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Action] [nvarchar](15) NOT NULL,
	[TriggerAbsoluteDateTimeUtc] [datetime2](7) NULL,
	[TriggerRelativeOffset] [bigint] NULL,
	[TriggerRelatedStart] [bit] NULL,
	[Summary] [nvarchar](255) NULL,
	[Description] [nvarchar](max) NULL,
	[Duration] [bigint] NULL,
	[Repeat] [int] NULL,
 CONSTRAINT [PK_cal_Alarm] PRIMARY KEY CLUSTERED 
(
	[AlarmId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_Attachment] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[cal_Attachment](
	[AttachmentId] [uniqueidentifier] NOT NULL,
	[EventComponentId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[MediaType] [nvarchar](50) NULL,
	[ExternalUrl] [nvarchar](max) NULL,
	[Content] [varbinary](max) NULL,
 CONSTRAINT [PK_cal_Attachment] PRIMARY KEY CLUSTERED 
(
	[AttachmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[cal_Attendee] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_Attendee](
	[AttendeeId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_Attendee_AttendeeId]  DEFAULT (newid()),
	[EventComponentId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Email] [nvarchar](50) NULL,
	[CommonName] [nvarchar](255) NULL,
	[DirectoryEntryRef] [nvarchar](max) NULL,
	[Language] [nvarchar](50) NULL,
	[UserType] [nvarchar](15) NULL,
	[SentBy] [nvarchar](50) NULL,
	[DelegatedFrom] [nvarchar](50) NULL,
	[DelegatedTo] [nvarchar](50) NULL,
	[Rsvp] [bit] NULL,
	[ParticipationRole] [nvarchar](15) NULL,
	[ParticipationStatus] [nvarchar](15) NULL,
 CONSTRAINT [PK_cal_Attendee] PRIMARY KEY CLUSTERED 
(
	[AttendeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_CalendarFile] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_CalendarFile](
	[UID] [nvarchar](255) NOT NULL,
	[CalendarFolderId] [uniqueidentifier] NOT NULL,
	[ETag] [timestamp] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_cal_CalendarFile_CreatedUtc]  DEFAULT (getutcdate()),
	[ModifiedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_cal_CalendarFile_ModifiedUtc]  DEFAULT (getutcdate()),
 CONSTRAINT [PK_cal_CalendarFile] PRIMARY KEY CLUSTERED 
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_CalendarFolder] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_CalendarFolder](
	[CalendarFolderId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_Calendar_CalendarFolderId]  DEFAULT (newid()),
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_cal_Calendar] PRIMARY KEY CLUSTERED 
(
	[CalendarFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO


/****** Object:  Table [dbo].[cal_CalendarFolderProperty] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_CalendarFolderProperty](
	[CalendarFolderPropertyId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_CalendarFolderProperty_CalendarFolderPropertyId] DEFAULT (newid()),
	[CalendarFolderId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Namespace] [nvarchar](100) NOT NULL,
	[PropVal] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_cal_CalendarFolderProperty] PRIMARY KEY CLUSTERED 
(
	[CalendarFolderPropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_RecurrenceException] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_RecurrenceException](
	[RecurrenceExceptionId] [uniqueidentifier] NOT NULL,
	[EventComponentId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[ExceptionDate] [datetime2](7) NOT NULL,
	[TimeZoneId] [nvarchar](255) NULL,
	[AllDay] [bit] NOT NULL,
 CONSTRAINT [PK_cal_RecurrenceException] PRIMARY KEY CLUSTERED 
(
	[RecurrenceExceptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_EventComponent] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_EventComponent](
	[EventComponentId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_EventComponent_EventComponentId]  DEFAULT (newid()),
	[UID] [nvarchar](255) NOT NULL,
	[ComponentType] [bit] NOT NULL,
	[DateTimeStampUtc] [datetime2](7) NOT NULL,
	[CreatedUtc] [datetime2](7) NULL,
	[LastModifiedUtc] [datetime2](7) NULL,
	[Summary] [nvarchar](511) NULL,
	[Description] [nvarchar](max) NULL,
	[OrganizerEmail] [nvarchar](255) NULL,
	[OrganizerCommonName] [nvarchar](50) NULL,
	[Start] [datetime2](7) NULL,
	[StartTimeZoneId] [nvarchar](255) NULL,
	[End] [datetime2](7) NULL,
	[EndTimeZoneId] [nvarchar](255) NULL,
	[Duration] [bigint] NULL,
	[AllDay] [bit] NULL,
	[Class] [nvarchar](50) NULL,
	[Location] [nvarchar](255) NULL,
	[Priority] [tinyint] NULL,
	[Sequence] [int] NULL,
	[Status] [nvarchar](50) NULL,
	[Categories] [nvarchar](255) NULL,
	[RecurFrequency] [nvarchar](50) NULL,
	[RecurInterval] [int] NULL,
	[RecurUntil] [datetime2](7) NULL,
	[RecurCount] [int] NULL,
	[RecurWeekStart] [nvarchar](50) NULL,
	[RecurByDay] [nvarchar](50) NULL,
	[RecurByMonthDay] [nvarchar](50) NULL,
	[RecurByMonth] [nvarchar](50) NULL,
	[RecurBySetPos] [nvarchar](50) NULL,
	[RecurrenceIdDate] [datetime2](7) NULL,
	[RecurrenceIdTimeZoneId] [nvarchar](255) NULL,
	[RecurrenceIdThisAndFuture] [bit] NULL,
	[EventTransparency] [bit] NULL,
	[ToDoCompletedUtc] [datetime2](7) NULL,
	[ToDoPercentComplete] [tinyint] NULL,
 CONSTRAINT [PK_cal_EventComponent] PRIMARY KEY CLUSTERED 
(
	[EventComponentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO
/****** Object:  Table [dbo].[cal_CustomProperty] ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[cal_CustomProperty](
	[CustomPropertyId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_CustomProperty_CustomPropertyId]  DEFAULT (newid()),
	[ParentId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[PropertyName] [nvarchar](50) NOT NULL,
	[ParameterName] [nvarchar](50) NULL,
	[Value] [nvarchar](512) NOT NULL,
 CONSTRAINT [PK_cal_CustomProperty] PRIMARY KEY CLUSTERED 
(
	[CustomPropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

/****** Object:  Table [dbo].[cal_Access] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[cal_Access](
	[AccessId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_cal_Access_AccessId]  DEFAULT (newid()),
	[CalendarFolderId] [uniqueidentifier] NOT NULL,
	[UserId] [nvarchar](50) NOT NULL,
	[Owner] [bit] NOT NULL,
	[Read] [bit] NOT NULL,
	[Write] [bit] NOT NULL,
 CONSTRAINT [PK_cal_Access] PRIMARY KEY CLUSTERED 
(
	[AccessId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[cal_Attachment] ADD  CONSTRAINT [DF_cal_Attachment_AttachmentId]  DEFAULT (newid()) FOR [AttachmentId]
GO
ALTER TABLE [dbo].[cal_RecurrenceException] ADD  CONSTRAINT [DF_cal_RecurrenceException_RecurrenceExceptionId]  DEFAULT (newid()) FOR [RecurrenceExceptionId]
GO

ALTER TABLE [dbo].[cal_Alarm]  WITH CHECK ADD  CONSTRAINT [FK_cal_Alarm_EventComponent] FOREIGN KEY([EventComponentId])
REFERENCES [dbo].[cal_EventComponent] ([EventComponentId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_Alarm] CHECK CONSTRAINT [FK_cal_Alarm_EventComponent]
GO

ALTER TABLE [dbo].[cal_Attachment]  WITH CHECK ADD  CONSTRAINT [FK_cal_Attachment_EventComponent] FOREIGN KEY([EventComponentId])
REFERENCES [dbo].[cal_EventComponent] ([EventComponentId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_Attachment] CHECK CONSTRAINT [FK_cal_Attachment_EventComponent]
GO

ALTER TABLE [dbo].[cal_Attendee]  WITH CHECK ADD  CONSTRAINT [FK_cal_Attendee_EventComponent] FOREIGN KEY([EventComponentId])
REFERENCES [dbo].[cal_EventComponent] ([EventComponentId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_Attendee] CHECK CONSTRAINT [FK_cal_Attendee_EventComponent]
GO

ALTER TABLE [dbo].[cal_CalendarFile]  WITH CHECK ADD  CONSTRAINT [FK_cal_CalendarFile_Calendar] FOREIGN KEY([CalendarFolderId])
REFERENCES [dbo].[cal_CalendarFolder] ([CalendarFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_CalendarFile] CHECK CONSTRAINT [FK_cal_CalendarFile_Calendar]
GO

ALTER TABLE [dbo].[cal_CalendarFolderProperty]  WITH CHECK ADD  CONSTRAINT [FK_cal_CalendarFolderProperty_CalendarFolder] FOREIGN KEY([CalendarFolderId])
REFERENCES [dbo].[cal_CalendarFolder] ([CalendarFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_CalendarFolderProperty] CHECK CONSTRAINT [FK_cal_CalendarFolderProperty_CalendarFolder]
GO

ALTER TABLE [dbo].[cal_RecurrenceException]  WITH CHECK ADD  CONSTRAINT [FK_cal_RecurrenceException_EventComponent] FOREIGN KEY([EventComponentId])
REFERENCES [dbo].[cal_EventComponent] ([EventComponentId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_RecurrenceException] CHECK CONSTRAINT [FK_cal_RecurrenceException_EventComponent]
GO

ALTER TABLE [dbo].[cal_EventComponent]  WITH CHECK ADD  CONSTRAINT [FK_cal_EventComponent_CalendarFile] FOREIGN KEY([UID])
REFERENCES [dbo].[cal_CalendarFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_EventComponent] CHECK CONSTRAINT [FK_cal_EventComponent_CalendarFile]
GO

ALTER TABLE [dbo].[cal_CustomProperty]  WITH CHECK ADD  CONSTRAINT [FK_cal_CustomProperty_CalendarFile] FOREIGN KEY([UID])
REFERENCES [dbo].[cal_CalendarFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_CustomProperty] CHECK CONSTRAINT [FK_cal_CustomProperty_CalendarFile]
GO

ALTER TABLE [dbo].[cal_Access]  WITH CHECK ADD  CONSTRAINT [FK_cal_Access_CalendarFolder] FOREIGN KEY([CalendarFolderId])
REFERENCES [dbo].[cal_CalendarFolder] ([CalendarFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[cal_Access] CHECK CONSTRAINT [FK_cal_Access_CalendarFolder]
GO

/****** Object:  Index [IX_cal_CalendarFolderProperty] ******/
CREATE NONCLUSTERED INDEX [IX_cal_CalendarFolderProperty] ON [dbo].[cal_CalendarFolderProperty]
(
	[CalendarFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_CalendarFile] ******/
CREATE NONCLUSTERED INDEX [IX_cal_CalendarFile] ON [dbo].[cal_CalendarFile]
(
	[CalendarFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_EventComponent] ******/
CREATE NONCLUSTERED INDEX [IX_cal_EventComponent] ON [dbo].[cal_EventComponent]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_RecurrenceException] ******/
CREATE NONCLUSTERED INDEX [IX_cal_RecurrenceException] ON [dbo].[cal_RecurrenceException]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_Alarm] ******/
CREATE NONCLUSTERED INDEX [IX_cal_Alarm] ON [dbo].[cal_Alarm]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_Attendee] ******/
CREATE NONCLUSTERED INDEX [IX_cal_Attendee] ON [dbo].[cal_Attendee]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_Attachment] ******/
CREATE NONCLUSTERED INDEX [IX_cal_Attachment] ON [dbo].[cal_Attachment]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_CustomProperty] ******/
CREATE NONCLUSTERED INDEX [IX_cal_CustomProperty] ON [dbo].[cal_CustomProperty]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_cal_Access] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_cal_Access] ON [dbo].[cal_Access]
(
	[UserId] ASC,
	[CalendarFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'AlarmId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Component to which this alarm belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'EventComponentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this alarm belongs. Only required here to reduce ammount of joins.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Defines which action to be invoked when an alarm is triggered (AUDIO/DISPLAY/EMAIL).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'Action'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The absolute date-time for the alarm in UTC. NULL if relative time is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'TriggerAbsoluteDateTimeUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The time for the trigger of the alarm relative to the start or the end of an event or to-do. NULL if absolute time is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'TriggerRelativeOffset'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Relationship of the alarm trigger with respect to the start or end of the calendar component in case relative time is specified. NULL if absolute date-time is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'TriggerRelatedStart'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Text to be used as the message subject when the Action is EMAIL.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'Summary'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Text to be displayed when the alarm is triggered if the Action is DISPLAY or the e-mail message body when the Action is EMAIL.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'Description'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Alarm duration in ticks. 1 tick = 100 nanoseconds.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'Duration'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The number of times the alarm should be repeated, after the initial trigger.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm', @level2type=N'COLUMN',@level2name=N'Repeat'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'A reminder or alarm for an event or a to-do.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Alarm'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Component to which this attchment belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment', @level2type=N'COLUMN',@level2name=N'EventComponentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this attachment belongs. Only required here to reduce ammount of joins.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Media type. Typically NULL when external URL is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment', @level2type=N'COLUMN',@level2name=N'MediaType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Attachment URL. NULL if file content is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment', @level2type=N'COLUMN',@level2name=N'ExternalUrl'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Attacment file content. NULL if external url is specified. It is recommended to keep attchment size below 256Kb. In case over 1Mb should be stored, convert to FILESTREAM, FileTable or store in file system.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment', @level2type=N'COLUMN',@level2name=N'Content'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contains event and to-do attchments.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attachment'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Component to which this this attendee is assigned.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'EventComponentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this attendee belongs. Only required here to reduce ammount of joins.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Attengee e-mail.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'Email'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Attendee common name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'CommonName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Reference to a directory entry associated with the attendee.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'DirectoryEntryRef'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Language.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'Language'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar user type (INDIVIDUAL/GROUP/RESOURCE/ROOM/UNKNOWN)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'UserType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used to indicate whom is acting on behalf of the attendee. Typically e-mail.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'SentBy'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used to indicate whom the request was delegated from. Typically e-mail.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'DelegatedFrom'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used to indicate the calendar users that the original request was delegated to. Typically e-mail.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'DelegatedTo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used for indicating whether the favor of a reply is requested.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'Rsvp'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used to specify the participation role (CHAIR/REQ-PARTICIPANT/OPT-PARTICIPANT/NON-PARTICIPANT).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'ParticipationRole'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Used to specify participation status (NEEDS-ACTION/ACCEPTED/DECLINED/TENTATIVE/DELEGATED/COMPLETED/IN-PROCESS).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee', @level2type=N'COLUMN',@level2name=N'ParticipationStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores attendees for an event, to-do, journal and free-busy calendar components.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Attendee'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) UID. All events or to-dos components withing a file has this UID.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar (calendar folder) to which this component belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile', @level2type=N'COLUMN',@level2name=N'CalendarFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Automaticaly changes each time this object is updated. Used for synchronization operations.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile', @level2type=N'COLUMN',@level2name=N'ETag'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time when this file was created. Typically CalDAV clients never request this property, used for demo purposes only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile', @level2type=N'COLUMN',@level2name=N'CreatedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time when this file was modified. This property is updated to trigger ETag update. Typically CalDAV clients never request this property, used for demo purposes only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile', @level2type=N'COLUMN',@level2name=N'ModifiedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contains calendar files (.ics). Each file contains one or more event or to-do instances.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFile'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolder', @level2type=N'COLUMN',@level2name=N'Name'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar description.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolder', @level2type=N'COLUMN',@level2name=N'Description'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contains calendars (calendar folders). Calendar folder contains calendar files each containing event or to-do description.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolder'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar (calendar folder) to which this custom property belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolderProperty', @level2type=N'COLUMN',@level2name=N'CalendarFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolderProperty', @level2type=N'COLUMN',@level2name=N'Name'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property namespace.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolderProperty', @level2type=N'COLUMN',@level2name=N'Namespace'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolderProperty', @level2type=N'COLUMN',@level2name=N'PropVal'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar WebDAV custom properties.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CalendarFolderProperty'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Component to which this this recurrence rule exception belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException', @level2type=N'COLUMN',@level2name=N'EventComponentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this recurrence exception belongs. Only required here to reduce ammount of joins.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence exception date.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException', @level2type=N'COLUMN',@level2name=N'ExceptionDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence exception date time zone ID. If if NULL - ExceptionDate is a "floating" time. If contains "UTC" - the ExceptionDate is in UTC.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException', @level2type=N'COLUMN',@level2name=N'TimeZoneId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time in ExceptionDate should be ignored for all-day events or to-dos.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException', @level2type=N'COLUMN',@level2name=N'AllDay'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contains exceptions for recurring events and to-dos.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_RecurrenceException'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'EventComponentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this component belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of component. 1 - event, 0 - to-do.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'ComponentType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies the date and time in UTC when the object was created.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'DateTimeStampUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies the date and time in UTC when the calendar information was created by the calendar user agent in the calendar store.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'CreatedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies the date and time in UTC when the information associated with the calendar component was last revised in the calendar store.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'LastModifiedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Defines a short summary or subject.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Summary'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Provides a more complete description of the calendar component than that provided by the "SUMMARY" property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Description'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies e-mail of the organizer of the event or to-do.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'OrganizerEmail'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies common name  of the organizer of the event or to-do.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'OrganizerCommonName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies the inclusive start of the event or to-do. For recurring events and to-dos, it also specifies the very first recurrence instance. Could be "floating" time, time relative to time zone in StartTimeZone field or UTC.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Start'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time zone ID for event or to-do Start time. If if NULL - Start is a "floating" time. If contains "UTC" - Start is in UTC.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'StartTimeZoneId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies the non-inclusive end of the event or due time for to-do component. Could be "floating" time, time relative to time zone in EndTimeZone field or UTC. NULL if Duration is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'End'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time zone ID for event End or to-do due time. If if NULL - End is a "floating" time. If contains "UTC" - the End is in UTC. Contains NULL if Duration is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'EndTimeZoneId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do duration in ticks. 1 tick = 100 nanoseconds. NULL if End is specified.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Duration'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'True for all-day event or to-do. Time in Start and End/Due property should be ignored for all-day events or to-dos. StartTimeZone/EndTimeZone must be set to NULL for all-day events and to-dos.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'AllDay'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies access classification (PUBLIC/PRIVATE/CONFIDENTIAL).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Class'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Location description.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Location'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Relative priority (0-9). A value of 0 specifies an undefined priority. A value of 1 is the highest priority.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Priority'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Defines the revision sequence number of the calendar component within a sequence of revisions.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Sequence'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Overall status or confirmation for the event or to-do (TENTATIVE/CONFIRMED/CANCELLED).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Status'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies categories or subtypes of the calendar component, coma-separated list.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'Categories'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence rule frequency (SECONDLY/MINUTELY/HOURLY/DAILY/WEEKLY/MONTHLY/YEARLY).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurFrequency'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Positive integer representing at which intervals the recurrence rule repeats.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurInterval'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Date-time untill which the recurrence rule is valid. Could be "floating" time or UTC, depending on Start value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurUntil'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contains the number of occurrences at which to range-bound the recurrence.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurCount'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'RecurWeekStart.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurWeekStart'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Days of the week for weekly, monthly or yearly recurrence rule separated with '',''. For example: ''TU,WE,FR'' or ''1SU,-1SU''.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurByDay'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Comma-separated list of days of the month. Valid values are 1 to 31 or -31 to -1.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurByMonthDay'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Comma-separated list of months of the year. Valid values are 1 to 12.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurByMonth'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Comma-separated list of days of the month. Valid values are 1 to 31 or -31 to -1.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurBySetPos'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence ID date or date and time. The value must be of the same type as Start value: "floating" time, UTC, or time in specific time zone.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurrenceIdDate'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence ID time zone ID.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurrenceIdTimeZoneId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Recurrence ID RANGE parameter. If true - indicates a range defined by the given recurrence instance and all subsequent instances.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'RecurrenceIdThisAndFuture'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Defines whether or not an event is transparent to busy time searches. Valid for events only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'EventTransparency'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Defines the date and time in UTC that a to-do was actually completed. Valid for to-dos only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'ToDoCompletedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Percent completion of a to-do. Valid for to-dos only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent', @level2type=N'COLUMN',@level2name=N'ToDoPercentComplete'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores calendars events and to-dos components (VEVENT and VTODO). Every calendar file can contain more than one component all sharing the same UID.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_EventComponent'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Parent component ID or parent property ID to which this custom property or parameter belongs to. This could be EventComponentId, AlarmId, AttachmentId, AttendeeId.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty', @level2type=N'COLUMN',@level2name=N'ParentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Event or to-do (calendar file) to which this custom property or property parameter belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Property name. This could be a custom property name (starting with ''X-'') or standard property name in case standard property contains custom parameters.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty', @level2type=N'COLUMN',@level2name=N'PropertyName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Parameter name. If null - Value field contains property value. Otherwise Value field contains parameter value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty', @level2type=N'COLUMN',@level2name=N'ParameterName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Property or parameter value. If ParameterName is null - this is a property value. If ParameterName is not null - this is a parameter value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty', @level2type=N'COLUMN',@level2name=N'Value'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores iCalendar custom properties and parameters.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_CustomProperty'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Calendar (calendar folder) for which user privileges are applied.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access', @level2type=N'COLUMN',@level2name=N'CalendarFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User ID of the user that has access to a calendar.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access', @level2type=N'COLUMN',@level2name=N'UserId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies if a user owns a calendar.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access', @level2type=N'COLUMN',@level2name=N'Owner'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User has a read privilege on a calendar.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access', @level2type=N'COLUMN',@level2name=N'Read'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User has a write privilege on a calendar.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access', @level2type=N'COLUMN',@level2name=N'Write'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores user calendar access privileges.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'cal_Access'
GO

