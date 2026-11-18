namespace Saffron.Models;

public sealed record PatientListRow(int PatientId, string Mrn, string FirstName, string LastName, string Bed, string UnitCode, string UnitName);

public sealed record PatientRecord(
    int PatientId,
    string Mrn,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Bed,
    string UnitCode,
    string UnitName,
    string HospitalName,
    DateTime? LastReadingUtc,
    int? MinutesSinceLastReading);

public sealed record OrderRecord(
    int OrderId,
    int PatientId,
    string Status,
    decimal BaseRate,
    decimal Multiplier,
    int TargetValue,
    decimal Divisor,
    DateTime OrderedAtUtc,
    DateTime? DiscontinuedAtUtc,
    string? OrderedByName);

public sealed record ReadingRow(int ReadingId, int Value, DateTime TakenAtUtc, decimal? Rate, bool IsLow, string? EnteredByName, string? Comment);

public sealed record UserRecord(int UserId, string Username, string PasswordHash, string DisplayName, string RoleName, bool IsActive);
