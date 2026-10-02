/*
  Valoraciones de los paseadores.

  El cliente califica al paseador (de 1 a 5 estrellas, con un comentario opcional) una vez que el paseador dio por finalizado
  el paseo. Hay una valoracion por reserva. Se reutiliza la tabla Rankings (estaba sin uso y vacia) y se amplia:

    WalkerId     el paseador valorado (para armar su promedio y su desglose)
    CustomerId   el cliente que valoro
    BookingKey   la reserva valorada (BookingCode sin guiones, o "w" + Id en paseos viejos). Es unica: una valoracion por reserva.
    WalkId       pasa a ser el primer paseo de esa reserva
    Comments     ahora es opcional y admite hasta 500 caracteres
    Score        estrellas, de 1 a 5

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-03_valoraciones_de_paseadores.sql
*/

IF COL_LENGTH('dbo.Rankings', 'BookingKey') IS NULL
BEGIN
    ALTER TABLE dbo.Rankings ADD
        WalkerId   bigint      NULL,
        CustomerId bigint      NULL,
        BookingKey varchar(40) NULL;
END
GO

-- Si hubiera valoraciones viejas, se completan los datos nuevos a partir de su paseo
UPDATE r
SET    r.WalkerId   = w.WalkerId,
       r.CustomerId = p.CustomerId,
       r.BookingKey = ISNULL(REPLACE(CONVERT(varchar(36), w.BookingCode), '-', ''), 'w' + CONVERT(varchar(20), w.Id))
FROM   dbo.Rankings r
       JOIN dbo.Walks w ON w.Id = r.WalkId
       JOIN dbo.Pets  p ON p.Id = w.PetId
WHERE  r.BookingKey IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Rankings WHERE WalkerId IS NULL OR CustomerId IS NULL OR BookingKey IS NULL)
BEGIN
    ALTER TABLE dbo.Rankings ALTER COLUMN WalkerId   bigint      NOT NULL;
    ALTER TABLE dbo.Rankings ALTER COLUMN CustomerId bigint      NOT NULL;
    ALTER TABLE dbo.Rankings ALTER COLUMN BookingKey varchar(40) NOT NULL;
END
GO

-- El comentario es opcional y puede ser mas largo
ALTER TABLE dbo.Rankings ALTER COLUMN Comments nvarchar(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rankings_Walkers')
    ALTER TABLE dbo.Rankings ADD CONSTRAINT FK_Rankings_Walkers FOREIGN KEY (WalkerId) REFERENCES dbo.Walkers (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rankings_Customers')
    ALTER TABLE dbo.Rankings ADD CONSTRAINT FK_Rankings_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Rankings_Score')
    ALTER TABLE dbo.Rankings ADD CONSTRAINT CK_Rankings_Score CHECK (Score >= 1 AND Score <= 5);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Rankings_BookingKey' AND object_id = OBJECT_ID('dbo.Rankings'))
    CREATE UNIQUE INDEX UX_Rankings_BookingKey ON dbo.Rankings (BookingKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rankings_WalkerId' AND object_id = OBJECT_ID('dbo.Rankings'))
    CREATE INDEX IX_Rankings_WalkerId ON dbo.Rankings (WalkerId, Date DESC);
GO

/*
  Para deshacerlo:
    DROP INDEX UX_Rankings_BookingKey ON dbo.Rankings;  DROP INDEX IX_Rankings_WalkerId ON dbo.Rankings;
    ALTER TABLE dbo.Rankings DROP CONSTRAINT FK_Rankings_Walkers, FK_Rankings_Customers, CK_Rankings_Score;
    ALTER TABLE dbo.Rankings DROP COLUMN WalkerId, CustomerId, BookingKey;
*/
