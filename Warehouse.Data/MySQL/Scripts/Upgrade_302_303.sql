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

INSERT INTO `operationtype` (`ID`,`BG`,`EN`,`DE`,`RU`,`TR`,`SQ`,`SR`) VALUES 
(33, 'Поръчка - ресторант', 'Order - restaurant', 'NOT TRANSLATED', 'Заказ - ресторан', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED'),
(34, 'Рекламация', 'Refund', 'NOT TRANSLATED', 'Возврат', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED'),
(35, 'Рекламация към доставчик', 'Refund to supplier', 'NOT TRANSLATED', 'Возврат поставщику', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');

UPDATE operationtype SET EN = 'Purchase', RU = 'Приход' WHERE ID = 1;
UPDATE operationtype SET EN = 'Waste', RU = 'Брак' WHERE ID = 3;
UPDATE operationtype SET EN = 'Transfer', RU = 'Перемещение' WHERE ID = 7;
UPDATE operationtype SET EN = 'Transfer', RU = 'Перемещение' WHERE ID = 8;
UPDATE operationtype SET EN = 'Write-off', RU = 'Списание' WHERE ID = 11;
UPDATE operationtype SET EN = 'Request', RU = 'Заказ поставщику' WHERE ID = 12;
UPDATE operationtype SET EN = 'Offer', RU = 'Счёт на оплату' WHERE ID = 13;
UPDATE operationtype SET EN = 'Proforma Invoice', RU = 'Счёт на оплату' WHERE ID = 14;
UPDATE operationtype SET EN = 'Consign', RU = 'Передать на реализацию' WHERE ID = 15;
UPDATE operationtype SET EN = 'Sales on Consignment', RU = 'Отчёт по реализации' WHERE ID = 16;
UPDATE operationtype SET EN = 'Return consignment', RU = 'Возврат от реализации' WHERE ID = 17;
UPDATE operationtype SET EN = 'Order', RU = 'Клиентский заказ' WHERE ID = 19;
UPDATE system SET Version = '3.03';

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;