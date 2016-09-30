-- MySQL Administrator dump 1.4
--
-- ------------------------------------------------------
-- Server version	5.0.19-nt-log


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

DROP TABLE IF EXISTS `paymenttypes`;
CREATE TABLE `paymenttypes` (
  `ID` INTEGER NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255),
  `PaymentMethod` INTEGER DEFAULT 0,
  PRIMARY KEY (`ID`)
)  TYPE= InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `paymenttypes` VALUES (1, 'Плащане в брой', 1);
INSERT INTO `paymenttypes` VALUES (2, 'Превод по сметка', 2);
INSERT INTO `paymenttypes` VALUES (3, 'Дебитна/Кредитна карта', 3);
INSERT INTO `paymenttypes` VALUES (4, 'Плащане чрез ваучер', 4);

UPDATE payments SET `Type` = `Type` + 1;
UPDATE documents SET PaymentType = PaymentType + 1;

ALTER TABLE operationtype ADD RO VARCHAR(255);

UPDATE operationtype SET RO = 'Aprovizionare' WHERE ID = 1;
UPDATE operationtype SET RO = 'Vănzare' WHERE ID = 2;
UPDATE operationtype SET RO = 'Casat' WHERE ID = 3;
UPDATE operationtype SET RO = 'Inventar' WHERE ID = 4;
UPDATE operationtype SET RO = 'Producţie' WHERE ID IN (5, 6);
UPDATE operationtype SET RO = 'Transfer' WHERE ID IN (7, 8);
UPDATE operationtype SET RO = 'Obiect comercial' WHERE ID = 9;
UPDATE operationtype SET RO = 'Ecran comercial' WHERE ID = 10;
UPDATE operationtype SET RO = 'Scoatere din gestiune' WHERE ID = 11;
UPDATE operationtype SET RO = 'Comandă' WHERE ID = 12;
UPDATE operationtype SET RO = 'Oferta' WHERE ID = 13;
UPDATE operationtype SET RO = 'Proforma' WHERE ID = 14;
UPDATE operationtype SET RO = 'Dare spre consignaţie' WHERE ID = 15;
UPDATE operationtype SET RO = 'Consignaţie înregistrată' WHERE ID = 16;
UPDATE operationtype SET RO = 'Retur de consignaţie' WHERE ID = 17;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 18;
UPDATE operationtype SET RO = 'Cerere de oferta' WHERE ID = 19;
UPDATE operationtype SET RO = 'Materie primă' WHERE ID IN (20, 22);
UPDATE operationtype SET RO = 'Produse' WHERE ID IN (21, 23);
UPDATE operationtype SET RO = 'Producţie complexă' WHERE ID IN (24, 25);
UPDATE operationtype SET RO = 'Notă de debit' WHERE ID = 26;
UPDATE operationtype SET RO = 'Notă de credit' WHERE ID = 27;
UPDATE operationtype SET RO = 'Certificat de garanţie' WHERE ID = 28;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 29;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 30;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 31;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 32;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 33;
UPDATE operationtype SET RO = 'Reclamaţie' WHERE ID = 34;
UPDATE operationtype SET RO = 'NOT TRANSLATED' WHERE ID = 35;
UPDATE system SET Version = '3.04';

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;