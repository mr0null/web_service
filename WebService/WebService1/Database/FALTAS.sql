IF DB_ID(N'FALTAS') IS NULL
BEGIN
    CREATE DATABASE FALTAS;
END
GO

USE FALTAS;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        DNI        NUMERIC(18,0) NOT NULL,
        Apellido   VARCHAR(30)  NOT NULL,
        Nombres    VARCHAR(50)  NOT NULL,
        Direccion  VARCHAR(100) NOT NULL,
        Localidad  VARCHAR(50)  NOT NULL,
        CONSTRAINT PK_Clientes PRIMARY KEY (DNI)
    );
END
GO

IF OBJECT_ID(N'dbo.Incidencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidencias
    (
        Nro_Incidencia NUMERIC(18,0) IDENTITY(1,1) NOT NULL,
        DNI            NUMERIC(18,0) NOT NULL,
        FechaHora      DATETIME NOT NULL CONSTRAINT DF_Incidencias_FechaHora DEFAULT (GETDATE()),
        Motivo         VARCHAR(50) NOT NULL,
        Estado         VARCHAR(30) NOT NULL CONSTRAINT DF_Incidencias_Estado DEFAULT ('PENDIENTE'),
        Observaciones  VARCHAR(512) NOT NULL CONSTRAINT DF_Incidencias_Observaciones DEFAULT (''),
        CONSTRAINT PK_Incidencias PRIMARY KEY (Nro_Incidencia),
        CONSTRAINT FK_Incidencias_Clientes FOREIGN KEY (DNI) REFERENCES dbo.Clientes (DNI),
        CONSTRAINT CK_Incidencias_Motivo
            CHECK (Motivo IN ('RECLAMO', 'PEDIDO DE INFO', 'DENUNCIA', 'OTRO')),
        CONSTRAINT CK_Incidencias_Estado
            CHECK (Estado IN ('PENDIENTE', 'DESPACHADO', 'SOLUCIONADO', 'CERRADO', 'ANULADO'))
    );
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Incidencias')
      AND name = N'IX_Incidencias_FechaHora'
)
BEGIN
    CREATE INDEX IX_Incidencias_FechaHora ON dbo.Incidencias (FechaHora);
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Incidencias')
      AND name = N'IX_Incidencias_DNI_FechaHora'
)
BEGIN
    CREATE INDEX IX_Incidencias_DNI_FechaHora ON dbo.Incidencias (DNI, FechaHora DESC);
END
GO

IF OBJECT_ID(N'dbo.AuditoriaIncidencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaIncidencias
    (
        IdAuditoria     BIGINT IDENTITY(1,1) NOT NULL,
        Nro_Incidencia  NUMERIC(18,0) NOT NULL,
        EstadoAnterior  VARCHAR(30) NOT NULL,
        EstadoNuevo     VARCHAR(30) NOT NULL,
        FechaCambio     DATETIME NOT NULL CONSTRAINT DF_AuditoriaIncidencias_FechaCambio DEFAULT (GETDATE()),
        Usuario         NVARCHAR(128) NOT NULL CONSTRAINT DF_AuditoriaIncidencias_Usuario DEFAULT (SUSER_SNAME()),
        CONSTRAINT PK_AuditoriaIncidencias PRIMARY KEY (IdAuditoria),
        CONSTRAINT FK_AuditoriaIncidencias_Incidencias
            FOREIGN KEY (Nro_Incidencia) REFERENCES dbo.Incidencias (Nro_Incidencia)
    );
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AuditoriaIncidencias')
      AND name = N'IX_AuditoriaIncidencias_Nro_Incidencia'
)
BEGIN
    CREATE INDEX IX_AuditoriaIncidencias_Nro_Incidencia
        ON dbo.AuditoriaIncidencias (Nro_Incidencia, FechaCambio);
END
GO
