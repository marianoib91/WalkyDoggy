/*
  Paseadores favoritos de cada cliente.

  FavoriteWalkers   un cliente marca a un paseador como favorito para encontrarlo rapido al pedir un paseo.
    CustomerId      el cliente
    WalkerId        el paseador
    CreatedAt       cuando lo marco
  Un cliente no puede tener dos veces al mismo paseador (indice unico).

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-07_paseadores_favoritos.sql
*/

IF OBJECT_ID('dbo.FavoriteWalkers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FavoriteWalkers (
        Id         bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_FavoriteWalkers PRIMARY KEY,
        CustomerId bigint   NOT NULL CONSTRAINT FK_FavoriteWalkers_Customers REFERENCES dbo.Customers (Id),
        WalkerId   bigint   NOT NULL CONSTRAINT FK_FavoriteWalkers_Walkers REFERENCES dbo.Walkers (Id),
        CreatedAt  datetime NOT NULL
    );

    CREATE UNIQUE INDEX UX_FavoriteWalkers_Customer_Walker ON dbo.FavoriteWalkers (CustomerId, WalkerId);
END
GO
