using Saffron.Services;

namespace Saffron.Models;

public static class CareText
{
    public const string Banner = "Sample application. Not for clinical use.";
}

public sealed record ErrorPageModel(string Heading, string Message);

public sealed record LoginViewModel(string? Error, string Username);

public sealed record PatientDetailViewModel(
    PatientRecord Patient,
    PatientSummary? Summary,
    IReadOnlyList<ReadingDto> History,
    int? Average,
    bool IsAdmin,
    int? SavedReadingId);

public sealed record RecommendationViewModel(
    string RateText,
    bool LowGlucose,
    IReadOnlyList<string> Messages,
    string NextDueLocal);

public sealed record GlucoseForm(string Value, string ConfirmValue, string EnteredTime, string Comment, bool AckLow)
{
    public static readonly GlucoseForm Empty = new("", "", "", "", false);
}

public sealed record GlucoseEntryViewModel(
    PatientRecord Patient,
    RecommendationViewModel? Recommendation,
    string? ApiError,
    IReadOnlyList<string> ValidationErrors,
    GlucoseForm Form);
