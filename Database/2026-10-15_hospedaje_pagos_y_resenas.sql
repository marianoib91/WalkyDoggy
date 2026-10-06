-- Hospedaje, etapa 2: forma de pago elegida al pedirlo (efectivo o Mercado Pago, como en los paseos) y valoraciones/reseñas
-- que tambien se pueden dejar sobre un hospedaje (sin paseo asociado). Es idempotente: se puede aplicar mas de una vez.

SET QUOTED_IDENTIFIER ON;   -- los indices filtrados lo exigen
GO

IF COL_LENGTH('dbo.Stays', 'PaymentMethod') IS NULL
BEGIN
    ALTER TABLE dbo.Stays ADD PaymentMethod VARCHAR(20) NOT NULL CONSTRAINT DF_Stays_PaymentMethod DEFAULT 'Cash';   -- Cash | MercadoPago
END
GO

IF COL_LENGTH('dbo.Stays', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Stays ADD PaymentStatus VARCHAR(20) NOT NULL CONSTRAINT DF_Stays_PaymentStatus DEFAULT 'Pending';   -- Pending | Paid | Received
END
GO

IF COL_LENGTH('dbo.Stays', 'PaymentId') IS NULL
BEGIN
    ALTER TABLE dbo.Stays ADD PaymentId VARCHAR(60) NULL;       -- id del pago en Mercado Pago
END
GO

IF COL_LENGTH('dbo.Stays', 'PaidAt') IS NULL
BEGIN
    ALTER TABLE dbo.Stays ADD PaidAt DATETIME NULL;
END
GO

-- Una valoracion o una reseña de mascota de un hospedaje no tiene paseo: WalkId pasa a ser opcional (la clave del hospedaje es "h" + id en BookingKey)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Rankings') AND name = 'WalkId' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.Rankings ALTER COLUMN WalkId BIGINT NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PetReviews') AND name = 'WalkId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PetReviews_Walk' AND object_id = OBJECT_ID('dbo.PetReviews'))
        DROP INDEX UX_PetReviews_Walk ON dbo.PetReviews;
    ALTER TABLE dbo.PetReviews ALTER COLUMN WalkId BIGINT NULL;
END
GO

-- Un paseo se reseña una vez; un hospedaje, una vez por mascota
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PetReviews_Walk' AND object_id = OBJECT_ID('dbo.PetReviews'))
BEGIN
    CREATE UNIQUE INDEX UX_PetReviews_Walk ON dbo.PetReviews (WalkId) WHERE WalkId IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PetReviews_Booking_Pet' AND object_id = OBJECT_ID('dbo.PetReviews'))
BEGIN
    CREATE UNIQUE INDEX UX_PetReviews_Booking_Pet ON dbo.PetReviews (BookingKey, PetId);
END
GO
