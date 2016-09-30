DROP TABLE IF EXISTS `ecrreceipts`;
CREATE TABLE `ecrreceipts` (
	`ID` INTEGER NOT NULL AUTO_INCREMENT,
	`OperType` INTEGER DEFAULT 0,
	`Acct` INTEGER DEFAULT 0,
	`ReceiptID` INTEGER DEFAULT 0,
	`ReceiptDate` DATETIME,
	`ReceiptType` INTEGER DEFAULT 0,
	`ECRID` VARCHAR(255),
	`Description` VARCHAR(255),
	`Total` DOUBLE DEFAULT 0,
	`UserID` INTEGER DEFAULT 0,
	`UserRealTime` DATETIME,
	PRIMARY KEY (`ID`),
	INDEX (`OperType`),
	INDEX (`Acct`),
	INDEX (`ReceiptID`),
	INDEX (`ReceiptType`),
	INDEX (`ECRID`),
	INDEX (`UserID`)
) TYPE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE registration ADD Note1 VARCHAR(255);
ALTER TABLE registration ADD Note2 VARCHAR(255);
UPDATE registration SET Note1 = ' ';
UPDATE registration SET Note2 = ' ';
UPDATE `system` SET Version = '3.06';

