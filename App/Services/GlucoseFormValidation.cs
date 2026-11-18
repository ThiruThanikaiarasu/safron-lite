using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Saffron.Services;

public static class GlucoseFormValidation
{
    private const string ValueLabel = "Glucose value";
    private const string ConfirmLabel = "Confirm value";
    private const string TimeLabel = "Time taken";
    private const string CommentLabel = "Comment";
    private const int CommentMax = 200;

    private static readonly Regex IntegerPattern = new("^[-+]?[0-9]+$", RegexOptions.CultureInvariant);

    public static string Raw(IFormCollection form, string key) => form[key].ToString();

    public static List<string> Validate(IFormCollection form, bool saving)
    {
        var errors = new List<string>();

        if (saving)
        {
            RequireAck(form, "ack_identity", "Patient identity check", errors);
            RequireAck(form, "ack_value", "Value double-check", errors);
            if (Raw(form, "api_low") == "1")
                RequireAck(form, "ack_low", "Low glucose acknowledgement", errors);
        }

        var value = Raw(form, "value");
        if (PhpCompat.Trim(value) == "")
            errors.Add($"The {ValueLabel} field is required.");
        else if (!IntegerPattern.IsMatch(value))
            errors.Add($"The {ValueLabel} field must contain an integer.");
        else if (PhpCompat.ParseInteger(value) < 20)
            errors.Add($"The {ValueLabel} field must contain a number greater than or equal to 20.");
        else if (PhpCompat.ParseInteger(value) > 600)
            errors.Add($"The {ValueLabel} field must contain a number less than or equal to 600.");

        var confirm = Raw(form, "confirm_value");
        if (PhpCompat.Trim(confirm) == "")
            errors.Add($"The {ConfirmLabel} field is required.");
        else if (confirm != value)
            errors.Add($"The {ConfirmLabel} field does not match the {ValueLabel} field.");

        var time = Raw(form, "entered_time");
        if (time.Length != 0 && !PhpCompat.IsHhmm(time))
            errors.Add($"The {TimeLabel} must be HH:MM (24 hour).");

        if (Raw(form, "comment").EnumerateRunes().Count() > CommentMax)
            errors.Add($"The {CommentLabel} field cannot exceed {CommentMax} characters in length.");

        return errors;
    }

    private static void RequireAck(IFormCollection form, string key, string label, List<string> errors)
    {
        if (PhpCompat.Trim(Raw(form, key)) == "")
            errors.Add($"The {label} field is required.");
    }
}
