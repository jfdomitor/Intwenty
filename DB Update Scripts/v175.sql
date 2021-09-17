
/*

Update Intwenty to 1.7.5

*/



alter table security_User add LegalIdNumber nvarchar(300)
GO
alter table security_User add CompanyName nvarchar(300)
GO
alter table security_User add [Address] nvarchar(300)
GO
alter table security_User add ZipCode nvarchar(300)
GO
alter table security_User add City nvarchar(300)
GO
alter table security_User add County nvarchar(300)
GO
alter table security_User add Country nvarchar(300)
GO
alter table security_User add LastLoginMethod nvarchar(300)
GO
alter table security_User add AllowSmsNotifications int
GO
alter table security_User add AllowEmailNotifications int
GO
alter table security_User add AllowPublicProfile int


CREATE TABLE [dbo].[security_UserSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](300) NULL,
	[Code] [nvarchar](300) NULL,
	[Value] [nvarchar](300) NULL,
 CONSTRAINT [PK_security_UserSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
CREATE UNIQUE NONCLUSTERED INDEX [USRSETTING_IDX_1] ON [dbo].[security_UserSetting]
(
	[UserId] ASC,
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
DROP TABLE [dbo].[sysmodel_DataViewItem]
GO
ALTER TABLE [dbo].[sysmodel_FunctionItem] DROP COLUMN [ActionUserInterfaceMetaCode]
GO
ALTER TABLE [dbo].[sysmodel_FunctionItem] ADD 
[ActionMetaCode] [nvarchar] (300) COLLATE Finnish_Swedish_CI_AS NULL,
[ActionMetaType] [nvarchar] (300) COLLATE Finnish_Swedish_CI_AS NULL
GO
ALTER TABLE [dbo].[sysmodel_UserInterfaceStructureItem] DROP COLUMN [DataViewMetaCode],[DataViewColumn1MetaCode],[DataViewColumn2MetaCode]
GO
