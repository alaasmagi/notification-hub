using System.Globalization;

namespace Contracts.External.Models;

/// <summary>Formats machine-readable dates, times and amounts for the recipient locale.</summary>
public class LocaleFormatter
{
    private readonly CultureInfo _culture;

    public LocaleFormatter(string? locale)
    {
        _culture = ResolveCulture(locale);
    }

    public string LocaleName => _culture.Name;

    public string Date(string? isoDate)
    {
        if (DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date.ToString("d", _culture);
        }

        return isoDate ?? string.Empty;
    }

    public string Month(string? isoMonth)
    {
        if (DateOnly.TryParseExact(isoMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date.ToString("MMMM yyyy", _culture);
        }

        return isoMonth ?? string.Empty;
    }

    public string Time(string? wallClock)
    {
        if (TimeOnly.TryParseExact(wallClock, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            return time.ToString("t", _culture);
        }

        return wallClock ?? string.Empty;
    }

    public string Amount(string? invariantDecimal)
    {
        if (decimal.TryParse(invariantDecimal, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var value))
        {
            return value.ToString("N2", _culture);
        }

        return invariantDecimal ?? string.Empty;
    }

    private static CultureInfo ResolveCulture(string? locale)
    {
        if (!string.IsNullOrWhiteSpace(locale))
        {
            try
            {
                return CultureInfo.GetCultureInfo(locale.Trim());
            }
            catch (CultureNotFoundException)
            {
                // Fall through to the default below.
            }
        }

        return CultureInfo.GetCultureInfo("en");
    }
}
