/*
  Reseñas de las mascotas, hechas por los paseadores que las llevaron.

  PetReviews    una reseña por paseo (fila de Walks, que es una mascota) y por paseador.
    WalkId      el paseo reseñado (unico: una sola reseña por mascota y paseo)
    PetId       la mascota
    WalkerId    el paseador que la escribio
    BookingKey  la reserva (BookingCode sin guiones, o "w" + Id en paseos viejos)
    Stars       de 1 a 5
    Comments    breve comentario (hasta 500 caracteres), opcional

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-09_resenas_de_mascotas.sql
*/

IF OBJECT_ID('dbo.PetReviews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PetReviews (
        Id         bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_PetReviews PRIMARY KEY,
        WalkId     bigint        NOT NULL CONSTRAINT FK_PetReviews_Walks REFERENCES dbo.Walks (Id),
        PetId      bigint        NOT NULL CONSTRAINT FK_PetReviews_Pets REFERENCES dbo.Pets (Id),
        WalkerId   bigint        NOT NULL CONSTRAINT FK_PetReviews_Walkers REFERENCES dbo.Walkers (Id),
        BookingKey varchar(40)   NOT NULL,
        [Date]     datetime      NOT NULL,
        Stars      int           NOT NULL CONSTRAINT CK_PetReviews_Stars CHECK (Stars BETWEEN 1 AND 5),
        Comments   nvarchar(500) NULL
    );

    CREATE UNIQUE INDEX UX_PetReviews_Walk ON dbo.PetReviews (WalkId);
    CREATE INDEX IX_PetReviews_Pet ON dbo.PetReviews (PetId);
END
GO
