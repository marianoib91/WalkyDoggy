-- Perfil de administrador: motivo del bloqueo de una cuenta y bitacora de las acciones del administrador.
-- Es idempotente: se puede aplicar mas de una vez.

IF COL_LENGTH('dbo.Users', 'BlockReason') IS NULL
    ALTER TABLE dbo.Users ADD BlockReason NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.Users', 'BlockedAt') IS NULL
    ALTER TABLE dbo.Users ADD BlockedAt DATETIME NULL;
GO

IF OBJECT_ID('dbo.AdminActions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminActions (
        Id BIGINT IDENTITY(1, 1) NOT NULL,
        AdminUserId BIGINT NOT NULL,
        Action VARCHAR(40) NOT NULL,
        TargetUserId BIGINT NULL,
        Detail NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL,
        CONSTRAINT PK_AdminActions PRIMARY KEY (Id)
    );

    CREATE INDEX IX_AdminActions_CreatedAt ON dbo.AdminActions (CreatedAt DESC);
END
GO

-- El rol de administrador ya existe en la base (Id 1); se asegura por si falta
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Roles ON;
    INSERT INTO dbo.Roles (Id, Name) VALUES (1, 'Admin');
    SET IDENTITY_INSERT dbo.Roles OFF;
END
GO
