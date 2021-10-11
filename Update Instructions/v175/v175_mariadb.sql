ALTER TABLE `security_User` ADD COLUMN `LegalIdNumber` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `CompanyName` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `Address` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `ZipCode` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `City` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `County` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `Country` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `LastLoginMethod` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `AllowSmsNotifications` tinyint(1) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `AllowEmailNotifications` tinyint(1) NULL DEFAULT NULL;
ALTER TABLE `security_User` ADD COLUMN `AllowPublicProfile` tinyint(1) NULL DEFAULT NULL;


CREATE TABLE IF NOT EXISTS `security_UserSetting` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(300) DEFAULT NULL,
  `Code` varchar(300) DEFAULT NULL,
  `Value` varchar(300) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `USRSETTING_IDX_1` (`UserId`,`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=UTF8;

DROP TABLE `sysmodel_DataViewItem`;

ALTER TABLE `sysmodel_FunctionItem` DROP COLUMN `ActionUserInterfaceMetaCode`;

ALTER TABLE `sysmodel_FunctionItem` ADD COLUMN `ActionMetaCode` VARCHAR(300) NULL DEFAULT NULL;

ALTER TABLE `sysmodel_FunctionItem` ADD COLUMN `ActionMetaType` VARCHAR(300) NULL DEFAULT NULL;

ALTER TABLE `sysmodel_UserInterfaceStructureItem` DROP COLUMN `DataViewMetaCode`;
ALTER TABLE `sysmodel_UserInterfaceStructureItem` DROP COLUMN `DataViewColumn1MetaCode`;
ALTER TABLE `sysmodel_UserInterfaceStructureItem` DROP COLUMN `DataViewColumn2MetaCode`;


ALTER TABLE `sysmodel_ViewItem` ADD COLUMN `FilePath` VARCHAR(300) NULL DEFAULT NULL;

