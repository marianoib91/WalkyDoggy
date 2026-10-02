/*
  Datos de cobro del paseador: la cuenta donde WalkyDoggy le transfiere lo que cobra con Mercado Pago.

  Agrega a la tabla Walkers:
    PayoutAccount   alias o CBU/CVU (22 digitos) de la cuenta donde cobra
    PayoutHolder    nombre del titular de esa cuenta

  Ambas son NULL hasta que el paseador las cargue desde "Mi perfil".

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-02_datos_de_cobro_del_paseador.sql
*/

IF COL_LENGTH('dbo.Walkers', 'PayoutAccount') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD
        PayoutAccount varchar(30)  NULL,
        PayoutHolder  varchar(100) NULL;
END
GO

/*
  Para deshacerlo:
    ALTER TABLE dbo.Walkers DROP COLUMN PayoutAccount, PayoutHolder;
*/
