-- Hospedaje: perros que se quedan varias noches en la casa de un cuidador (un paseador que activa la opcion).
-- Es idempotente: se puede aplicar mas de una vez.

IF COL_LENGTH('dbo.Walkers', 'BoardingEnabled') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD BoardingEnabled BIT NOT NULL CONSTRAINT DF_Walkers_BoardingEnabled DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.Walkers', 'BoardingPricePerNight') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD BoardingPricePerNight DECIMAL(10, 2) NULL;      -- por noche y por perro
END
GO

IF COL_LENGTH('dbo.Walkers', 'BoardingMaxDogs') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD BoardingMaxDogs INT NOT NULL CONSTRAINT DF_Walkers_BoardingMaxDogs DEFAULT 1;   -- perros a la vez
END
GO

IF COL_LENGTH('dbo.Walkers', 'BoardingDescription') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD BoardingDescription NVARCHAR(500) NULL;         -- como es la casa (patio, otros animales...)
END
GO

IF OBJECT_ID('dbo.Stays', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stays (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        WalkerId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        CheckIn DATETIME NOT NULL,              -- dia de ingreso (sin hora: la hora se acuerda por el chat)
        CheckOut DATETIME NOT NULL,             -- dia de salida
        Nights INT NOT NULL,
        DogsCount INT NOT NULL,
        PricePerNight DECIMAL(10, 2) NOT NULL,  -- precio vigente al reservar, por noche y por perro
        Total DECIMAL(12, 2) NOT NULL,          -- Nights * DogsCount * PricePerNight
        Details NVARCHAR(500) NULL,             -- indicaciones del cliente (comida, medicacion, cuidados)
        Status VARCHAR(20) NOT NULL,            -- Pending | Confirmed | Cancelled
        CancelledBy VARCHAR(20) NULL,           -- Walker | Customer | System | Admin
        StatusChangedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL,
        StartedAt DATETIME NULL,                -- el cuidador recibio a los perros
        FinishedAt DATETIME NULL,               -- el cuidador devolvio a los perros
        ReceivedAt DATETIME NULL,               -- el cuidador confirmo que cobro
        CONSTRAINT PK_Stays PRIMARY KEY (Id),
        CONSTRAINT FK_Stays_Walkers FOREIGN KEY (WalkerId) REFERENCES dbo.Walkers (Id),
        CONSTRAINT FK_Stays_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
    CREATE INDEX IX_Stays_WalkerId ON dbo.Stays (WalkerId);
    CREATE INDEX IX_Stays_CustomerId ON dbo.Stays (CustomerId);
END
GO

IF OBJECT_ID('dbo.StayPets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StayPets (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        StayId BIGINT NOT NULL,
        PetId BIGINT NOT NULL,
        CONSTRAINT PK_StayPets PRIMARY KEY (Id),
        CONSTRAINT FK_StayPets_Stays FOREIGN KEY (StayId) REFERENCES dbo.Stays (Id),
        CONSTRAINT FK_StayPets_Pets FOREIGN KEY (PetId) REFERENCES dbo.Pets (Id)
    );
    CREATE INDEX IX_StayPets_StayId ON dbo.StayPets (StayId);
END
GO
