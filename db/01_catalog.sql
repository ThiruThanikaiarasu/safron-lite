-- Catalog database: maps a facility group (tenant) to its database.
SET NOCOUNT ON;
GO
CREATE DATABASE SaffronCatalog;
GO
USE SaffronCatalog;
GO
CREATE USER saffron_app FOR LOGIN saffron_app;
ALTER ROLE db_datareader ADD MEMBER saffron_app;
GO
CREATE TABLE dbo.Tenant (
    TenantCode        VARCHAR(20)   NOT NULL PRIMARY KEY,
    DisplayName       NVARCHAR(100) NOT NULL,
    DbName            SYSNAME       NOT NULL,
    FacilityTimeZone  VARCHAR(50)   NOT NULL,
    IsActive          BIT           NOT NULL DEFAULT 1
);
GO
INSERT INTO dbo.Tenant (TenantCode, DisplayName, DbName, FacilityTimeZone) VALUES
 ('alder', N'Alder Valley Health', 'Saffron_Alder', 'America/Chicago'),
 ('birch', N'Birch Point Health',  'Saffron_Birch', 'America/Phoenix');
GO
