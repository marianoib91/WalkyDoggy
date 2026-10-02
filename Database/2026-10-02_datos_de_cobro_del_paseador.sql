/*
  Datos de cobro del paseador: la cuenta donde WalkyDoggy le transfiere lo que cobra con Mercado Pago.

  Agrega a la tabla Walkers:
    PayoutAccount   alias, CBU/CVU (22 digitos) o email de Mercado Pago de la cuenta donde cobra
    PayoutHolder    nombre del titular de esa cuenta

  Ambas son NULL hasta que el paseador las cargue desde "Mi perfil".
  PayoutAccount admite hasta 120 caracteres para que entre un email.

  Es seguro ejecutarlo mas de una vez (tambien ensancha la columna si una version anterior la creo mas corta).
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-02_datos_de_cobro_del_paseador.sql
*/

IF COL_LENGTH('dbo.Walkers', 'PayoutAccount') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD
        PayoutAccount varchar(120) NULL,
        PayoutHolder  varchar(100) NULL;
END
ELSE IF COL_LENGTH('dbo.Walkers', 'PayoutAccount') < 120
BEGIN
    ALTER TABLE dbo.Walkers ALTER COLUMN PayoutAccount varchar(120) NULL;
END
GO

/*
  Para deshacerlo:
    ALTER TABLE dbo.Walkers DROP COLUMN PayoutAccount, PayoutHolder;
*/
