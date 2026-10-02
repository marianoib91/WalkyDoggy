/*
  Pagos con retencion: el cliente le paga a WalkyDoggy (Mercado Pago) y el dinero queda retenido
  hasta que el paseo se hace. Despues se libera al paseador o se reembolsa al cliente.

  Agrega a la tabla Walks:
    PaymentId             id del pago en Mercado Pago (igual para todos los paseos de una reserva)
    PaidAt                cuando se cobro
    ReleasedAt            cuando se libero al paseador
    RefundedAt            cuando se reembolso al cliente
    PaymentDisputeReason  motivo del reclamo del cliente

  PaymentStatus pasa a ser: Pending | Held | Released | Refunded | Disputed
  (el valor anterior "Paid" se convierte en "Held").

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-02_pagos_con_retencion.sql
*/

IF COL_LENGTH('dbo.Walks', 'PaymentId') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD
        PaymentId            varchar(50)  NULL,
        PaidAt               datetime     NULL,
        ReleasedAt           datetime     NULL,
        RefundedAt           datetime     NULL,
        PaymentDisputeReason varchar(500) NULL;
END
GO

UPDATE dbo.Walks SET PaymentStatus = 'Held' WHERE PaymentStatus = 'Paid';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Walks_PaymentId' AND object_id = OBJECT_ID('dbo.Walks'))
BEGIN
    CREATE INDEX IX_Walks_PaymentId ON dbo.Walks (PaymentId);
END
GO

/*
  Para deshacerlo:
    DROP INDEX IX_Walks_PaymentId ON dbo.Walks;
    ALTER TABLE dbo.Walks DROP COLUMN PaymentId, PaidAt, ReleasedAt, RefundedAt, PaymentDisputeReason;
*/
