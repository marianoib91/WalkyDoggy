/*
  Zona de trabajo del paseador.

  El paseador deja de cargar un domicilio personal: ahora carga una direccion de referencia (por ejemplo, una plaza donde
  hace sus paseos) y un radio en kilometros. Las columnas StreetName, StreetNumber, Latitude y Longitude pasan a ser esa
  direccion de referencia, y se agrega:

    ServiceRadiusKm   radio, en kilometros, alrededor de la direccion de referencia en el que acepta retirar mascotas

  Los paseadores que ya existian arrancan con 5 km (los de demostracion, con radios variados) y pueden cambiarlo desde su perfil.
  La columna PayoutAccount (alias o CBU/CVU) ya existia y vuelve a usarse.

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-04_zona_de_trabajo_del_paseador.sql
*/

IF COL_LENGTH('dbo.Walkers', 'ServiceRadiusKm') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD ServiceRadiusKm float NOT NULL CONSTRAINT DF_Walkers_ServiceRadiusKm DEFAULT 5;
END
GO

-- Radios de los paseadores de demostracion (ids 14 a 28) y del paseador de prueba (id 1): solo si todavia tienen el valor por defecto
UPDATE w
SET    w.ServiceRadiusKm = v.Radio
FROM   dbo.Walkers w
       JOIN (VALUES (1, 6.0), (14, 4.0), (15, 8.0), (16, 3.0), (17, 5.0), (18, 6.0), (19, 2.5), (20, 10.0), (21, 4.0), (22, 5.0),
                    (23, 12.0), (24, 3.0), (25, 6.0), (26, 7.0), (27, 2.0), (28, 15.0)) AS v(Id, Radio) ON v.Id = w.Id
WHERE  w.ServiceRadiusKm = 5;
GO
