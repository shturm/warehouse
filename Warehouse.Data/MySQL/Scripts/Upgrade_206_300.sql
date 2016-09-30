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

ALTER TABLE operationtype ADD TR VARCHAR(255);

ALTER TABLE operationtype ADD SQ VARCHAR(255);

ALTER TABLE operationtype ADD SR VARCHAR(255);

ALTER TABLE payments ADD EndDate DATETIME;

ALTER TABLE objects ADD `PriceGroup` INTEGER;

UPDATE objects SET PriceGroup = 1;

UPDATE payments SET EndDate = CURRENT_DATE();;

UPDATE operationtype SET TR = 'Alışlar', SQ = 'Hyrjet', SR = 'Nabavke' WHERE ID = 1;
              
UPDATE operationtype SET TR = 'Satışlar', SQ = 'Shitjet', SR = 'Prodaje' WHERE ID = 2;

UPDATE operationtype SET TR = 'Hurda', SQ = 'Shpërdorje', SR = 'Trošak' WHERE ID = 3;
              
UPDATE operationtype SET TR = 'Gözden geçirme', SQ = 'Regjistrimi i malli', SR = 'Popis' WHERE ID = 4;
              
UPDATE operationtype SET TR = 'Üretim', SQ = 'Prodhimi', SR = 'Produkcija' WHERE ID IN (5, 6);
              
UPDATE operationtype SET TR = 'Transfer', SQ = 'Transfero', SR = 'Prenos' WHERE ID IN (7, 8);
              
UPDATE operationtype SET TR = 'İş yeri', SQ = 'Trade Object', SR = 'Trgovački objekat' WHERE ID = 9;
              
UPDATE operationtype SET TR = 'Mağaza ekranı', SQ = 'Touch Screen', SR = 'Reagovanje ekrana na dodir' WHERE ID = 10;
              
UPDATE operationtype SET TR = 'Depodan çıkarma', SQ = 'Tërheqje interne', SR = 'Zapis' WHERE ID = 11;
              
UPDATE operationtype SET TR = 'Mal istekleri', SQ = 'Kërkesat', SR = 'Zahtevi' WHERE ID = 12;
              
UPDATE operationtype SET TR = 'Teklifler', SQ = 'Oferta', SR = 'Ponude' WHERE ID = 13;
              
UPDATE operationtype SET TR = 'Farura öncesi belge', SQ = 'Pro-Faturë', SR = 'Profaktura' WHERE ID = 14;
              
UPDATE operationtype SET TR = 'Vadeli mal verme', SQ = 'Artikujt në konsignacion', SR = 'Davanje konsignacione robe' WHERE ID = 15;
              
UPDATE operationtype SET TR = 'Vadeli mal hesap tutma', SQ = 'Shitjet me konsignacion', SR = 'Konsignaciona prodaja' WHERE ID = 16;
              
UPDATE operationtype SET TR = 'Vadeli mal geri alma', SQ = 'Kthimi me konsignacion', SR = 'Vraćanje konsignacione robe' WHERE ID = 17;
              
UPDATE operationtype SET TR = 'Vadeli mal alma', SQ = 'Hyrje me konsignacion', SR = 'Prijem poslate robe' WHERE ID = 18;
              
UPDATE operationtype SET TR = 'Siparişler', SQ = 'Porositë', SR = 'Narudžbine' WHERE ID = 19;
              
UPDATE operationtype SET TR = 'Malzeme', SQ = 'Lëndë e parë', SR = 'Sirovi materijal' WHERE ID IN (20, 22);
              
UPDATE operationtype SET TR = 'Ürün', SQ = 'Prodhim', SR = 'Produkcija' WHERE ID IN (21, 23);
              
UPDATE operationtype SET TR = 'Karışık üretim', SQ = 'Prodhimet Komplekse', SR = 'Složena produkcija' WHERE ID IN (24, 25);
              
UPDATE operationtype SET TR = 'Borç dekontu', SQ = 'Shënimet mbi debin', SR = 'Izveštaj o zaduženji' WHERE ID = 26;
              
UPDATE operationtype SET TR = 'Kredi dekontu', SQ = 'Shënimet mbi kredin', SR = 'Izveštaj o odobrenjima' WHERE ID = 27;
              
UPDATE operationtype SET TR = 'Garanti belgeleri', SQ = 'Fletëgarancionet', SR = '- Garantni list' WHERE ID = 28;
              
UPDATE operationtype SET TR = 'Ambalaj ham maddesi', SQ = 'Lënda e parë e ambalazhit', SR = 'NOT TRANSLATED' WHERE ID = 29;
              
UPDATE operationtype SET TR = 'Ambalaj hazır ürünü', SQ = 'Ambalazh produkti', SR = 'NOT TRANSLATED' WHERE ID = 30;
              
UPDATE operationtype SET TR = 'Ambalaj verme', SQ = 'Dhënie e ambalazhit', SR = 'NOT TRANSLATED' WHERE ID = 31;
              
UPDATE operationtype SET TR = 'Ambalaj geri alma', SQ = 'Kthim i ambalazhit', SR = 'NOT TRANSLATED' WHERE ID = 32;
              
UPDATE `system` SET Version = '3.00';

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;