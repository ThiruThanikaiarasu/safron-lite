namespace Saffron.Services;

public sealed record RateOrder(decimal BaseRate, decimal Multiplier, int TargetValue, decimal Divisor);

public static class RateCalculator
{
    public const int Decimals = 2;

    public static decimal ComputeRate(int value, RateOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        decimal raw = order.BaseRate + ((value - order.TargetValue) / order.Divisor) * order.Multiplier;
        if (raw < 0m)
            raw = 0m;

        return Math.Round(raw, Decimals);
    }

    public static decimal ValueForRate(decimal rate, RateOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Multiplier == 0m)
            throw new ArgumentException("Multiplier must be non-zero", nameof(order));

        return order.TargetValue + ((rate - order.BaseRate) / order.Multiplier) * order.Divisor;
    }
}
