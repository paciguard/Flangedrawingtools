using System.Globalization;
using System.Text.RegularExpressions;

namespace FlangeWeb;

public static class JacketRules
{
    // The additional 10 mm is a design allowance requested for jacket IF flanges.
    // It is not a drill-depth specification or a claim about the flange standard.
    public static decimal? DisplayedThickness(string standard, string type, decimal? original, string? thread)
    {
        if (!standard.EndsWith("-夹套", StringComparison.Ordinal) || type != "整体法兰") return original;
        var match = Regex.Match(thread ?? "", @"^\s*M\s*(\d+(?:\.\d+)?)(?:\s*[×xX].*)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var diameter) || diameter <= 0 || original is null) return null;
        var minimum = diameter * 1.5m + 10m;
        return Math.Max(original.Value, minimum);
    }
}
