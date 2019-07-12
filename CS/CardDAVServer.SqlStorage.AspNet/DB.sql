
USE [master]
GO

CREATE DATABASE [WebDav]
GO

USE [WebDav]
GO



/****** Object:  Table [dbo].[card_Address] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_Address](
	[AddressId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[PoBox] [nvarchar](50) NULL,
	[AppartmentNumber] [nvarchar](15) NULL,
	[Street] [nvarchar](255) NULL,
	[Locality] [nvarchar](50) NULL,
	[Region] [nvarchar](50) NULL,
	[PostalCode] [nvarchar](15) NULL,
	[Country] [nvarchar](50) NULL,
	[PreferenceLevel] [tinyint] NULL,
	[Label] [nvarchar](50) NULL,
	[Geo] [nvarchar](50) NULL,
	[TimeZone] [nvarchar](50) NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_Address] PRIMARY KEY CLUSTERED 
(
	[AddressId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[card_AddressbookFolder] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_AddressbookFolder](
	[AddressbookFolderId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_card_AddressbookFolder_AddressbookFolderId]  DEFAULT (newid()),
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_card_AddressbookFolder] PRIMARY KEY CLUSTERED 
(
	[AddressbookFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


/****** Object:  Table [dbo].[card_AddressbookFolderProperty] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_AddressbookFolderProperty](
	[AddressbookFolderPropertyId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_card_AddressbookFolderProperty_AddressbookFolderPropertyId] DEFAULT (newid()),
	[AddressbookFolderId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Namespace] [nvarchar](100) NOT NULL,
	[PropVal] [nvarchar](255) NOT NULL,
 CONSTRAINT [PK_card_AddressbookFolderProperty] PRIMARY KEY CLUSTERED 
(
	[AddressbookFolderPropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[card_CardFile] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[card_CardFile](
	[UID] [nvarchar](255) NOT NULL,
	[AddressbookFolderId] [uniqueidentifier] NOT NULL,
	[FileName] [nvarchar](255) NOT NULL,
	[ETag] [timestamp] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_card_CardFile_CreatedUtc]  DEFAULT (getutcdate()),
	[ModifiedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_card_CardFile_ModifiedUtc]  DEFAULT (getutcdate()),
	[Version] [nvarchar](3) NOT NULL,
	[FormattedName] [nvarchar](255) NOT NULL,
	[FamilyName] [nvarchar](50) NOT NULL,
	[GivenName] [nvarchar](50) NOT NULL,
	[AdditionalNames] [nvarchar](50) NOT NULL,
	[HonorificPrefix] [nvarchar](50) NOT NULL,
	[HonorificSuffix] [nvarchar](50) NOT NULL,
	[Product] [nvarchar](50) NULL,
	[Kind] [nvarchar](15) NULL,
	[Nickname] [nvarchar](50) NULL,
	[Photo] [varbinary](max) NULL,
	[PhotoMediaType] [nvarchar](15) NULL,
	[Logo] [varbinary](max) NULL,
	[LogoMediaType] [nvarchar](15) NULL,
	[Sound] [varbinary](max) NULL,
	[SoundMediaType] [nvarchar](15) NULL,
	[Birthday] [datetime2](7) NULL,
	[Anniversary] [datetime2] NULL,
	[Gender] [nvarchar](1) NULL,
	[RevisionUtc] [datetime2](7) NULL,
	[SortString] [nvarchar](255) NULL,
	[Language] [nvarchar](50) NULL,
	[TimeZone] [nvarchar](50) NULL,
	[Geo] [nvarchar](50) NULL,
	[Title] [nvarchar](255) NULL,
	[Role] [nvarchar](255) NULL,
	[OrgName] [nvarchar](255) NULL,
	[OrgUnit] [nvarchar](255) NULL,
	[Categories] [nvarchar](255) NULL,
	[Note] [nvarchar](max) NULL,
	[Classification] [nvarchar](50) NULL,
 CONSTRAINT [PK_card_CardFile] PRIMARY KEY CLUSTERED 
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[card_Email] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_Email](
	[EmailId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[Email] [nvarchar](50) NOT NULL,
	[PreferenceLevel] [tinyint] NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_Email] PRIMARY KEY CLUSTERED 
(
	[EmailId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[card_InstantMessenger] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_InstantMessenger](
	[InstantMessengerId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[InstantMessenger] [nvarchar](50) NOT NULL,
	[PreferenceLevel] [tinyint] NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_InstantMessenger] PRIMARY KEY CLUSTERED 
(
	[InstantMessengerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[card_Telephone] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_Telephone](
	[TelephoneId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[Telephone] [nvarchar](20) NOT NULL,
	[PreferenceLevel] [tinyint] NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_Telephone] PRIMARY KEY CLUSTERED 
(
	[TelephoneId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[card_Url] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_Url](
	[UrlId] [uniqueidentifier] NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[Url] [nvarchar](20) NOT NULL,
	[PreferenceLevel] [tinyint] NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_Url] PRIMARY KEY CLUSTERED 
(
	[UrlId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[card_CustomProperty] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_CustomProperty](
	[CustomPropertyId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_card_CustomProperty_CustomPropertyId]  DEFAULT (newid()),
	[ParentId] [nvarchar](255) NOT NULL,
	[UID] [nvarchar](255) NOT NULL,
	[ClientAppName] [nvarchar](255) NULL,
	[PropertyName] [nvarchar](50) NOT NULL,
	[ParameterName] [nvarchar](50) NULL,
	[Value] [nvarchar](512) NOT NULL,
	[SortIndex] [int] NULL,
 CONSTRAINT [PK_card_CustomProperty] PRIMARY KEY CLUSTERED 
(
	[CustomPropertyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

/****** Object:  Table [dbo].[card_Access] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[card_Access](
	[AccessId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_card_Access_AccessId]  DEFAULT (newid()),
	[AddressbookFolderId] [uniqueidentifier] NOT NULL,
	[UserId] [nvarchar](50) NOT NULL,
	[Owner] [bit] NOT NULL,
	[Read] [bit] NOT NULL,
	[Write] [bit] NOT NULL,
 CONSTRAINT [PK_card_Access] PRIMARY KEY CLUSTERED 
(
	[AccessId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


ALTER TABLE [dbo].[card_Address]  WITH CHECK ADD  CONSTRAINT [FK_card_Address_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_Address] CHECK CONSTRAINT [FK_card_Address_Card]
GO

ALTER TABLE [dbo].[card_AddressbookFolderProperty]  WITH CHECK ADD  CONSTRAINT [FK_card_AddressbookFolderProperty_AddressbookFolder] FOREIGN KEY([AddressbookFolderId])
REFERENCES [dbo].[card_AddressbookFolder] ([AddressbookFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_AddressbookFolderProperty] CHECK CONSTRAINT [FK_card_AddressbookFolderProperty_AddressbookFolder]
GO

ALTER TABLE [dbo].[card_CardFile]  WITH CHECK ADD  CONSTRAINT [FK_card_CardFile_AddressbookFolder] FOREIGN KEY([AddressbookFolderId])
REFERENCES [dbo].[card_AddressbookFolder] ([AddressbookFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_CardFile] CHECK CONSTRAINT [FK_card_CardFile_AddressbookFolder]
GO

ALTER TABLE [dbo].[card_Email]  WITH CHECK ADD  CONSTRAINT [FK_card_Email_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_Email] CHECK CONSTRAINT [FK_card_Email_Card]
GO

ALTER TABLE [dbo].[card_InstantMessenger]  WITH CHECK ADD  CONSTRAINT [FK_card_InstantMessenger_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_InstantMessenger] CHECK CONSTRAINT [FK_card_InstantMessenger_Card]
GO

ALTER TABLE [dbo].[card_Telephone]  WITH CHECK ADD  CONSTRAINT [FK_card_Telephone_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_Telephone] CHECK CONSTRAINT [FK_card_Telephone_Card]
GO

ALTER TABLE [dbo].[card_Url]  WITH CHECK ADD  CONSTRAINT [FK_card_Url_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_Url] CHECK CONSTRAINT [FK_card_Url_Card]
GO

ALTER TABLE [dbo].[card_CustomProperty]  WITH CHECK ADD  CONSTRAINT [FK_card_CustomProperty_Card] FOREIGN KEY([UID])
REFERENCES [dbo].[card_CardFile] ([UID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_CustomProperty] CHECK CONSTRAINT [FK_card_CustomProperty_Card]
GO

ALTER TABLE [dbo].[card_Access]  WITH CHECK ADD  CONSTRAINT [FK_card_Access_AddressbookFolder] FOREIGN KEY([AddressbookFolderId])
REFERENCES [dbo].[card_AddressbookFolder] ([AddressbookFolderId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[card_Access] CHECK CONSTRAINT [FK_card_Access_AddressbookFolder]
GO

/****** Object:  Index [IX_card_AddressbookFolderProperty] ******/
CREATE NONCLUSTERED INDEX [IX_card_AddressbookFolderProperty] ON [dbo].[card_AddressbookFolderProperty]
(
	[AddressbookFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_CardFileAddressbookFolderId] ******/
CREATE NONCLUSTERED INDEX [IX_card_CardFileAddressbookFolderId] ON [dbo].[card_CardFile]
(
	[AddressbookFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_CardFileFileName] ******/
CREATE NONCLUSTERED INDEX [IX_card_CardFileFileName] ON [dbo].[card_CardFile]
(
	[FileName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO


/****** Object:  Index [IX_card_Email] ******/
CREATE NONCLUSTERED INDEX [IX_card_Email] ON [dbo].[card_Email]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_InstantMessenger] ******/
CREATE NONCLUSTERED INDEX [IX_card_InstantMessenger] ON [dbo].[card_InstantMessenger]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_Address] ******/
CREATE NONCLUSTERED INDEX [IX_card_Address] ON [dbo].[card_Address]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_Telephone] ******/
CREATE NONCLUSTERED INDEX [IX_card_Telephone] ON [dbo].[card_Telephone]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_Url] ******/
CREATE NONCLUSTERED INDEX [IX_card_Url] ON [dbo].[card_Url]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_CustomProperty] ******/
CREATE NONCLUSTERED INDEX [IX_card_CustomProperty] ON [dbo].[card_CustomProperty]
(
	[UID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Index [IX_card_Access] ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_card_Access] ON [dbo].[card_Access]
(
	[UserId] ASC,
	[AddressbookFolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this address belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coma separated list of type flags (WORK/HOME/DOM/INTL/POSTAL/PARCEL/etc.)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Type'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'PO box. For max interoperability, should be empty.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'PoBox'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Appartment number. For max interoperability, should be empty.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'AppartmentNumber'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Street name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Street'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'City name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Locality'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Region (e.g., state or province).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Region'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Postal code.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'PostalCode'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Country name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Country'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores prefernce level for vCard 4.0. 1-100, 1-most prefered. In case of vCard 3.0 stores 1 for prefered property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'PreferenceLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Delivery address label.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Label'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coordinates associated with this address. vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'Geo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time zone for this address. vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address', @level2type=N'COLUMN',@level2name=N'TimeZone'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores addresses.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Address'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Address book name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolder', @level2type=N'COLUMN',@level2name=N'Name'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Address book description.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolder', @level2type=N'COLUMN',@level2name=N'Description'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Contain CardDAV addressbooks (addressbook folders)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolder'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Addressbook to which this custom property belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolderProperty', @level2type=N'COLUMN',@level2name=N'AddressbookFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolderProperty', @level2type=N'COLUMN',@level2name=N'Name'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property namespace.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolderProperty', @level2type=N'COLUMN',@level2name=N'Namespace'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom property value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolderProperty', @level2type=N'COLUMN',@level2name=N'PropVal'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Addressbook WebDAV custom properties.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_AddressbookFolderProperty'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business card UID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Address book (address book folder) to which this business card belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'AddressbookFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'File name without extension. In case of CardDAV this is different from UID.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'FileName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'vCard version - 2.1/3.0/4.0.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Version'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Client app that created this card. For exmple -//Apple Inc.//iOS 10.0.2//EN' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Product'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Formatted name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'FormattedName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Last name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'FamilyName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'First name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'GivenName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Middle names, coma-separated list.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'AdditionalNames'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Honorific prefix.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'HonorificPrefix'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Honorific suffix.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'HonorificSuffix'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Kind of object the business card represents (individual/group/org/locaton). vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Kind'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business card nickname.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Nickname'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Photoghraph image associated with this business card, typically when bysiness card represents an individual.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Photo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Photo media type. PNG/JPEG/etc.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'PhotoMediaType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logo associated with this business card typically when bysiness card represents an organization.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Logo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logo media type. PNG/JPEG/etc.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'LogoMediaType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Digital sound content associated with this business card.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Sound'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Sound media type. PNG/JPEG/etc.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'SoundMediaType'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Birth date of the object the business card represents.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Birthday'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Anniversary of the object the business card represents. vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Anniversary'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Gender of the identity this business card represents (M - male, F- female, O - other, N - none, U - unknown). vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Gender'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Revision date-time in UTC.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'RevisionUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Text to be used as a sort string when displaying info on a client side. Typically family name or combination of family name and given name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'SortString'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Language used for contacting. vCard 4.0 only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Language'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Represents information related to the time zone of the business object.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'TimeZone'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business object location - latitude and longitude.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Geo'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The position or job of the business card.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Title'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'The function or part played in a particular situation by the business object.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Role'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Organization name.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'OrgName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Organization unit.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'OrgUnit'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Tags associated with business card.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Categories'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business card supplemental information or a comment.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Note'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business card access classification.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'Classification'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores business card (vCard files).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Automaticaly changes each time this object is updated.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'ETag'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time when this file was created. Typically CardDAV clients never request this property, used for demo purposes only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'CreatedUtc'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time when this file was modified. This property is updated to trigger ETag update. Typically CardDAV clients never request this property, used for demo purposes only.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CardFile', @level2type=N'COLUMN',@level2name=N'ModifiedUtc'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this telephone belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coma separated list of type flags (WORK/HOME/INTERNET/X400/etc.).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email', @level2type=N'COLUMN',@level2name=N'Type'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'E-mail.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email', @level2type=N'COLUMN',@level2name=N'Email'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores prefernce level for vCard 4.0. 1-100, 1-most prefered. In case of vCard 2.1 and 3.0 stores 1 for prefered property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email', @level2type=N'COLUMN',@level2name=N'PreferenceLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores e-mails.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Email'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this instant messenger belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coma separated list of type flags (WORK/HOME/PERSONAL/BUSINESS/MOBILE/etc.).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger', @level2type=N'COLUMN',@level2name=N'Type'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Instant messenger.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger', @level2type=N'COLUMN',@level2name=N'InstantMessenger'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores prefernce level for vCard 4.0. 1-100, 1-most prefered. In case of vCard 2.1 and 3.0 stores 1 for prefered property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger', @level2type=N'COLUMN',@level2name=N'PreferenceLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores stores instant messengers.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_InstantMessenger'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this telephone belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coma separated list of type flags (WORK/HOME/TEXT/TEXTPHONE/PCS/VOICE/FAX/MSG/CELL/PAGER/BBS/MODEM/CAR/ISDN/VIDEO/etc.).' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone', @level2type=N'COLUMN',@level2name=N'Type'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Telephone.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone', @level2type=N'COLUMN',@level2name=N'Telephone'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores prefernce level for vCard 4.0. 1-100, 1-most prefered. In case of vCard 2.1 and 3.0 stores 1 for prefered property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone', @level2type=N'COLUMN',@level2name=N'PreferenceLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores telephones' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Telephone'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this URL belongs.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Coma separated list of type flags.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url', @level2type=N'COLUMN',@level2name=N'Type'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Url.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url', @level2type=N'COLUMN',@level2name=N'Url'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores prefernce level for vCard 4.0. 1-100, 1-most prefered. In case of vCard 2.1 and 3.0 stores 1 for prefered property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url', @level2type=N'COLUMN',@level2name=N'PreferenceLevel'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores URLs' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Url'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Parent Card ID or parent property ID to which this custom property or parameter belongs to. This could be UID, EmailId, InstantMessengerId, AddressId, TelephoneId, UrlId.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'ParentId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Card ID to which this custom property or property parameter belongs to.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'UID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Property name. This could be a custom property name (starting with ''X-'') or standard property name in case standard property contains custom parameters.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'PropertyName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Client application name that created this property. If business card is updated by any other client application except specified in this field, property will be preserved. Used to prevent custom props deletion by client apps that can not interpret this property.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'ClientAppName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Parameter name. If null - Value field contains property value. Otherwise Value field contains parameter value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'ParameterName'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Property or parameter value. If ParameterName is null - this is a property value. If ParameterName is not null - this is a parameter value.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'Value'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Propery position in vCard. Used to keep properties in the order submitted by the CardDAV client app.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty', @level2type=N'COLUMN',@level2name=N'SortIndex'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores vCard custom properties and parameters.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_CustomProperty'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Address book (address book folder) for which user privileges are applied.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access', @level2type=N'COLUMN',@level2name=N'AddressbookFolderId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User ID of the user that has access to an address book.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access', @level2type=N'COLUMN',@level2name=N'UserId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Specifies if a user owns an address book.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access', @level2type=N'COLUMN',@level2name=N'Owner'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User has a read privilege on an address book.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access', @level2type=N'COLUMN',@level2name=N'Read'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User has a write privilege on an address book.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access', @level2type=N'COLUMN',@level2name=N'Write'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Stores user address books access privileges.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'card_Access'
GO
