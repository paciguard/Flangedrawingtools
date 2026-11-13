using System.Globalization;
using System.Text.RegularExpressions;

namespace FlangeWeb;

public static class JacketRules
{
    // Jacket IF thickness is the requested drilled-hole depth (1.5 × thread diameter)
    // plus 10 mm. The formula is a design rule, not a claim about the flange standard.
    public static decimal? DisplayedThickness(string standard, string type, decimal? original, string? thread)
    {
        if (!standard.EndsWith("-夹套", StringComparison.Ordinal) || type != "整体法兰") return original;
        var match = Regex.Match(thread ?? "", @"^\s*M\s*(\d+(?:\.\d+)?)(?:\s*[×xX].*)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var diameter) || diameter <= 0) return null;
        return diameter * 1.5m + 10m;
    }
}
