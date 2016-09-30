DROP TABLE IF EXISTS `applicationlog`;
CREATE TABLE `applicationlog` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Message` VARCHAR(255), 
  `UserID` INTEGER NOT NULL, 
  `UserRealTime` DATETIME, 
  `MessageSource` VARCHAR(50)
);
CREATE INDEX `applicationlog_ID_idx` ON `applicationlog` (`ID`);
CREATE INDEX `applicationlog_UserID_idx` ON `applicationlog` (`UserID`);


DROP TABLE IF EXISTS `cashbook`;
CREATE TABLE `cashbook` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Date` DATETIME, 
  `Desc` VARCHAR(255), 
  `OperType` INTEGER, 
  `Sign` INTEGER, 
  `Profit` DOUBLE NULL, 
  `UserID` INTEGER, 
  `UserRealtime` DATETIME, 
  `ObjectID` INTEGER
);
CREATE INDEX `cashbook_ID_idx` ON `cashbook` (`ID`);
CREATE INDEX `cashbook_Date_idx` ON `cashbook` (`Date`);
CREATE INDEX `cashbook_OperType_idx` ON `cashbook` (`OperType`);
CREATE INDEX `cashbook_UserID_idx` ON `cashbook` (`UserID`);
CREATE INDEX `cashbook_ObjectID_idx` ON `cashbook` (`ObjectID`);


DROP TABLE IF EXISTS `configuration`;
CREATE TABLE `configuration` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Key` VARCHAR(50), 
  `Value` VARCHAR(50), 
  `UserID` INTEGER
);
CREATE INDEX `configuration_ID_idx` ON `configuration` (`ID`);
CREATE INDEX `configuration_Key_idx` ON `configuration` (`Key`);
CREATE INDEX `configuration_UserID_idx` ON `configuration` (`UserID`);


DROP TABLE IF EXISTS `currencies`;
CREATE TABLE `currencies` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Currency` VARCHAR(3), 
  `Description` VARCHAR(255), 
  `ExchangeRate` DOUBLE NULL, 
  `Deleted` INTEGER
);
CREATE INDEX `currencies_ID_idx` ON `currencies` (`ID`);

INSERT INTO `currencies` VALUES (1, 'BGN', 'Български лев', 1, 0);


DROP TABLE IF EXISTS `currencieshistory`;
CREATE TABLE `currencieshistory` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `CurrencyID` INTEGER, 
  `ExchangeRate` DOUBLE NULL, 
  `Date` DATETIME, 
  `UserID` INTEGER
);
CREATE INDEX `currencieshistory_ID_idx` ON `currencieshistory` (`ID`);
CREATE INDEX `currencieshistory_CurrencyID_idx` ON `currencieshistory` (`CurrencyID`);
CREATE INDEX `currencieshistory_UserID_idx` ON `currencieshistory` (`UserID`);


DROP TABLE IF EXISTS `documents`;
CREATE TABLE `documents` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Acct` INTEGER NOT NULL, 
  `InvoiceNumber` VARCHAR(255) NOT NULL, 
  `OperType` INTEGER NOT NULL, 
  `InvoiceDate` DATETIME, 
  `DocumentType` INTEGER, 
  `ExternalInvoiceDate` DATETIME, 
  `ExternalInvoiceNumber` VARCHAR(255), 
  `PaymentType` INTEGER, 
  `Recipient` VARCHAR(255), 
  `EGN` VARCHAR(255), 
  `Provider` VARCHAR(255), 
  `TaxDate` DATETIME,
  `Reason` VARCHAR(255),
  `Description` VARCHAR(255),
  `Place` VARCHAR(255),
  UNIQUE (`Acct`, `InvoiceNumber`, `OperType`) ON CONFLICT REPLACE
);
CREATE INDEX `documents_ID_idx` ON `documents` (`ID`);
CREATE INDEX `documents_Acct_idx` ON `documents` (`Acct`);
CREATE INDEX `documents_InvoiceNumber_idx` ON `documents` (`InvoiceNumber`);
CREATE INDEX `documents_OperType_idx` ON `documents` (`OperType`);
CREATE INDEX `documents_Recipient_idx` ON `documents` (`Recipient`);


DROP TABLE IF EXISTS `ecrreceipts`;
CREATE TABLE `ecrreceipts` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `OperType` INTEGER, 
  `Acct` INTEGER, 
  `ReceiptID` INTEGER,
  `ReceiptDate` DATETIME, 
  `ReceiptType` INTEGER, 
  `ECRID` VARCHAR(255), 
  `Description` VARCHAR(255),
  `Total` DOUBLE,
  `UserID` INTEGER, 
  `UserRealTime` DATETIME
);
CREATE INDEX `ecrreceipts_OperType_idx` ON `ecrreceipts` (`OperType`);
CREATE INDEX `ecrreceipts_Acct_idx` ON `ecrreceipts` (`Acct`);
CREATE INDEX `ecrreceipts_ReceiptID_idx` ON `ecrreceipts` (`ReceiptID`);
CREATE INDEX `ecrreceipts_ReceiptType_idx` ON `ecrreceipts` (`ReceiptType`);
CREATE INDEX `ecrreceipts_ECRID_idx` ON `ecrreceipts` (`ECRID`);
CREATE INDEX `ecrreceipts_UserID_idx` ON `ecrreceipts` (`UserID`);


DROP TABLE IF EXISTS `goods`;
CREATE TABLE `goods` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
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
  `Ratio` DOUBLE NULL, 
  `PriceIn` DOUBLE NULL, 
  `PriceOut1` DOUBLE NULL, 
  `PriceOut2` DOUBLE NULL, 
  `PriceOut3` DOUBLE NULL, 
  `PriceOut4` DOUBLE NULL, 
  `PriceOut5` DOUBLE NULL, 
  `PriceOut6` DOUBLE NULL, 
  `PriceOut7` DOUBLE NULL, 
  `PriceOut8` DOUBLE NULL, 
  `PriceOut9` DOUBLE NULL, 
  `PriceOut10` DOUBLE NULL, 
  `MinQtty` DOUBLE NULL, 
  `NormalQtty` DOUBLE NULL, 
  `Description` VARCHAR(255), 
  `Type` INTEGER, 
  `IsRecipe` INTEGER, 
  `TaxGroup` INTEGER, 
  `IsVeryUsed` INTEGER, 
  `GroupID` INTEGER, 
  `Deleted` INTEGER
);
CREATE INDEX `goods_ID_idx` ON `goods` (`ID`);
CREATE INDEX `goods_Code_idx` ON `goods` (`Code`);
CREATE INDEX `goods_BarCode1_idx` ON `goods` (`BarCode1`);
CREATE INDEX `goods_BarCode2_idx` ON `goods` (`BarCode2`);
CREATE INDEX `goods_BarCode3_idx` ON `goods` (`BarCode3`);
CREATE INDEX `goods_Catalog1_idx` ON `goods` (`Catalog1`);
CREATE INDEX `goods_Catalog2_idx` ON `goods` (`Catalog2`);
CREATE INDEX `goods_Catalog3_idx` ON `goods` (`Catalog3`);
CREATE INDEX `goods_Name_idx` ON `goods` (`Name`);
CREATE INDEX `goods_Name2_idx` ON `goods` (`Name2`);
CREATE INDEX `goods_GroupID_idx` ON `goods` (`GroupID`);

INSERT INTO `goods` VALUES (1, ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'Служебна стока','Служебна стока', 'бр.', 'бр.', 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ' ', 0, 0, 1, 1, -1, 0);


DROP TABLE IF EXISTS `goodsgroups`;
CREATE TABLE `goodsgroups` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Name` VARCHAR(255), 
  `Code` VARCHAR(255)
);
CREATE INDEX `goodsgroups_ID_idx` ON `goodsgroups` (`ID`);
CREATE INDEX `goodsgroups_Code_idx` ON `goodsgroups` (`Code`);

INSERT INTO `goodsgroups` VALUES (1, 'Служебна група', '-1');

DROP TABLE IF EXISTS `internallog`;
CREATE TABLE `internallog` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Message` VARCHAR(3000)
);
CREATE INDEX `internallog_ID_idx` ON `internallog` (`ID`);


DROP TABLE IF EXISTS `lots`;
CREATE TABLE `lots` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `SerialNo` VARCHAR(255), 
  `EndDate` DATETIME, 
  `ProductionDate` DATETIME, 
  `Location` VARCHAR(255)
);
CREATE INDEX `lots_ID_idx` ON `lots` (`ID`);
CREATE INDEX `lots_SerialNo_idx` ON `lots` (`SerialNo`);
CREATE INDEX `lots_EndDate_idx` ON `lots` (`EndDate`);

INSERT INTO `lots` VALUES (1, ' ', NULL, NULL, ' ');


DROP TABLE IF EXISTS `network`;
CREATE TABLE `network` (
  `Num` INTEGER NOT NULL, 
  `Counter` INTEGER, 
  PRIMARY KEY (`Num`)
);
CREATE INDEX `network_Num_idx` ON `network` (`Num`);

INSERT INTO `network` VALUES (0, 0);

DROP TABLE IF EXISTS `nextacct`;
CREATE TABLE `nextacct` (
  `NextAcct` VARCHAR(50) NOT NULL, 
  PRIMARY KEY (`NextAcct`)
);
CREATE INDEX `nextacct_NextAcct_idx` ON `nextacct` (`NextAcct`);


DROP TABLE IF EXISTS `objects`;
CREATE TABLE `objects` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `Name2` VARCHAR(255),   
  `PriceGroup` INTEGER,
  `IsVeryUsed` INTEGER, 
  `GroupID` INTEGER, 
  `Deleted` INTEGER
);
CREATE INDEX `objects_ID_idx` ON `objects` (`ID`);
CREATE INDEX `objects_Code_idx` ON `objects` (`Code`);
CREATE INDEX `objects_Name_idx` ON `objects` (`Name`);
CREATE INDEX `objects_Name2_idx` ON `objects` (`Name2`);
CREATE INDEX `objects_GroupID_idx` ON `objects` (`GroupID`);

INSERT INTO `objects` VALUES (1, ' ', 'Служебен обект', 'Служебен обект', 1, 1, -1, 0);


DROP TABLE IF EXISTS `objectsgroups`;
CREATE TABLE `objectsgroups` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255)
);
CREATE INDEX `objectsgroups_ID_idx` ON `objectsgroups` (`ID`);
CREATE INDEX `objectsgroups_Code_idx` ON `objectsgroups` (`Code`);

INSERT INTO `objectsgroups` VALUES (1, '-1', 'Служебна група');


DROP TABLE IF EXISTS `operations`;
CREATE TABLE `operations` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `OperType` INTEGER, 
  `Acct` INTEGER, 
  `GoodID` INTEGER, 
  `PartnerID` INTEGER, 
  `ObjectID` INTEGER, 
  `OperatorID` INTEGER, 
  `Qtty` DOUBLE NULL, 
  `Sign` INTEGER, 
  `PriceIn` DOUBLE NULL, 
  `PriceOut` DOUBLE NULL, 
  `VATIn` DOUBLE NULL, 
  `VATOut` DOUBLE NULL, 
  `Discount` DOUBLE NULL, 
  `CurrencyID` INTEGER, 
  `CurrencyRate` DOUBLE NULL, 
  `Date` DATETIME, 
  `Lot` VARCHAR(50), 
  `LotID`  INTEGER, 
  `Note` VARCHAR(255), 
  `SrcDocID` INTEGER, 
  `UserID` INTEGER, 
  `UserRealTime` DATETIME
);
CREATE INDEX `operations_ID_idx` ON `operations` (`ID`);
CREATE INDEX `operations_OperType_idx` ON `operations` (`OperType`);
CREATE INDEX `operations_Acct_idx` ON `operations` (`Acct`);
CREATE INDEX `operations_OperTypeAcct_idx` ON `operations` (`OperType`, `Acct`);
CREATE INDEX `operations_GoodID_idx` ON `operations` (`GoodID`);
CREATE INDEX `operations_PartnerID_idx` ON `operations` (`PartnerID`);
CREATE INDEX `operations_ObjectID_idx` ON `operations` (`ObjectID`);
CREATE INDEX `operations_OperatorID_idx` ON `operations` (`OperatorID`);
CREATE INDEX `operations_CurrencyID_idx` ON `operations` (`CurrencyID`);
CREATE INDEX `operations_Date_idx` ON `operations` (`Date`);
CREATE INDEX `operations_Lot_idx` ON `operations` (`Lot`);
CREATE INDEX `operations_LotID_idx` ON `operations` (`LotID`);
CREATE INDEX `operations_SrcDocID_idx` ON `operations` (`SrcDocID`);
CREATE INDEX `operations_UserID_idx` ON `operations` (`UserID`);


DROP TABLE IF EXISTS `operationtype`;
CREATE TABLE `operationtype` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `BG` VARCHAR(255), 
  `EN` VARCHAR(255), 
  `DE` VARCHAR(255),
  `RU` VARCHAR(255),
  `TR` VARCHAR(255),
  `SQ` VARCHAR(255),
  `SR` VARCHAR(255),
  `RO` VARCHAR(255),
  `GR` VARCHAR(255)
);
CREATE INDEX `operationtype_ID_idx` ON `operations` (`ID`);

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


DROP TABLE IF EXISTS `partners`;
CREATE TABLE `partners` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
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
  `Discount` DOUBLE NULL, 
  `Type` INTEGER, 
  `IsVeryUsed` INTEGER, 
  `UserID` INTEGER, 
  `GroupID` INTEGER, 
  `UserRealTime` DATETIME, 
  `Deleted` INTEGER,
  `CardNumber` VARCHAR(255), 
  `Note1` VARCHAR(255), 
  `Note2` VARCHAR(255), 
  `PaymentDays` INTEGER
);
CREATE INDEX `partners_ID_idx` ON `partners` (`ID`);
CREATE INDEX `partners_Code_idx` ON `partners` (`Code`);
CREATE INDEX `partners_Company_idx` ON `partners` (`Company`);
CREATE INDEX `partners_MOL_idx` ON `partners` (`MOL`);
CREATE INDEX `partners_TaxNo_idx` ON `partners` (`TaxNo`);
CREATE INDEX `partners_Bulstat_idx` ON `partners` (`Bulstat`);
CREATE INDEX `partners_BankCode_idx` ON `partners` (`BankCode`);
CREATE INDEX `partners_BankVATCode_idx` ON `partners` (`BankVATCode`);
CREATE INDEX `partners_UserID_idx` ON `partners` (`UserID`);
CREATE INDEX `partners_GroupID_idx` ON `partners` (`GroupID`);

INSERT INTO `partners` VALUES (1, ' ', 'Служебен партньор', 'Служебен партньор', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 1, 0, 0, -2, 1, -1, NULL, 0, ' ', ' ', ' ', 0);


DROP TABLE IF EXISTS `partnersgroups`;
CREATE TABLE `partnersgroups` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255)
);
CREATE INDEX `partnersgroups_ID_idx` ON `partnersgroups` (`ID`);
CREATE INDEX `partnersgroups_Code_idx` ON `partnersgroups` (`Code`);

INSERT INTO `partnersgroups` VALUES (1, '-1', 'Служебна група');


DROP TABLE IF EXISTS `payments`;
CREATE TABLE `payments` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Acct` DOUBLE NULL, 
  `OperType` INTEGER, 
  `PartnerID` INTEGER, 
  `ObjectID` INTEGER, 
  `Qtty` DOUBLE NULL, 
  `Mode` INTEGER, 
  `Sign` INTEGER, 
  `Date` DATETIME, 
  `UserID` INTEGER, 
  `UserRealTime` DATETIME, 
  `Type` INTEGER, 
  `TransactionNumber` VARCHAR(255),
  `EndDate` DATETIME
);
CREATE INDEX `payments_ID_idx` ON `payments` (`ID`);
CREATE INDEX `payments_Acct_idx` ON `payments` (`Acct`);
CREATE INDEX `payments_OperType_idx` ON `payments` (`OperType`);
CREATE INDEX `payments_PartnerID_idx` ON `payments` (`PartnerID`);
CREATE INDEX `payments_ObjectID_idx` ON `payments` (`ObjectID`);
CREATE INDEX `payments_Date_idx` ON `payments` (`Date`);
CREATE INDEX `payments_UserID_idx` ON `payments` (`UserID`);
CREATE INDEX `payments_EndDate_idx` ON `payments` (`EndDate`);


DROP TABLE IF EXISTS `paymenttypes`;
CREATE TABLE `paymenttypes` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT,
  `Name` VARCHAR(255),
  `PaymentMethod` INTEGER
);

INSERT INTO `paymenttypes` VALUES (1, 'Плащане в брой', 1);
INSERT INTO `paymenttypes` VALUES (2, 'Превод по сметка', 2);
INSERT INTO `paymenttypes` VALUES (3, 'Дебитна/Кредитна карта', 3);
INSERT INTO `paymenttypes` VALUES (4, 'Плащане чрез ваучер', 4);


DROP TABLE IF EXISTS `pricerules`;
CREATE TABLE `pricerules` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Name` VARCHAR(255),
  `Formula` VARCHAR(1000),
  `Enabled` INTEGER,
  `Priority` INTEGER
);
CREATE INDEX `pricerules_ID_idx` ON `pricerules` (`ID`);
CREATE INDEX `pricerules_Name_idx` ON `pricerules` (`Name`);
CREATE INDEX `pricerules_Formula_idx` ON `pricerules` (`Formula`);


DROP TABLE IF EXISTS `registration`;
CREATE TABLE `registration` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
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
  `IsDefault` INTEGER,
  `Note1` VARCHAR(255), 
  `Note2` VARCHAR(255),
  `Deleted` INTEGER
);
CREATE INDEX `registration_ID_idx` ON `registration` (`ID`);
CREATE INDEX `registration_Code_idx` ON `registration` (`Code`);
CREATE INDEX `registration_Company_idx` ON `registration` (`Company`);
CREATE INDEX `registration_MOL_idx` ON `registration` (`MOL`);
CREATE INDEX `registration_TaxNo_idx` ON `registration` (`TaxNo`);
CREATE INDEX `registration_Bulstat_idx` ON `registration` (`Bulstat`);
CREATE INDEX `registration_BankCode_idx` ON `registration` (`BankCode`);
CREATE INDEX `registration_UserID_idx` ON `registration` (`UserID`);

INSERT INTO `registration` VALUES (1, ' ', 'Служебна фирма', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 1, NULL, -1, ' ', ' ', 0);


DROP TABLE IF EXISTS `store`;
CREATE TABLE `store` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `ObjectID` INTEGER, 
  `GoodID` INTEGER, 
  `Qtty` DOUBLE NULL, 
  `Price` DOUBLE NULL, 
  `Lot` VARCHAR(250), 
  `LotID`  INTEGER, 
  `LotOrder`  INTEGER
);
CREATE INDEX `store_ID_idx` ON `store` (`ID`);
CREATE INDEX `store_GoodID_idx` ON `store` (`GoodID`);
CREATE INDEX `store_ObjectID_idx` ON `store` (`ObjectID`);
CREATE INDEX `store_Lot_idx` ON `store` (`Lot`);
CREATE INDEX `store_LotID_idx` ON `store` (`LotID`);

INSERT INTO `store` VALUES (1, 1, 1, 0, 0, ' ', 1, 1);


DROP TABLE IF EXISTS `system`;
CREATE TABLE `system` (
  `Version` VARCHAR(20) NOT NULL, 
  `ProductID` INTEGER, 
  `LastBackup` DATETIME, 
  PRIMARY KEY (`Version`)
);
CREATE INDEX `system_Version_idx` ON `system` (`Version`);
CREATE INDEX `system_ProductID_idx` ON `system` (`ProductID`);

INSERT INTO `system` VALUES ('3.07', 1, NULL);


DROP TABLE IF EXISTS `transformations`;
CREATE TABLE `transformations` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `RootOperType` INTEGER, 
  `RootAcct` INTEGER, 
  `FromOperType` INTEGER, 
  `FromAcct` INTEGER, 
  `ToOperType` INTEGER, 
  `ToAcct` INTEGER, 
  `UserID` INTEGER, 
  `UserRealTime` DATETIME
);
CREATE INDEX `transformations_RootOperType_idx` ON `transformations` (`RootOperType`);
CREATE INDEX `transformations_RootAcct_idx` ON `transformations` (`RootAcct`);
CREATE INDEX `transformations_FromOperType_idx` ON `transformations` (`FromOperType`);
CREATE INDEX `transformations_FromAcct_idx` ON `transformations` (`FromAcct`);
CREATE INDEX `transformations_ToOperType_idx` ON `transformations` (`ToOperType`);
CREATE INDEX `transformations_ToAcct_idx` ON `transformations` (`ToAcct`);
CREATE INDEX `transformations_UserID_idx` ON `transformations` (`UserID`);


DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `Name2` VARCHAR(255), 
  `IsVeryUsed` INTEGER, 
  `GroupID` INTEGER, 
  `Password` VARCHAR(50), 
  `UserLevel` INTEGER, 
  `Deleted` INTEGER, 
  `CardNumber` VARCHAR(255)
);
CREATE INDEX `users_ID_idx` ON `users` (`ID`);
CREATE INDEX `users_Code_idx` ON `users` (`Code`);
CREATE INDEX `users_Name_idx` ON `users` (`Name`);
CREATE INDEX `users_Name2_idx` ON `users` (`Name2`);
CREATE INDEX `users_GroupID_idx` ON `users` (`GroupID`);

INSERT INTO `users` VALUES (1, ' ', 'Служебен потребител', 'Служебен потребител', 1, -1, 'YsAB16V90Bs=', 3, 0, ' ');


DROP TABLE IF EXISTS `usersgroups`;
CREATE TABLE `usersgroups` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255)
);
CREATE INDEX `usersgroups_ID_idx` ON `usersgroups` (`ID`);
CREATE INDEX `usersgroups_Code_idx` ON `usersgroups` (`Code`);

INSERT INTO `usersgroups` VALUES (1, '-1', 'Служебна група');


DROP TABLE IF EXISTS `userssecurity`;
CREATE TABLE `userssecurity` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `UserID` INTEGER, 
  `ControlName` VARCHAR(100), 
  `State` INTEGER
);
CREATE INDEX `userssecurity_ID_idx` ON `userssecurity` (`ID`);
CREATE INDEX `userssecurity_UserID_idx` ON `userssecurity` (`UserID`);
CREATE INDEX `userssecurity_ControlName_idx` ON `userssecurity` (`ControlName`);


DROP TABLE IF EXISTS `vatgroups`;
CREATE TABLE `vatgroups` (
  `ID` INTEGER PRIMARY KEY AUTOINCREMENT, 
  `Code` VARCHAR(255), 
  `Name` VARCHAR(255), 
  `VATValue` DOUBLE NULL
);
CREATE INDEX `vatgroups_ID_idx` ON `vatgroups` (`ID`);
CREATE INDEX `vatgroups_Code_idx` ON `vatgroups` (`Code`);
CREATE INDEX `vatgroups_Name_idx` ON `vatgroups` (`Name`);

INSERT INTO `vatgroups` VALUES (1, '1', 'Основна ДДС група', 20);

