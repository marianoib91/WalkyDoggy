/*
  Agrega a la tabla Walkers los datos de la cuenta de Mercado Pago que el paseador vincula
  (autorizacion OAuth) para cobrar los pagos online directamente en su cuenta.

  Los tokens se guardan CIFRADOS por la aplicacion (nunca en texto plano) y no se envian al navegador.
  Todas las columnas son opcionales: un paseador sin cuenta vinculada solo puede cobrar en efectivo.

  MercadoPagoUserId          id de la cuenta de Mercado Pago del paseador
  MercadoPagoAccessToken     token de acceso (cifrado); vale 180 dias
  MercadoPagoRefreshToken    token para renovarlo (cifrado)
  MercadoPagoPublicKey       clave publica de la cuenta
  MercadoPagoTokenExpiresAt  cuando vence el token de acceso (UTC)
  MercadoPagoLinkedAt        cuando se vinculo la cuenta (UTC)

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-09-20_vinculo_de_mercado_pago_del_paseador.sql
*/

IF COL_LENGTH('dbo.Walkers', 'MercadoPagoUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Walkers ADD
        MercadoPagoUserId         varchar(50)   NULL,
        MercadoPagoAccessToken    varchar(1500) NULL,
        MercadoPagoRefreshToken   varchar(1500) NULL,
        MercadoPagoPublicKey      varchar(200)  NULL,
        MercadoPagoTokenExpiresAt datetime      NULL,
        MercadoPagoLinkedAt       datetime      NULL;
END
GO

/*
  Para deshacerlo:
    ALTER TABLE dbo.Walkers DROP COLUMN MercadoPagoUserId, MercadoPagoAccessToken, MercadoPagoRefreshToken,
                                        MercadoPagoPublicKey, MercadoPagoTokenExpiresAt, MercadoPagoLinkedAt;
*/
