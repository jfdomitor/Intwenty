ALTER TABLE `security_user` ADD COLUMN `LegalIdNumber` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `CompanyName` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `Address` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `ZipCode` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `City` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `County` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `Country` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `LastLoginMethod` VARCHAR(300) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `AllowSmsNotifications` tinyint(1) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `AllowEmailNotifications` tinyint(1) NULL DEFAULT NULL;
ALTER TABLE `security_user` ADD COLUMN `AllowPublicProfile` tinyint(1) NULL DEFAULT NULL;


CREATE TABLE IF NOT EXISTS `security_usersetting` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(300) DEFAULT NULL,
  `Code` varchar(300) DEFAULT NULL,
  `Value` varchar(300) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `USRSETTING_IDX_1` (`UserId`,`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE `sysmodel_DataViewItem`;

ALTER TABLE `sysmodel_functionitem` DROP COLUMN `ActionUserInterfaceMetaCode`;

ALTER TABLE `sysmodel_functionitem` ADD COLUMN `ActionMetaCode` VARCHAR(300) NULL DEFAULT NULL;

ALTER TABLE `sysmodel_functionitem` ADD COLUMN `ActionMetaType` VARCHAR(300) NULL DEFAULT NULL;

ALTER TABLE `sysmodel_userinterfacestructureitem` DROP COLUMN `DataViewMetaCode`,`DataViewColumn1MetaCode`,`DataViewColumn2MetaCode`;

ALTER TABLE `sysmodel_viewitem` ADD COLUMN `FilePath` VARCHAR(300) NULL DEFAULT NULL;

