-- Drops and recreates the Saffron databases and the application login.
-- Run by init.sh. Uses sqlcmd variables: AppPassword
SET NOCOUNT ON;
GO
IF DB_ID('SaffronCatalog') IS NOT NULL
BEGIN
    ALTER DATABASE SaffronCatalog SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SaffronCatalog;
END
GO
IF DB_ID('Saffron_Alder') IS NOT NULL
BEGIN
    ALTER DATABASE Saffron_Alder SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Saffron_Alder;
END
GO
IF DB_ID('Saffron_Birch') IS NOT NULL
BEGIN
    ALTER DATABASE Saffron_Birch SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Saffron_Birch;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'saffron_app')
    CREATE LOGIN saffron_app WITH PASSWORD = '$(AppPassword)', CHECK_POLICY = OFF, DEFAULT_DATABASE = master;
GO
