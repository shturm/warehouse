/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


--
-- Create schema
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `{0}`;
USE `{0}`;

SET CHARACTER SET utf8;
SET NAMES 'utf8';

--
-- Definition of table `applicationlog`
--

DROP TABLE IF EXISTS `applicationlog`;
CREATE TABLE `applicationlog` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Message` VARCHAR(255), 
  `UserID` INTEGER DEFAULT 0, 
  `UserRealTime` DATETIME, 
  `MessageSource` VARCHAR(50), 
  PRIMARY KEY(`ID`), 
  INDEX (`ID`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `cashbook`
--

DROP TABLE IF EXISTS `cashbook`;
CREATE TABLE `cashbook` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Date` DATETIME, 
  `Desc` VARCHAR(255), 
  `OperType` INTEGER DEFAULT 0, 
  `Sign` INTEGER DEFAULT 0, 
  `Profit` DOUBLE NULL DEFAULT 0, 
  `UserID` INTEGER DEFAULT 0, 
  `UserRealtime` DATETIME, 
  `ObjectID` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Date`), 
  INDEX (`OperType`), 
  INDEX (`UserID`), 
  INDEX (`ObjectID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `configuration`
--

DROP TABLE IF EXISTS `configuration`;
CREATE TABLE `configuration` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Key` VARCHAR(50), 
  `Value` VARCHAR(50), 
  `UserID` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Key`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `currencies`
--

DROP TABLE IF EXISTS `currencies`;
CREATE TABLE `currencies` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Currency` VARCHAR(3), 
  `Description` VARCHAR(255), 
  `ExchangeRate` DOUBLE NULL DEFAULT 0, 
  `Deleted` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `currencies`
--

/*!40000 ALTER TABLE `currencies` DISABLE KEYS */;
INSERT INTO `currencies` VALUES (1, 'BGN', 'Български лев', 1, 0);
/*!40000 ALTER TABLE `currencies` ENABLE KEYS */;


--
-- Definition of table `currencieshistory`
--

DROP TABLE IF EXISTS `currencieshistory`;
CREATE TABLE `currencieshistory` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `CurrencyID` INTEGER DEFAULT 0, 
  `ExchangeRate` DOUBLE NULL DEFAULT 0, 
  `Date` DATETIME, 
  `UserID` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`CurrencyID`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `documents`
--

DROP TABLE IF EXISTS `documents`;
CREATE TABLE `documents` (
  `ID` INTEGER AUTO_INCREMENT, 
  `Acct` INTEGER NOT NULL DEFAULT 0, 
  `InvoiceNumber` VARCHAR(255) NOT NULL, 
  `OperType` INTEGER NOT NULL DEFAULT 0, 
  `InvoiceDate` DATETIME, 
  `DocumentType` INTEGER DEFAULT 0, 
  `ExternalInvoiceDate` DATETIME, 
  `ExternalInvoiceNumber` VARCHAR(255), 
  `PaymentType` INTEGER DEFAULT 0, 
  `Recipient` VARCHAR(255), 
  `EGN` VARCHAR(255), 
  `Provider` VARCHAR(255), 
  `TaxDate` DATETIME,
  `Reason` VARCHAR(255),
  `Description` VARCHAR(255),
  `Place` VARCHAR(255),
  PRIMARY KEY (`Acct`, `InvoiceNumber` (190), `OperType`),
  INDEX (`ID`), 
  INDEX (`Acct`), 
  INDEX (`InvoiceNumber` (190)),
  INDEX (`OperType`), 
  INDEX (`Recipient` (190))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `ecrreceipts`
--

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
  `Total` DOUBLE,
  `UserID` INTEGER DEFAULT 0, 
  `UserRealTime` DATETIME, 
  PRIMARY KEY (`ID`), 
  INDEX (`OperType`), 
  INDEX (`Acct`), 
  INDEX (`ReceiptID`), 
  INDEX (`ReceiptType`), 
  INDEX (`ECRID`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `goods`
--

DROP TABLE IF EXISTS `goods`;
CREATE TABLE `goods` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `BarCode1` VARCHAR(255), 
  `BarCode2` VARCHAR(255), 
  `BarCode3` VARCHAR(255), 
  `Catalog1` VARCHAR(255), 
  `Catalog2` VARCHAR(255), 
  `Catalog3` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `Name2` VARCHAR(255), 
  `Measure1` VARCHAR(255), 
  `Measure2` VARCHAR(255), 
  `Ratio` DOUBLE NULL DEFAULT 0, 
  `PriceIn` DOUBLE NULL DEFAULT 0, 
  `PriceOut1` DOUBLE NULL DEFAULT 0, 
  `PriceOut2` DOUBLE NULL DEFAULT 0, 
  `PriceOut3` DOUBLE NULL DEFAULT 0, 
  `PriceOut4` DOUBLE NULL DEFAULT 0, 
  `PriceOut5` DOUBLE NULL DEFAULT 0, 
  `PriceOut6` DOUBLE NULL DEFAULT 0, 
  `PriceOut7` DOUBLE NULL DEFAULT 0, 
  `PriceOut8` DOUBLE NULL DEFAULT 0, 
  `PriceOut9` DOUBLE NULL DEFAULT 0, 
  `PriceOut10` DOUBLE NULL DEFAULT 0, 
  `MinQtty` DOUBLE NULL DEFAULT 0, 
  `NormalQtty` DOUBLE NULL DEFAULT 0, 
  `Description` VARCHAR(255), 
  `Type` INTEGER DEFAULT 0, 
  `IsRecipe` INTEGER DEFAULT 0, 
  `TaxGroup` INTEGER DEFAULT 0, 
  `IsVeryUsed` INTEGER DEFAULT 0, 
  `GroupID` INTEGER DEFAULT 0, 
  `Deleted` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`BarCode1`), 
  INDEX (`BarCode2`), 
  INDEX (`BarCode3`), 
  INDEX (`Catalog1`), 
  INDEX (`Catalog2`), 
  INDEX (`Catalog3`), 
  INDEX (`Name`), 
  INDEX (`Name2`), 
  INDEX (`GroupID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `goods`
--

/*!40000 ALTER TABLE `goods` DISABLE KEYS */;
INSERT INTO `goods` VALUES (1, ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'Служебна стока','Служебна стока', 'бр.', 'бр.', 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ' ', 0, 0, 1, 1, -1, 0);
/*!40000 ALTER TABLE `goods` ENABLE KEYS */;


--
-- Definition of table `goodsgroups`
--

DROP TABLE IF EXISTS `goodsgroups`;
CREATE TABLE `goodsgroups` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Name` VARCHAR(255), 
  `Code` VARCHAR(255), 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `goodsgroups`
--

/*!40000 ALTER TABLE `goodsgroups` DISABLE KEYS */;
INSERT INTO `goodsgroups` VALUES (1, 'Служебна група', '-1');
/*!40000 ALTER TABLE `goodsgroups` ENABLE KEYS */;


--
-- Definition of table `internallog`
--

DROP TABLE IF EXISTS `internallog`;
CREATE TABLE `internallog` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Message` VARCHAR(3000),
  PRIMARY KEY(`ID`), 
  INDEX (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `lots`
--

DROP TABLE IF EXISTS `lots`;
CREATE TABLE `lots` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `SerialNo` VARCHAR(255), 
  `EndDate` DATETIME, 
  `ProductionDate` DATETIME, 
  `Location` VARCHAR(255), 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`),
  INDEX (`SerialNo`),
  INDEX (`EndDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `lots`
--

/*!40000 ALTER TABLE `lots` DISABLE KEYS */;
INSERT INTO `lots` VALUES (1, ' ', NULL, NULL, ' ');
/*!40000 ALTER TABLE `lots` ENABLE KEYS */;


--
-- Definition of table `network`
--

DROP TABLE IF EXISTS `network`;
CREATE TABLE `network` (
  `Num` INTEGER NOT NULL DEFAULT 0, 
  `Counter` INTEGER DEFAULT 0, 
  PRIMARY KEY (`Num`), 
  INDEX (`Num`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `network`
--

/*!40000 ALTER TABLE `network` DISABLE KEYS */;
INSERT INTO `network` VALUES (0, 0);
/*!40000 ALTER TABLE `network` ENABLE KEYS */;


--
-- Definition of table `nextacct`
--

DROP TABLE IF EXISTS `nextacct`;
CREATE TABLE `nextacct` (
  `NextAcct` VARCHAR(50) NOT NULL, 
  PRIMARY KEY (`NextAcct`), 
  INDEX (`NextAcct`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `objects`
--

DROP TABLE IF EXISTS `objects`;
CREATE TABLE `objects` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `Name2` VARCHAR(255),   
  `PriceGroup` INTEGER DEFAULT 0,
  `IsVeryUsed` INTEGER DEFAULT 0, 
  `GroupID` INTEGER DEFAULT 0, 
  `Deleted` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`Name`), 
  INDEX (`Name2`),   
  INDEX (`GroupID`) 
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `objects`
--

/*!40000 ALTER TABLE `objects` DISABLE KEYS */;
INSERT INTO `objects` VALUES (1, ' ', 'Служебен обект', 'Служебен обект', 1, 1, -1, 0);
/*!40000 ALTER TABLE `objects` ENABLE KEYS */;


--
-- Definition of table `objectsgroups`
--

DROP TABLE IF EXISTS `objectsgroups`;
CREATE TABLE `objectsgroups` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `objectsgroups`
--

/*!40000 ALTER TABLE `objectsgroups` DISABLE KEYS */;
INSERT INTO `objectsgroups` VALUES (1, '-1', 'Служебна група');
/*!40000 ALTER TABLE `objectsgroups` ENABLE KEYS */;


--
-- Definition of table `operations`
--

DROP TABLE IF EXISTS `operations`;
CREATE TABLE `operations` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `OperType` INTEGER DEFAULT 0, 
  `Acct` INTEGER DEFAULT 0, 
  `GoodID` INTEGER DEFAULT 0, 
  `PartnerID` INTEGER DEFAULT 0, 
  `ObjectID` INTEGER DEFAULT 0, 
  `OperatorID` INTEGER DEFAULT 0, 
  `Qtty` DOUBLE NULL DEFAULT 0, 
  `Sign` INTEGER DEFAULT 0, 
  `PriceIn` DOUBLE NULL DEFAULT 0, 
  `PriceOut` DOUBLE NULL DEFAULT 0, 
  `VATIn` DOUBLE NULL DEFAULT 0, 
  `VATOut` DOUBLE NULL DEFAULT 0, 
  `Discount` DOUBLE NULL DEFAULT 0, 
  `CurrencyID` INTEGER DEFAULT 0, 
  `CurrencyRate` DOUBLE NULL DEFAULT 0, 
  `Date` DATETIME, 
  `Lot` VARCHAR(50), 
  `LotID`  INTEGER DEFAULT 0, 
  `Note` VARCHAR(255), 
  `SrcDocID` INTEGER DEFAULT 0, 
  `UserID` INTEGER DEFAULT 0, 
  `UserRealTime` DATETIME, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`OperType`), 
  INDEX (`Acct`), 
  INDEX (`OperType`, `Acct`), 
  INDEX (`GoodID`), 
  INDEX (`PartnerID`), 
  INDEX (`ObjectID`), 
  INDEX (`OperatorID`), 
  INDEX (`CurrencyID`), 
  INDEX (`Date`), 
  INDEX (`Lot`), 
  INDEX (`LotID`), 
  INDEX (`SrcDocID`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `operationtype`
--

DROP TABLE IF EXISTS `operationtype`;
CREATE TABLE `operationtype` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `BG` VARCHAR(255), 
  `EN` VARCHAR(255), 
  `DE` VARCHAR(255),
  `RU` VARCHAR(255),
  `TR` VARCHAR(255),
  `SQ` VARCHAR(255),
  `SR` VARCHAR(255),
  `RO` VARCHAR(255),
  `GR` VARCHAR(255),
  PRIMARY KEY (`ID`), 
  INDEX (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `operationtype`
--

/*!40000 ALTER TABLE `operationtype` DISABLE KEYS */;
INSERT INTO `operationtype` VALUES (1, 'Доставка', 'Purchase', 'Lieferung', 'Приход', 'Alışlar', 'Hyrjet', 'Nabavke', 'Livrare','Παράδοση');
INSERT INTO `operationtype` VALUES (2, 'Продажба', 'Sale', 'Verkauf', 'Продажа', 'Satışlar', 'Daljet', 'Prodaje', 'Vănzare','Πώληση');
INSERT INTO `operationtype` VALUES (3, 'Брак', 'Waste', 'Ausschussproduktion', 'Брак', 'Hurda', 'Shpërdorje', 'Trošak', 'Casat','Σκάρτα');
INSERT INTO `operationtype` VALUES (4, 'Ревизия', 'Stock-taking', 'Inventur', 'Переучет', 'Gözden geçirme', 'Regjistrimi i malli', 'Popis', 'Inventar','Απογραφή');
INSERT INTO `operationtype` VALUES (5, 'Производство', 'Production', 'Produce', 'Производство', 'Gözden geçirme', 'Prodhimi', 'Produkcija', 'Producţie','Παραγωγή');
INSERT INTO `operationtype` VALUES (6, 'Производство', 'Production', 'Produce', 'Производство', 'Gözden geçirme', 'Prodhimi', 'Produkcija', 'Producţie','Παραγωγή');
INSERT INTO `operationtype` VALUES (7, 'Трансфер', 'Transfer', 'Transfer', 'Перемещение', 'Gözden geçirme', 'Transfero', 'Prenos', 'Transfer','Μεταφορά');
INSERT INTO `operationtype` VALUES (8, 'Трансфер', 'Transfer', 'Transfer', 'Перемещение', 'Gözden geçirme', 'Transfero', 'Prenos', 'Transfer','Μεταφορά');
INSERT INTO `operationtype` VALUES (9, 'Търговски обект', 'Point of Sale', 'Handelsobjekt', 'Торговый объект', 'İş yeri', 'Trade Object', 'Trgovački objekat', 'Obiect comercial','Εμπορικό σημείο');
INSERT INTO `operationtype` VALUES (10, 'Търговски екран', 'Touch Screen', 'Tastschirm', 'Торговый экран', 'Mağaza ekranı', 'Touch Screen', 'Reagovanje ekrana na dodir', 'Ecran comercial','Εμπορική οθόνη');
INSERT INTO `operationtype` VALUES (11, 'Изписване', 'Write-off', 'Abschreibung', 'Списание', 'Depodan çıkarma', 'Tërheqje interne', 'Zapis', 'Scoatere din gestiune','Εξαγωγή από την αποθήκη');
INSERT INTO `operationtype` VALUES (12, 'Заявка', 'Request', 'Bestellung', 'Заказ поставщику', 'Mal istekleri', 'Kërkesat', 'Zahtevi', 'Comandă','Παραγγελία');
INSERT INTO `operationtype` VALUES (13, 'Оферта', 'Offer', 'Angebot', 'Счёт на оплату', 'Teklifler', 'Oferta', 'Ponude', 'Oferta','Προσφορά');
INSERT INTO `operationtype` VALUES (14, 'Проформа', 'Proform Invoice', 'Proforma-Rechnung', 'Проформа', 'Farura öncesi belge', 'Pro-Faturë', 'Profaktura', 'Proforma','Προτιμολόγιο');
INSERT INTO `operationtype` VALUES (15, 'Даване на консигнация', 'Consign', 'Ware in konsignation geben', 'Передать на реализацию', 'Vadeli mal verme', 'Artikujt në konsignacion', 'Davanje konsignacione robe', 'Dare spre consignaţie','Παραγγελία εμπορευμάτων σε παρακαταθήκη');
INSERT INTO `operationtype` VALUES (16, 'Отчитане на консигнация', 'Sales on consignment', 'Abrechnung der Konsignation',  'Отчёт по реализации', 'Vadeli mal hesap tutma', 'Daljet me konsignacion', 'Konsignaciona prodaja', 'Consignaţie înregistrată','Εμπορεύματα σε παρακαταθήκη');
INSERT INTO `operationtype` VALUES (17, 'Връщане на консигнация', 'Return consignment', 'Rückgabe in Konsignation', 'Возврат от реализации', 'Vadeli mal geri alma', 'Kthimi me konsignacion', 'Vraćanje konsignacione robe', 'Retur de consignaţie','Επιστροφή εμπορευμάτων σε παρακαταθήκη');
INSERT INTO `operationtype` VALUES (18, 'Доставка на консигнация', 'Purchase on consignment', 'Konsignation nehmend', 'Взять на консигнацию', 'Vadeli mal alma', 'Hyrje me konsignacion', 'Prijem poslate robe', 'Livrare în consignaţie','Παράδοση με παρακαταθήκη');
INSERT INTO `operationtype` VALUES (19, 'Поръчка', 'Order', 'Auftrag', 'Клиентский заказ', 'Siparişler', 'Porositë', 'Narudžbine', 'Cerere de oferta','Παραγγελία');
INSERT INTO `operationtype` VALUES (20, 'Суровина', 'Raw material', 'Rohstoff', 'Сырье', 'Malzeme', 'Lëndë e parë', 'Sirovi materijal', 'Materie primă','Πρώτη ύλη');
INSERT INTO `operationtype` VALUES (21, 'Продукт', 'Product', 'Produkt', 'Продукт', 'Ürün', 'Prodhim', 'Produkcija', 'Produse','Προϊόν');
INSERT INTO `operationtype` VALUES (22, 'Суровина', 'Raw material', 'Rohstoff', 'Сырье','Malzeme', 'Lëndë e parë', 'Sirovi materijal', 'Materie primă','Πρώτη ύλη');
INSERT INTO `operationtype` VALUES (23, 'Продукт', 'Product', 'Produkt', 'Продукт', 'Ürün', 'Prodhim', 'Produkcija', 'Produse','Προϊόν');
INSERT INTO `operationtype` VALUES (24, 'Сложно производство', 'Complex Production', 'Komplizierte Produktion', 'Сложное производство', 'Karışık üretim', 'Prodhimet Komplekse', 'Složena produkcija', 'Producţie complexă','Σύνθετη παραγωγή');
INSERT INTO `operationtype` VALUES (25, 'Сложно производство', 'Complex Production', 'Komplizierte Produktion', 'Сложное производство', 'Karışık üretim', 'Prodhimet Komplekse', 'Složena produkcija', 'Producţie complexă','Σύνθετη παραγωγή');
INSERT INTO `operationtype` VALUES (26, 'Дебитно известие', 'Debit Note', 'Mitteilung Debit', 'Дебетное извещение', 'Borç dekontu', 'Shënimet mbi debin', 'Izveštaj o zaduženjima', 'Notă de debit','Χρεωστικό σημείωμα');
INSERT INTO `operationtype` VALUES (27, 'Кредитно известие', 'Credit Note', 'Mitteilung Kredit', 'Кредитное извещение', 'Kredi dekontu', 'Shënimet mbi kredin', 'Izveštaj o odobrenjima', 'Notă de credit','Πιστωτικό σημείωμα');
INSERT INTO `operationtype` VALUES (28, 'Гаранционна карта', 'Warranty Card', 'Garantieschein', 'Гаранционная карта', 'Garanti belgeleri', 'Fletëgarancionet', 'Garantni list', 'Certificat de garanţie','Κάρτα εγγύησης');
INSERT INTO `operationtype` VALUES (29, 'Амбалажна суровина', 'Packing raw material', 'Verpackungsmaterial', 'Амбалажное сырье', 'Ambalaj ham maddesi', 'Lënda e parë e ambalazhit', 'NOT TRANSLATED', 'NOT TRANSLATED','Συσκευασία πρώτων υλών');
INSERT INTO `operationtype` VALUES (30, 'Амбалажен продукт', 'Packing product', 'Verpackungsprodukt', 'Амбалажны продукт', 'Ambalaj hazır ürünü', 'Ambalazh produkti', 'NOT TRANSLATED', 'NOT TRANSLATED','Είδος συσκευασίας');
INSERT INTO `operationtype` VALUES (31, 'Даване на амбалаж', 'Packing give', 'Verpackungs geben', 'Амбалаж отдача', 'Ambalaj verme', 'Dhënie e ambalazhit', 'NOT TRANSLATED', 'NOT TRANSLATED','Παράδοση ειδών συσκευασίας');
INSERT INTO `operationtype` VALUES (32, 'Връщане на амбалаж', 'Packing return', 'Rückgabe der Verpackungs', 'Амбалаж возврат', 'Ambalaj geri alma', 'Kthim i ambalazhit', 'NOT TRANSLATED', 'NOT TRANSLATED','Επιστροφή ειδών συσκευασίας');
INSERT INTO `operationtype` VALUES (33, 'Поръчка - ресторант', 'Order - restaurant', 'Bestellungen - Restaurant', 'Заказ - ресторан', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'Comenzi-restaurant','Παραγγελία – εστιατόριο');
INSERT INTO `operationtype` VALUES (34, 'Рекламация', 'Refund', 'Rücküberweisung', 'Возврат', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'Reclamaţie','Διαμαρτυρία');
INSERT INTO `operationtype` VALUES (35, 'Резервация', 'Booking', 'Buchungen', 'Бронирование', 'Rezervasyon', 'Rezervim', 'Rezervacija', 'Rezervare', 'Κράτηση');
INSERT INTO `operationtype` VALUES (36, 'Авансово плащане', 'Advance payment', 'Vorauszahlung', 'Предоплата', 'Avans ödemesi', 'NOT TRANSLATED', 'NOT TRANSLATED', 'Plata în avans', 'Προκαταβολή');
INSERT INTO `operationtype` VALUES (37, 'Дебитно известие от доставчик', 'Debit Note by supplier', 'NOT TRANSLATED', 'Дебетное извещение от поставщика', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');
INSERT INTO `operationtype` VALUES (38, 'Кредитно известие от доставчик', 'Credit Note by supplier', 'NOT TRANSLATED', 'Кредитное извещение от поставщика', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');
INSERT INTO `operationtype` VALUES (39, 'Рекламация към доставчик', 'Refund to supplier', 'NOT TRANSLATED', 'Возврат поставщику', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED','Διαμαρτυρία προς προμηθευτή');
/*!40000 ALTER TABLE `operationtype` ENABLE KEYS */;


--
-- Definition of table `partners`
--

DROP TABLE IF EXISTS `partners`;
CREATE TABLE `partners` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Company` VARCHAR(255), 
  `Company2` VARCHAR(255),   
  `MOL` VARCHAR(255), 
  `MOL2` VARCHAR(255),   
  `City` VARCHAR(255), 
  `City2` VARCHAR(255), 
  `Address` VARCHAR(255), 
  `Address2` VARCHAR(255), 
  `Phone` VARCHAR(255), 
  `Phone2` VARCHAR(255), 
  `Fax` VARCHAR(255), 
  `eMail` VARCHAR(255), 
  `TaxNo` VARCHAR(255), 
  `Bulstat` VARCHAR(255), 
  `BankName` VARCHAR(255), 
  `BankCode` VARCHAR(255), 
  `BankAcct` VARCHAR(255), 
  `BankVATName` VARCHAR(255), 
  `BankVATCode` VARCHAR(255), 
  `BankVATAcct` VARCHAR(255), 
  `PriceGroup` INTEGER DEFAULT 1, 
  `Discount` DOUBLE NULL DEFAULT 0, 
  `Type` INTEGER DEFAULT 0, 
  `IsVeryUsed` INTEGER, 
  `UserID` INTEGER, 
  `GroupID` INTEGER DEFAULT 0, 
  `UserRealTime` DATETIME, 
  `Deleted` INTEGER DEFAULT 0,
  `CardNumber` VARCHAR(255), 
  `Note1` VARCHAR(255), 
  `Note2` VARCHAR(255), 
  `PaymentDays` INTEGER DEFAULT 0,
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`Company`), 
  INDEX (`MOL`), 
  INDEX (`TaxNo`), 
  INDEX (`Bulstat`), 
  INDEX (`BankCode`), 
  INDEX (`BankVATCode`), 
  INDEX (`UserID`), 
  INDEX (`GroupID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `partners`
--

/*!40000 ALTER TABLE `partners` DISABLE KEYS */;
INSERT INTO `partners` VALUES (1, ' ', 'Служебен партньор', 'Служебен партньор', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 1, 0, 0, -2, 1, -1, NULL, 0, ' ', ' ', ' ', 0);
/*!40000 ALTER TABLE `partners` ENABLE KEYS */;


--
-- Definition of table `partnersgroups`
--

DROP TABLE IF EXISTS `partnersgroups`;
CREATE TABLE `partnersgroups` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `partnersgroups`
--

/*!40000 ALTER TABLE `partnersgroups` DISABLE KEYS */;
INSERT INTO `partnersgroups` VALUES (1, '-1', 'Служебна група');
/*!40000 ALTER TABLE `partnersgroups` ENABLE KEYS */;


--
-- Definition of table `payments`
--

DROP TABLE IF EXISTS `payments`;
CREATE TABLE `payments` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Acct` DOUBLE NULL DEFAULT 0, 
  `OperType` INTEGER DEFAULT 0, 
  `PartnerID` INTEGER DEFAULT 0, 
  `ObjectID` INTEGER DEFAULT 0, 
  `Qtty` DOUBLE NULL DEFAULT 0, 
  `Mode` INTEGER DEFAULT 0, 
  `Sign` INTEGER DEFAULT 0, 
  `Date` DATETIME, 
  `UserID` INTEGER DEFAULT 0, 
  `UserRealTime` DATETIME, 
  `Type` INTEGER DEFAULT 0, 
  `TransactionNumber` VARCHAR(255),
  `EndDate` DATETIME,
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Acct`), 
  INDEX (`OperType`), 
  INDEX (`PartnerID`), 
  INDEX (`ObjectID`), 
  INDEX (`Date`), 
  INDEX (`UserID`), 
  INDEX (`EndDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `pricerules`
--

DROP TABLE IF EXISTS `pricerules`;
CREATE TABLE `pricerules` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Name` VARCHAR(255),
  `Formula` VARCHAR(1000),
  `Enabled` INTEGER DEFAULT 0,
  `Priority` INTEGER DEFAULT 0,
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Name`),   
  INDEX (`Formula`)   
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `registration`
--

DROP TABLE IF EXISTS `registration`;
CREATE TABLE `registration` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Company` VARCHAR(255), 
  `MOL` VARCHAR(255), 
  `City` VARCHAR(255), 
  `Address` VARCHAR(255), 
  `Phone` VARCHAR(255), 
  `Fax` VARCHAR(255), 
  `eMail` VARCHAR(255), 
  `TaxNo` VARCHAR(255), 
  `Bulstat` VARCHAR(255), 
  `BankName` VARCHAR(255), 
  `BankCode` VARCHAR(255), 
  `BankAcct` VARCHAR(255), 
  `BankVATAcct` VARCHAR(255), 
  `UserID` INTEGER, 
  `UserRealTime` DATETIME, 
  `IsDefault` INTEGER DEFAULT 0,
  `Note1` VARCHAR(255), 
  `Note2` VARCHAR(255),
  `Deleted` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`Company`), 
  INDEX (`MOL`), 
  INDEX (`TaxNo`), 
  INDEX (`Bulstat`), 
  INDEX (`BankCode`), 
  INDEX (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `registration`
--

/*!40000 ALTER TABLE `registration` DISABLE KEYS */;
INSERT INTO `registration` VALUES (1, ' ', 'Служебна фирма', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 1, NULL, -1, ' ', ' ', 0);
/*!40000 ALTER TABLE `registration` ENABLE KEYS */;

--
-- Definition of table `store`
--

DROP TABLE IF EXISTS `store`;
CREATE TABLE `store` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `ObjectID` INTEGER DEFAULT 0, 
  `GoodID` INTEGER DEFAULT 0, 
  `Qtty` DOUBLE NULL DEFAULT 0, 
  `Price` DOUBLE NULL DEFAULT 0, 
  `Lot` VARCHAR(250), 
  `LotID`  INTEGER DEFAULT 0, 
  `LotOrder`  INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`GoodID`), 
  INDEX (`ObjectID`), 
  INDEX (`Lot`), 
  INDEX (`LotID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `store`
--

/*!40000 ALTER TABLE `store` DISABLE KEYS */;
INSERT INTO `store` VALUES (1, 1, 1, 0, 0, ' ', 1, 1);
/*!40000 ALTER TABLE `store` ENABLE KEYS */;

--
-- Definition of table `system`
--

DROP TABLE IF EXISTS `system`;
CREATE TABLE `system` (
  `Version` VARCHAR(20) NOT NULL, 
  `ProductID` INTEGER DEFAULT 0, 
  `LastBackup` DATETIME, 
  PRIMARY KEY (`Version`), 
  INDEX (`Version`), 
  INDEX (`ProductID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `system`
--

/*!40000 ALTER TABLE `system` DISABLE KEYS */;
INSERT INTO `system` VALUES ('3.07', 1, NULL);
/*!40000 ALTER TABLE `system` ENABLE KEYS */;


--
-- Definition of table `transformations`
--

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `users`
--

DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `Name2` VARCHAR(255), 
  `IsVeryUsed` INTEGER DEFAULT 0, 
  `GroupID` INTEGER DEFAULT 0, 
  `Password` VARCHAR(50), 
  `UserLevel` INTEGER DEFAULT 0, 
  `Deleted` INTEGER DEFAULT 0, 
  `CardNumber` VARCHAR(255),
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`Name`), 
  INDEX (`Name2`), 
  INDEX (`GroupID`) 
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `users`
--

/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1, ' ', 'Служебен потребител', 'Служебен потребител', 1, -1, 'YsAB16V90Bs=', 3, 0, ' ');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;


--
-- Definition of table `usersgroups`
--

DROP TABLE IF EXISTS `usersgroups`;
CREATE TABLE `usersgroups` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255),
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`)  
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `usersgroups`
--

/*!40000 ALTER TABLE `usersgroups` DISABLE KEYS */;
INSERT INTO `usersgroups` VALUES (1, '-1', 'Служебна група');
/*!40000 ALTER TABLE `usersgroups` ENABLE KEYS */;


--
-- Definition of table `userssecurity`
--

DROP TABLE IF EXISTS `userssecurity`;
CREATE TABLE `userssecurity` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `UserID` INTEGER DEFAULT 0, 
  `ControlName` VARCHAR(100), 
  `State` INTEGER DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`UserID`), 
  INDEX (`ControlName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Definition of table `vatgroups`
--

DROP TABLE IF EXISTS `vatgroups`;
CREATE TABLE `vatgroups` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `VATValue` DOUBLE NULL DEFAULT 0, 
  PRIMARY KEY (`ID`), 
  INDEX (`ID`), 
  INDEX (`Code`), 
  INDEX (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `vatgroups`
--

/*!40000 ALTER TABLE `vatgroups` DISABLE KEYS */;
INSERT INTO `vatgroups` VALUES (1, '1', 'Основна ДДС група', 20);
/*!40000 ALTER TABLE `vatgroups` ENABLE KEYS */;


--
-- Definition of table `paymenttypes`
--

DROP TABLE IF EXISTS `paymenttypes`;
CREATE TABLE `paymenttypes` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255),
  `PaymentMethod` INTEGER DEFAULT 0,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Dumping data for table `paymenttypes`
--

/*!40000 ALTER TABLE `paymenttypes` DISABLE KEYS */;
INSERT INTO `paymenttypes` VALUES (1, 'Плащане в брой', 1);
INSERT INTO `paymenttypes` VALUES (2, 'Превод по сметка', 2);
INSERT INTO `paymenttypes` VALUES (3, 'Дебитна/Кредитна карта', 3);
INSERT INTO `paymenttypes` VALUES (4, 'Плащане чрез ваучер', 4);
/*!40000 ALTER TABLE `paymenttypes` ENABLE KEYS */;



/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;