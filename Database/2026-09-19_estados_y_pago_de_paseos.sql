/*
  Agrega a la tabla Walks el estado del paseo y el metodo de pago.

  Status          Pending (esperando al paseador) | Confirmed | Cancelled
  CancelledBy     Walker | Customer | System (cancelacion automatica por falta de respuesta)
  StatusChangedAt cuando se confirmo o cancelo
  BookingCode     agrupa los paseos (uno por mascota) que salieron de una misma reserva; NULL en paseos anteriores
  PaymentMethod   Cash | MercadoPago
  PaymentStatus   Pending | Paid

  La columna Confirmed se mantiene sincronizada con Status por compatibilidad.
  Los paseos ya confirmados pasan a Status = Confirmed; el resto queda Pending.

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-09-19_estados_y_pago_de_paseos.sql
*/

IF COL_LENGTH('dbo.Walks', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD
        Status          varchar(20)      NOT NULL CONSTRAINT DF_Walks_Status DEFAULT 'Pending',
        CancelledBy     varchar(20)      NULL,
        StatusChangedAt datetime         NULL,
        BookingCode     uniqueidentifier NULL,
        PaymentMethod   varchar(20)      NOT NULL CONSTRAINT DF_Walks_PaymentMethod DEFAULT 'Cash',
        PaymentStatus   varchar(20)      NOT NULL CONSTRAINT DF_Walks_PaymentStatus DEFAULT 'Pending';
END
GO

UPDATE dbo.Walks SET Status = 'Confirmed' WHERE Confirmed = 1 AND Status = 'Pending';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Walks_BookingCode' AND object_id = OBJECT_ID('dbo.Walks'))
BEGIN
    CREATE INDEX IX_Walks_BookingCode ON dbo.Walks (BookingCode);
END
GO

/*
  Para deshacerlo:
    DROP INDEX IX_Walks_BookingCode ON dbo.Walks;
    ALTER TABLE dbo.Walks DROP CONSTRAINT DF_Walks_Status, DF_Walks_PaymentMethod, DF_Walks_PaymentStatus;
    ALTER TABLE dbo.Walks DROP COLUMN Status, CancelledBy, StatusChangedAt, BookingCode, PaymentMethod, PaymentStatus;
*/
