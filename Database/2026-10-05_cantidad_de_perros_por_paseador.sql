/*
  Cantidad de perros que cada paseador puede pasear a la vez.

  Hasta ahora el limite era 5 para todos. Ahora lo elige cada paseador, de 1 a 5:

    MaxPetsAtOnce   mascotas que lleva en simultaneo en un mismo horario

  Los paseadores que ya existian arrancan con 5 (el limite de antes); los de demostracion, con valores variados.

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-05_cantidad_de_perros_por_paseador.sql
*/

IF COL_LENGTH('dbo.Walkers', 'MaxPetsAtOnce') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD MaxPetsAtOnce int NOT NULL CONSTRAINT DF_Walkers_MaxPetsAtOnce DEFAULT 5;
END
GO

-- Valores de los paseadores de demostracion (ids 14 a 28), del paseador de prueba (id 1) y de Tomas Quintin Palma (id 58): solo si todavia tienen el valor por defecto
UPDATE w
SET    w.MaxPetsAtOnce = v.Cantidad
FROM   dbo.Walkers w
       JOIN (VALUES (1, 3), (14, 4), (15, 5), (16, 2), (17, 4), (18, 3), (19, 5), (20, 1), (21, 3), (22, 2), (23, 3), (24, 5),
                    (25, 4), (26, 2), (27, 3), (28, 1), (58, 2)) AS v(Id, Cantidad) ON v.Id = w.Id
WHERE  w.MaxPetsAtOnce = 5;
GO
