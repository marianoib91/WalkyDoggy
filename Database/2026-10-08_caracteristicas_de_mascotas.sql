/*
  Caracteristicas de las mascotas, para el matching entre perros.

  Pets.Traits   codigos separados por coma de las caracteristicas que el cliente marco para su perro
                (por ejemplo "Playful,Runner,Obedient"). NULL = todavia no marco ninguna.
                De cada par de opuestos (Playful/Calm, Extroverted/Introverted, Runner/SunNapper, Obedient/Naughty,
                Barker/Quiet, WaterLover/WaterAverse, Affectionate/Independent) se elige uno solo.

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-08_caracteristicas_de_mascotas.sql
*/

IF COL_LENGTH('dbo.Pets', 'Traits') IS NULL
BEGIN
    ALTER TABLE dbo.Pets ADD Traits varchar(200) NULL;
END
GO
