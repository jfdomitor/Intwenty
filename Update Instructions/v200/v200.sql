alter table sysdata_EventLog add ProductID nvarchar(300)
GO
alter table sysdata_EventLog add ProductTitle nvarchar(300)
GO
UPDATE sysdata_EventLog SET ProductID='',ProductTitle='' WHERE ProductID IS NULL