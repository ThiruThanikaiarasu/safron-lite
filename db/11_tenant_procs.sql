-- Stored procedures for the tenant databases.
-- sqlcmd variables: TenantDb
USE [$(TenantDb)];
GO

CREATE PROCEDURE dbo.usp_User_GetByUsername
    @Username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, PasswordHash, DisplayName, RoleName, IsActive
    FROM dbo.AppUser
    WHERE Username = @Username;
END
GO

CREATE PROCEDURE dbo.usp_User_TouchLogin
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.AppUser SET LastLoginUtc = SYSUTCDATETIME() WHERE UserId = @UserId;
END
GO

CREATE PROCEDURE dbo.usp_Patient_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PatientId, p.Mrn, p.FirstName, p.LastName, p.Bed,
           u.Code AS UnitCode, u.Name AS UnitName
    FROM dbo.Patient p
    JOIN dbo.Unit u ON u.UnitId = p.UnitId
    WHERE p.IsActive = 1
    ORDER BY u.Code, p.Bed;
END
GO

CREATE PROCEDURE dbo.usp_Patient_Get
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PatientId, p.Mrn, p.FirstName, p.LastName, p.DateOfBirth, p.Bed,
           u.Code AS UnitCode, u.Name AS UnitName, h.Name AS HospitalName,
           (SELECT MAX(r.TakenAtUtc) FROM dbo.GlucoseReading r WHERE r.PatientId = p.PatientId) AS LastReadingUtc,
           DATEDIFF(MINUTE,
                    (SELECT MAX(r.TakenAtUtc) FROM dbo.GlucoseReading r WHERE r.PatientId = p.PatientId),
                    GETDATE()) AS MinutesSinceLastReading
    FROM dbo.Patient p
    JOIN dbo.Unit u ON u.UnitId = p.UnitId
    JOIN dbo.Hospital h ON h.HospitalId = u.HospitalId
    WHERE p.PatientId = @PatientId;
END
GO

CREATE PROCEDURE dbo.usp_Reading_History
    @PatientId INT,
    @Top INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    IF @Top IS NULL OR @Top < 1 SET @Top = 20;
    IF @Top > 100 SET @Top = 100;

    SELECT TOP (@Top) r.ReadingId, r.Value, r.TakenAtUtc, r.Rate, r.IsLow, r.Comment,
           u.DisplayName AS EnteredByName
    FROM dbo.GlucoseReading r
    LEFT JOIN dbo.AppUser u ON u.UserId = r.EnteredBy
    WHERE r.PatientId = @PatientId
    ORDER BY r.TakenAtUtc DESC, r.ReadingId DESC;
END
GO

CREATE PROCEDURE dbo.usp_Reading_Insert
    @PatientId  INT,
    @Value      INT,
    @TakenAtUtc DATETIME2(0),
    @Rate       DECIMAL(6,2),
    @EnteredBy  INT,
    @Comment    NVARCHAR(200) = NULL,
    @ReadingId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @lowLimit INT = (SELECT CAST(Value AS INT) FROM dbo.Setting WHERE Name = 'LowThreshold');
    DECLARE @interval INT = (SELECT CAST(Value AS INT) FROM dbo.Setting WHERE Name = 'CorrectionIntervalMinutes');

    IF @Rate > 0 AND EXISTS (
        SELECT 1 FROM dbo.GlucoseReading
        WHERE PatientId = @PatientId
          AND Rate > 0
          AND DATEDIFF(MINUTE, TakenAtUtc, GETDATE()) < @interval)
    BEGIN
        RAISERROR('Correction interval has not elapsed for this patient.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.GlucoseReading (PatientId, Value, TakenAtUtc, Rate, IsLow, EnteredBy, Comment)
    VALUES (@PatientId, @Value, @TakenAtUtc, @Rate,
            CASE WHEN @Value < @lowLimit THEN 1 ELSE 0 END,
            @EnteredBy, @Comment);

    SET @ReadingId = SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE dbo.usp_Order_GetActive
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 OrderId, PatientId, Status, BaseRate, Multiplier, TargetValue, Divisor, OrderedAtUtc
    FROM dbo.InfusionOrder
    WHERE PatientId = @PatientId AND Status = 'Active'
    ORDER BY OrderedAtUtc DESC;
END
GO

CREATE PROCEDURE dbo.usp_Order_GetLatest
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 o.OrderId, o.PatientId, o.Status, o.BaseRate, o.Multiplier, o.TargetValue, o.Divisor,
           o.OrderedAtUtc, o.DiscontinuedAtUtc, u.DisplayName AS OrderedByName
    FROM dbo.InfusionOrder o
    LEFT JOIN dbo.AppUser u ON u.UserId = o.OrderedBy
    WHERE o.PatientId = @PatientId
    ORDER BY o.OrderedAtUtc DESC, o.OrderId DESC;
END
GO

CREATE PROCEDURE dbo.usp_Order_Discontinue
    @OrderId INT,
    @UserId  INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.InfusionOrder
    SET Status = 'Discontinued', DiscontinuedBy = @UserId, DiscontinuedAtUtc = SYSUTCDATETIME()
    WHERE OrderId = @OrderId AND Status = 'Active';
    SELECT @@ROWCOUNT AS Affected;
END
GO

CREATE PROCEDURE dbo.usp_Order_Resume
    @OrderId INT,
    @UserId  INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @pid INT = (SELECT PatientId FROM dbo.InfusionOrder WHERE OrderId = @OrderId);
    IF @pid IS NULL OR EXISTS (SELECT 1 FROM dbo.InfusionOrder WHERE PatientId = @pid AND Status = 'Active')
    BEGIN
        SELECT 0 AS Affected;
        RETURN;
    END
    UPDATE dbo.InfusionOrder
    SET Status = 'Active', DiscontinuedBy = NULL, DiscontinuedAtUtc = NULL
    WHERE OrderId = @OrderId AND Status = 'Discontinued';
    SELECT @@ROWCOUNT AS Affected;
END
GO
