/*
  Pago despues del paseo, directo al paseador (reemplaza el modelo con retencion).

  Flujo: el paseador da por finalizado el paseo -> el cliente paga (Mercado Pago a la cuenta del paseador, o efectivo)
  -> el paseador confirma que recibio el pago.

  Agrega a la tabla Walks:
    FinishedAt   cuando el paseador dio por finalizado el paseo
    ReceivedAt   cuando el paseador confirmo que recibio el pago

  PaymentStatus pasa a ser: Pending | Paid | Received
    Held, Released   -> Paid     (el cliente ya habia pagado)
    Refunded, Disputed -> Pending

  Quedan sin uso, y se conservan sin tocar, estas columnas del modelo anterior (todas NULL salvo prueba):
    Walks.ReleasedAt, Walks.RefundedAt, Walks.PaymentDisputeReason, Walkers.PayoutAccount, Walkers.PayoutHolder
  PaymentId y PaidAt siguen en uso (id del pago en Mercado Pago y cuando se pago).

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-02_pago_al_finalizar_el_paseo.sql
*/

IF COL_LENGTH('dbo.Walks', 'FinishedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD
        FinishedAt datetime NULL,
        ReceivedAt datetime NULL;
END
GO

UPDATE dbo.Walks SET PaymentStatus = 'Paid'    WHERE PaymentStatus IN ('Held', 'Released');
UPDATE dbo.Walks SET PaymentStatus = 'Pending' WHERE PaymentStatus IN ('Refunded', 'Disputed');
GO

/*
  Para deshacerlo:
    ALTER TABLE dbo.Walks DROP COLUMN FinishedAt, ReceivedAt;
*/
