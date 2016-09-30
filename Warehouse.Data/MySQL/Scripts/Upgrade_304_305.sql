DROP TABLE IF EXISTS `ecrreceipts`;
CREATE TABLE `ecrreceipts` (
	`ID` INTEGER NOT NULL AUTO_INCREMENT,
	`OperType` INTEGER DEFAULT 0,
	`Acct` INTEGER DEFAULT 0,
	`ReceiptID` INTEGER DEFAULT 0,
	`ReceiptType` INTEGER DEFAULT 0,
	`ECRID` INTEGER DEFAULT 0,
	`UserID` INTEGER DEFAULT 0,
	`Description` VARCHAR(255),
	`UserRealTime` DATETIME,
	PRIMARY KEY (`ID`),
	INDEX (`OperType`),
	INDEX (`Acct`),
	INDEX (`ReceiptID`),
	INDEX (`ReceiptType`),
	INDEX (`ECRID`),
	INDEX (`UserID`)
)  TYPE= InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `transformations`;
CREATE TABLE `transformations` (
	`ID` INTEGER NOT NULL AUTO_INCREMENT, 
	`RootOperType` INTEGER DEFAULT 0, 
	`RootAcct` INTEGER DEFAULT 0, 
	`FromOperType` INTEGER DEFAULT 0, 
	`FromAcct` INTEGER DEFAULT 0, 
	`ToOperType` INTEGER DEFAULT 0, 
	`ToAcct` INTEGER DEFAULT 0, 
	`UserID` INTEGER DEFAULT 0, 
	`UserRealTime` DATETIME, 
	PRIMARY KEY (`ID`), 
	INDEX (`RootOperType`), 
	INDEX (`RootAcct`), 
	INDEX (`FromOperType`), 
	INDEX (`FromAcct`), 
	INDEX (`ToOperType`), 
	INDEX (`ToAcct`), 
	INDEX (`UserID`)
)  TYPE= InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE objects ADD Name2 VARCHAR(255);
ALTER TABLE users ADD Name2 VARCHAR(255);
ALTER TABLE partners ADD Company2 VARCHAR(255);
ALTER TABLE partners ADD MOL2 VARCHAR(255);
ALTER TABLE partners ADD Phone2 VARCHAR(255);
ALTER TABLE partners ADD City2 VARCHAR(255);
ALTER TABLE partners ADD Note1 VARCHAR(255);
ALTER TABLE partners ADD Note2 VARCHAR(255);
ALTER TABLE operationtype ADD GR VARCHAR(255);

UPDATE goods SET Name2 = Name;
UPDATE objects SET Name2 = Name;
UPDATE users SET Name2 = Name;
UPDATE partners SET Company2 = Company;
UPDATE partners SET MOL2 = MOL;
UPDATE partners SET Phone2 = Phone;
UPDATE partners SET Address2 = Address;
UPDATE partners SET City2 = City;
UPDATE partners SET Note1 = ' ';
UPDATE partners SET Note2 = ' ';
UPDATE operationtype SET GR = 'Παράδοση' WHERE ID = 1;
UPDATE operationtype SET GR = 'Πώληση' WHERE ID = 2;
UPDATE operationtype SET GR = 'Σκάρτα' WHERE ID = 3;
UPDATE operationtype SET GR = 'Απογραφή' WHERE ID = 4;
UPDATE operationtype SET GR = 'Παραγωγή' WHERE ID IN (5, 6);
UPDATE operationtype SET GR = 'Μεταφορά' WHERE ID IN (7, 8);
UPDATE operationtype SET GR = 'Εμπορικό σημείο' WHERE ID = 9;
UPDATE operationtype SET GR = 'Εμπορική οθόνη' WHERE ID = 10;
UPDATE operationtype SET GR = 'Εξαγωγή από την αποθήκη' WHERE ID = 11;
UPDATE operationtype SET GR = 'Παραγγελία' WHERE ID = 12;
UPDATE operationtype SET GR = 'Προσφορά' WHERE ID = 13;
UPDATE operationtype SET GR = 'Προτιμολόγιο' WHERE ID = 14;
UPDATE operationtype SET GR = 'Παραγγελία εμπορευμάτων σε παρακαταθήκη' WHERE ID = 15;
UPDATE operationtype SET GR = 'Εμπορεύματα σε παρακαταθήκη' WHERE ID = 16;
UPDATE operationtype SET GR = 'Επιστροφή εμπορευμάτων σε παρακαταθήκη' WHERE ID = 17;
UPDATE operationtype SET GR = 'Παράδοση με παρακαταθήκη' WHERE ID = 18;
UPDATE operationtype SET GR = 'Παραγγελία' WHERE ID = 19;
UPDATE operationtype SET GR = 'Πρώτη ύλη' WHERE ID IN (20, 22);
UPDATE operationtype SET GR = 'Προϊόν' WHERE ID IN (21, 23);
UPDATE operationtype SET GR = 'Σύνθετη παραγωγή' WHERE ID IN (24, 25);
UPDATE operationtype SET GR = 'Χρεωστικό σημείωμα' WHERE ID = 26;
UPDATE operationtype SET GR = 'Πιστωτικό σημείωμα' WHERE ID = 27;
UPDATE operationtype SET GR = 'Κάρτα εγγύησης' WHERE ID = 28;
UPDATE operationtype SET GR = 'Συσκευασία πρώτων υλών' WHERE ID = 29;
UPDATE operationtype SET GR = 'Είδος συσκευασίας' WHERE ID = 30;
UPDATE operationtype SET GR = 'Παράδοση ειδών συσκευασίας' WHERE ID = 31;
UPDATE operationtype SET GR = 'Επιστροφή ειδών συσκευασίας' WHERE ID = 32;
UPDATE operationtype SET GR = 'Παραγγελία – εστιατόριο' WHERE ID = 33;
UPDATE operationtype SET GR = 'Διαμαρτυρία' WHERE ID = 34;
UPDATE operationtype SET GR = 'Διαμαρτυρία προς προμηθευτή' WHERE ID = 35;
UPDATE system SET Version = '3.05';