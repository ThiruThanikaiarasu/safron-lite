namespace Saffron.Services;

public static class GlucoseRules
{
    public const int MinValue = 20;
    public const int MaxValue = 600;
    public const int LowLimit = 72;
    public const int HighRepeatLimit = 300;

    public static bool InRange(int value) => value >= MinValue && value <= MaxValue;

    public static bool IsLow(int value) => value < LowLimit;

    public static bool NeedsRepeat(int value) => value >= HighRepeatLimit;
}
