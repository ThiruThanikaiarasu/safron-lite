-- Tenant database. One copy per facility group; all copies share this schema.
-- sqlcmd variables: TenantDb
SET NOCOUNT ON;
GO
CREATE DATABASE [$(TenantDb)];
GO
USE [$(TenantDb)];
GO
CREATE USER saffron_app FOR LOGIN saffron_app;
ALTER ROLE db_datareader ADD MEMBER saffron_app;
ALTER ROLE db_datawriter ADD MEMBER saffron_app;
GRANT EXECUTE TO saffron_app;
GO

CREATE TABLE dbo.Hospital (
    HospitalId   INT           NOT NULL PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Unit (
    UnitId       INT           NOT NULL PRIMARY KEY,
    HospitalId   INT           NOT NULL REFERENCES dbo.Hospital(HospitalId),
    Code         VARCHAR(10)   NOT NULL,
    Name         NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.AppUser (
    UserId       INT           NOT NULL PRIMARY KEY,
    Username     VARCHAR(50)   NOT NULL UNIQUE,
    PasswordHash VARCHAR(100)  NOT NULL,
    DisplayName  NVARCHAR(100) NOT NULL,
    RoleName     VARCHAR(20)   NOT NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    LastLoginUtc DATETIME2(0)  NULL
);

CREATE TABLE dbo.Patient (
    PatientId    INT           NOT NULL PRIMARY KEY,
    Mrn          VARCHAR(20)   NOT NULL UNIQUE,
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    DateOfBirth  DATE          NOT NULL,
    UnitId       INT           NOT NULL REFERENCES dbo.Unit(UnitId),
    Bed          VARCHAR(10)   NOT NULL,
    IsActive     BIT           NOT NULL DEFAULT 1
);

CREATE TABLE dbo.InfusionOrder (
    OrderId          INT IDENTITY(1,1) PRIMARY KEY,
    PatientId        INT           NOT NULL REFERENCES dbo.Patient(PatientId),
    Status           VARCHAR(20)   NOT NULL,          -- Active / Discontinued
    BaseRate         DECIMAL(6,3)  NOT NULL,
    Multiplier       DECIMAL(6,3)  NOT NULL,
    TargetValue      INT           NOT NULL,
    Divisor          DECIMAL(6,2)  NOT NULL,
    OrderedBy        INT           NOT NULL,
    OrderedAtUtc     DATETIME2(0)  NOT NULL,
    DiscontinuedBy   INT           NULL,
    DiscontinuedAtUtc DATETIME2(0) NULL
);

CREATE TABLE dbo.GlucoseReading (
    ReadingId    INT IDENTITY(1,1) PRIMARY KEY,
    PatientId    INT           NOT NULL REFERENCES dbo.Patient(PatientId),
    Value        INT           NOT NULL,
    TakenAtUtc   DATETIME2(0)  NOT NULL,
    Rate         DECIMAL(5,1)  NULL,
    IsLow        BIT           NOT NULL DEFAULT 0,
    EnteredBy    INT           NULL,
    EnteredAtUtc DATETIME2(0)  NOT NULL DEFAULT SYSUTCDATETIME(),
    Comment      NVARCHAR(200) NULL
);
CREATE INDEX IX_GlucoseReading_Patient ON dbo.GlucoseReading (PatientId, TakenAtUtc DESC);

CREATE TABLE dbo.Setting (
    Name         VARCHAR(50)   NOT NULL PRIMARY KEY,
    Value        VARCHAR(100)  NOT NULL
);
GO
