-- Denuncias (con imagenes adjuntas) y moderacion de reseñas.
-- Es idempotente: se puede aplicar mas de una vez.

-- Una valoracion o reseña oculta por un administrador no se muestra ni cuenta en los promedios, pero NO se borra y se puede volver a mostrar
IF COL_LENGTH('dbo.Rankings', 'Hidden') IS NULL
    ALTER TABLE dbo.Rankings ADD Hidden BIT NOT NULL CONSTRAINT DF_Rankings_Hidden DEFAULT 0;
GO
IF COL_LENGTH('dbo.Rankings', 'HiddenReason') IS NULL
    ALTER TABLE dbo.Rankings ADD HiddenReason NVARCHAR(300) NULL;
GO
IF COL_LENGTH('dbo.Rankings', 'HiddenAt') IS NULL
    ALTER TABLE dbo.Rankings ADD HiddenAt DATETIME NULL;
GO

IF COL_LENGTH('dbo.PetReviews', 'Hidden') IS NULL
    ALTER TABLE dbo.PetReviews ADD Hidden BIT NOT NULL CONSTRAINT DF_PetReviews_Hidden DEFAULT 0;
GO
IF COL_LENGTH('dbo.PetReviews', 'HiddenReason') IS NULL
    ALTER TABLE dbo.PetReviews ADD HiddenReason NVARCHAR(300) NULL;
GO
IF COL_LENGTH('dbo.PetReviews', 'HiddenAt') IS NULL
    ALTER TABLE dbo.PetReviews ADD HiddenAt DATETIME NULL;
GO

IF OBJECT_ID('dbo.Complaints', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Complaints (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        BookingKey VARCHAR(40) NOT NULL,
        ReporterUserId BIGINT NOT NULL,
        ReportedUserId BIGINT NOT NULL,
        ReporterRole VARCHAR(10) NOT NULL,          -- Customer | Walker
        Reason VARCHAR(30) NOT NULL,                -- AnimalAbuse | DisrespectfulTreatment | NoShow | UndueCharge | Other
        Description NVARCHAR(1000) NOT NULL,
        Status VARCHAR(15) NOT NULL,                -- Open | InReview | Resolved
        Resolution VARCHAR(15) NULL,                -- NoAction | Warning | Block
        ResolutionNote NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL,
        ResolvedAt DATETIME NULL,
        ResolvedByUserId BIGINT NULL,
        CONSTRAINT PK_Complaints PRIMARY KEY (Id)
    );

    CREATE INDEX IX_Complaints_Status ON dbo.Complaints (Status, CreatedAt DESC);
    CREATE INDEX IX_Complaints_Booking ON dbo.Complaints (BookingKey);
END
GO

-- Las imagenes se guardan como archivos fuera de las carpetas publicas (App_Data/complaints) y solo las ve un administrador
IF OBJECT_ID('dbo.ComplaintImages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComplaintImages (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        ComplaintId BIGINT NOT NULL,
        FileName VARCHAR(80) NOT NULL,
        OriginalName NVARCHAR(200) NULL,
        ContentType VARCHAR(40) NOT NULL,
        SizeBytes INT NOT NULL,
        CreatedAt DATETIME NOT NULL,
        CONSTRAINT PK_ComplaintImages PRIMARY KEY (Id),
        CONSTRAINT FK_ComplaintImages_Complaints FOREIGN KEY (ComplaintId) REFERENCES dbo.Complaints (Id)
    );

    CREATE INDEX IX_ComplaintImages_Complaint ON dbo.ComplaintImages (ComplaintId);
END
GO
