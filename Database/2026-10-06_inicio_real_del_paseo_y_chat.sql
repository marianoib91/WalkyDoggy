/*
  Inicio real del paseo y chat entre el paseador y el cliente.

  Walks.StartedAt   el momento REAL en que empezo el paseo: lo inicia el CLIENTE cuando el paseador llega a buscar a la mascota
                    (asi el horario no depende de que el paseador diga que llego). Hasta ahora el inicio era el horario agendado.
                    Los paseos que ya estaban finalizados conservan como inicio su horario agendado.
  Walks.StartedBy   Customer | Walker: quien lo inicio. El paseador solo puede iniciarlo si el cliente no lo hizo pasados 15 minutos
                    de la hora agendada.

  Messages          mensajes del chat de una reserva (se habilita cuando el paseador la confirma).
    BookingKey      la reserva (BookingCode sin guiones, o "w" + Id en paseos viejos)
    SenderRole      Walker | Customer | System (avisos automaticos: confirmada, iniciada, finalizada)
    SenderId        id del paseador o del cliente (NULL en los avisos del sistema)
    ReadAt          cuando lo leyo la otra persona (NULL = sin leer)

  Es seguro ejecutarlo mas de una vez.
  Uso:  sqlcmd -S "(local)\SQLEXPRESS" -d WalkyDoggy -i 2026-10-06_inicio_real_del_paseo_y_chat.sql
*/

IF COL_LENGTH('dbo.Walks', 'StartedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD StartedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.Walks', 'StartedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Walks ADD StartedBy varchar(10) NULL;
END
GO

UPDATE dbo.Walks
SET    StartedAt = DATEADD(hour, CAST(LEFT(TimeFrom, 2) AS int), CAST(CAST([Date] AS date) AS datetime))
WHERE  StartedAt IS NULL AND FinishedAt IS NOT NULL;
GO

IF OBJECT_ID('dbo.Messages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages (
        Id         bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Messages PRIMARY KEY,
        BookingKey varchar(40)   NOT NULL,
        SenderRole varchar(10)   NOT NULL,
        SenderId   bigint        NULL,
        [Text]     nvarchar(500) NOT NULL,
        SentAt     datetime      NOT NULL,
        ReadAt     datetime      NULL
    );

    CREATE INDEX IX_Messages_BookingKey ON dbo.Messages (BookingKey, Id);
END
GO
