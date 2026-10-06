-- Publicidad: comercios que se anuncian en la app (pet shops, veterinarias, transporte de mascotas, forrajerias, etc.) y sus avisos.
-- Es idempotente: se puede aplicar mas de una vez.

IF OBJECT_ID('dbo.Advertisers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Advertisers (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        Name NVARCHAR(80) NOT NULL,
        Category VARCHAR(20) NOT NULL,          -- PetShop | Veterinary | PetTransport | FeedStore | Groomer | Trainer | DayCare | Other
        Description NVARCHAR(300) NULL,
        Phone VARCHAR(30) NULL,
        Website VARCHAR(200) NULL,              -- http o https
        StreetName NVARCHAR(100) NULL,
        StreetNumber BIGINT NULL,
        CityName NVARCHAR(100) NULL,
        Latitude VARCHAR(30) NULL,
        Longitude VARCHAR(30) NULL,
        Active BIT NOT NULL CONSTRAINT DF_Advertisers_Active DEFAULT 1,
        CreatedAt DATETIME NOT NULL,
        CONSTRAINT PK_Advertisers PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID('dbo.Ads', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ads (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        AdvertiserId BIGINT NOT NULL,
        Title NVARCHAR(60) NOT NULL,
        Text NVARCHAR(200) NULL,
        ImageFile VARCHAR(80) NULL,             -- archivo en Content/images/uploads/ads
        LinkUrl VARCHAR(200) NULL,              -- http o https
        StartDate DATETIME NOT NULL,            -- dia desde el que se muestra (inclusive)
        EndDate DATETIME NOT NULL,              -- dia hasta el que se muestra (inclusive)
        Audience VARCHAR(10) NOT NULL,          -- All | Customers | Walkers
        RadiusKm INT NULL,                      -- null = se muestra a todos, sin importar la zona
        Active BIT NOT NULL CONSTRAINT DF_Ads_Active DEFAULT 1,
        Impressions INT NOT NULL CONSTRAINT DF_Ads_Impressions DEFAULT 0,
        Clicks INT NOT NULL CONSTRAINT DF_Ads_Clicks DEFAULT 0,
        CreatedAt DATETIME NOT NULL,
        CONSTRAINT PK_Ads PRIMARY KEY (Id),
        CONSTRAINT FK_Ads_Advertisers FOREIGN KEY (AdvertiserId) REFERENCES dbo.Advertisers (Id)
    );

    CREATE INDEX IX_Ads_Active_Dates ON dbo.Ads (Active, StartDate, EndDate);
    CREATE INDEX IX_Ads_Advertiser ON dbo.Ads (AdvertiserId);
END
GO
