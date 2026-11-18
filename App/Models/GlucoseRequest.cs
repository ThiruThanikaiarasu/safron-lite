namespace Saffron.Models;

public sealed class GlucoseRequest
{
    public int PatientId { get; set; }
    public int? Value { get; set; }
    public string? EnteredTime { get; set; }
    public string? Comment { get; set; }
}
