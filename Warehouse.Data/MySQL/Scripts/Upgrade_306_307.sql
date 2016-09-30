SET CHARACTER SET utf8;
SET NAMES 'utf8';

ALTER TABLE payments ADD ObjectID INTEGER;
CREATE INDEX `IX_ObjectID` ON payments (ObjectID);
ALTER TABLE payments ADD `Sign` INTEGER;
ALTER TABLE partners ADD PaymentDays INTEGER;
ALTER TABLE operations ADD CurrencyRate DOUBLE;
ALTER TABLE applicationlog ADD `MessageSource` VARCHAR(50);
UPDATE operations, currencies SET operations.CurrencyRate = currencies.ExchangeRate WHERE currencies.ID = operations.CurrencyID;
UPDATE payments, operations SET payments.ObjectID = operations.ObjectID WHERE payments.Acct = operations.Acct AND payments.OperType = operations.OperType;
UPDATE payments SET ObjectID = TransactionNumber WHERE TransactionNumber <> ' ' AND OperType = 36;
UPDATE payments SET ObjectID = 1 WHERE ObjectID IS NULL;
UPDATE payments SET `Sign` = 0;
UPDATE payments SET `Sign` = -1 WHERE OperType IN (1,27,34);
UPDATE payments SET `Sign` = 1 WHERE OperType IN (2,4,16,26,36);
UPDATE applicationlog SET MessageSource = 'NA';
UPDATE operationtype SET `BG` = 'Резервация', `EN` = 'Booking', `DE` = 'Buchungen', `RU` = 'Бронирование', `TR` = 'Rezervasyon', `SQ` = 'Rezervim', `SR` = 'Rezervacija', `RO` = 'Rezervare', `GR` = 'Κράτηση'  WHERE ID = 35;
INSERT INTO operationtype (BG, EN, DE, RU, TR, SQ, SR, RO, GR) VALUES
	('Авансово плащане', 'Advance payment', 'NOT TRANSLATED', 'Предоплата', 'Avans ödemesi', 'NOT TRANSLATED', 'NOT TRANSLATED', 'Plata în avans', 'Προκαταβολή');
INSERT INTO operationtype (BG, EN, DE, RU, TR, SQ, SR, RO, GR) VALUES
    ('Дебитно известие от доставчик', 'Debit Note by supplier', 'NOT TRANSLATED', 'Дебетное извещение от поставщика', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');
INSERT INTO operationtype (BG, EN, DE, RU, TR, SQ, SR, RO, GR) VALUES
    ('Кредитно известие от доставчик', 'Credit Note by supplier', 'NOT TRANSLATED', 'Кредитное извещение от поставщика', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');
INSERT INTO operationtype (BG, EN, DE, RU, TR, SQ, SR, RO, GR) VALUES
    ('Рекламация към доставчик', 'Refund to supplier', 'NOT TRANSLATED', 'Возврат поставщику', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED', 'NOT TRANSLATED');
UPDATE partners SET PaymentDays = 0;
UPDATE goods SET Ratio = 1 WHERE Ratio = 0;
UPDATE `system` SET Version = '3.07';