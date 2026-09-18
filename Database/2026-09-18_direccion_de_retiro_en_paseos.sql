/*
  Agrega a la tabla Walks la direccion donde se retira a las mascotas en cada paseo.
  Por defecto es el domicilio del cliente, pero puede ser otra direccion (ej: esta con sus perros en otro lado).
  Las columnas son opcionales: los paseos anteriores quedan sin direccion propia y se usa la del cliente.

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-09-18_direccion_de_retiro_en_paseos.sql
*/

IF COL_LENGTH('dbo.Walks', 'PickupStreetName') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD
        PickupStreetName   nvarchar(50)  NULL,
        PickupStreetNumber bigint        NULL,
        PickupCityId       bigint        NULL,
        PickupLatitude     varchar(100)  NULL,
        PickupLongitude    varchar(100)  NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_dbo.Walks_dbo.Cities_PickupCityId')
BEGIN
    ALTER TABLE dbo.Walks ADD CONSTRAINT [FK_dbo.Walks_dbo.Cities_PickupCityId]
        FOREIGN KEY (PickupCityId) REFERENCES dbo.Cities (Id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PickupCityId' AND object_id = OBJECT_ID('dbo.Walks'))
BEGIN
    CREATE INDEX IX_PickupCityId ON dbo.Walks (PickupCityId);
END
GO

/*
  Para deshacerlo:
    DROP INDEX IX_PickupCityId ON dbo.Walks;
    ALTER TABLE dbo.Walks DROP CONSTRAINT [FK_dbo.Walks_dbo.Cities_PickupCityId];
    ALTER TABLE dbo.Walks DROP COLUMN PickupStreetName, PickupStreetNumber, PickupCityId, PickupLatitude, PickupLongitude;
*/
