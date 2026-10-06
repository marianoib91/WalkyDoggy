-- Catalogos administrables: baja logica de razas y tamaños, y las caracteristicas de las mascotas (antes una lista fija en el codigo).
-- Es idempotente: se puede aplicar mas de una vez.

IF COL_LENGTH('dbo.Breeds', 'Active') IS NULL
    ALTER TABLE dbo.Breeds ADD Active BIT NOT NULL CONSTRAINT DF_Breeds_Active DEFAULT 1;
GO

IF COL_LENGTH('dbo.Sizes', 'Active') IS NULL
    ALTER TABLE dbo.Sizes ADD Active BIT NOT NULL CONSTRAINT DF_Sizes_Active DEFAULT 1;
GO

-- Cada caracteristica es una fila; las dos de un mismo par (opuestas) comparten PairId y se distinguen por Position (1 o 2).
-- El Code es lo que se guarda en la mascota ("Playful,Runner"): no cambia nunca, aunque se renombre la caracteristica.
IF OBJECT_ID('dbo.PetTraitDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PetTraitDefinitions (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        Code VARCHAR(40) NOT NULL,
        Label NVARCHAR(60) NOT NULL,
        PairId BIGINT NOT NULL,
        Position INT NOT NULL,
        Active BIT NOT NULL CONSTRAINT DF_PetTraitDefinitions_Active DEFAULT 1,
        CONSTRAINT PK_PetTraitDefinitions PRIMARY KEY (Id),
        CONSTRAINT UX_PetTraitDefinitions_Code UNIQUE (Code)
    );

    CREATE INDEX IX_PetTraitDefinitions_Pair ON dbo.PetTraitDefinitions (PairId);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PetTraitDefinitions)
BEGIN
    INSERT INTO dbo.PetTraitDefinitions (Code, Label, PairId, Position, Active) VALUES
        ('Playful', N'Juguetón', 1, 1, 1),         ('Calm', N'Tranquilo', 1, 2, 1),
        ('Extroverted', N'Extrovertido', 2, 1, 1), ('Introverted', N'Introvertido', 2, 2, 1),
        ('Runner', N'Corredor', 3, 1, 1),          ('SunNapper', N'Siestas al sol', 3, 2, 1),
        ('Obedient', N'Obediente', 4, 1, 1),       ('Naughty', N'Travieso', 4, 2, 1),
        ('Barker', N'Ladrador', 5, 1, 1),          ('Quiet', N'Silencioso', 5, 2, 1),
        ('WaterLover', N'Ama el agua', 6, 1, 1),   ('WaterAverse', N'Evita el agua', 6, 2, 1),
        ('Affectionate', N'Cariñoso', 7, 1, 1),    ('Independent', N'Independiente', 7, 2, 1);
END
GO
