IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'DBSettings'))
BEGIN
CREATE TABLE [dbo].[DBSettings](
	[SettingKey] [varchar](50) NOT NULL,
	[SettingValue] [varchar](max) NULL,
 CONSTRAINT [PK_DBSettings] PRIMARY KEY CLUSTERED 
(
	[SettingKey] ASC
))
END
GO

IF (NOT EXISTS (SELECT SettingKey FROM [dbo].[DBSettings] WHERE SettingKey = 'SchemaVersion'))
BEGIN
INSERT INTO [dbo].[DBSettings] (SettingKey, SettingValue) VALUES ('SchemaVersion', '0')
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'UploadLogs'))
BEGIN
CREATE TABLE [dbo].[UploadLogs](
	[id] [uniqueidentifier] NOT NULL,
	[post_fullname] [varchar](max) NOT NULL,
	[requestor_username] [varchar](50) NOT NULL,
	[reply_fullname] [varchar](max) NOT NULL,
	[upload_destination] [varchar](50) NOT NULL,
	[upload_delete_key] [varchar](50) NOT NULL,
  [upload_datetime] [datetime] NOT NULL,
  [reply_deleted] [bit] NOT NULL,
  [upload_deleted] [bit] NOT NULL,
	[delete_datetime] [datetime] NULL,
	[delete_reason] [varchar](50) NULL,
 CONSTRAINT [PK_UploadLogs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
))
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'FallbackRepliesLinks'))
BEGIN
CREATE TABLE [dbo].[FallbackRepliesLinks](
	[link_fullname] [varchar](50) NOT NULL,
	[link_datetime] [datetime] NOT NULL,
 CONSTRAINT [PK_FallbackRepliesLinks] PRIMARY KEY CLUSTERED 
(
	[link_fullname] ASC
))
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'BlacklistedUsers'))
BEGIN
CREATE TABLE [dbo].[BlacklistedUsers](
	[username] [varchar](50) NOT NULL,
  [banned_by] [varchar](50) NOT NULL,
 CONSTRAINT [PK_BlacklistedUsers] PRIMARY KEY CLUSTERED 
(
	[username] ASC
))
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'BlacklistedSubreddits'))
BEGIN
CREATE TABLE [dbo].[BlacklistedSubreddits](
	[subreddit] [varchar](50) NOT NULL,
  [banned_by] [varchar](50) NOT NULL,
 CONSTRAINT [PK_BlacklistedSubreddits] PRIMARY KEY CLUSTERED 
(
	[subreddit] ASC
))
END
GO