namespace FlangeWeb;

public static class ThicknessRules
{
    // Implements only formulas verified against the IF templates; missing inputs stay missing.
    public static decimal? Calculate(string standard, string pressure, string dn, string face, string type,
        decimal? otherThickness, decimal? integralThickness, decimal? rfHeight,
        decimal? f1, decimal? f2, decimal? ringFaceHeight)
    {
        var isClass = pressure.StartsWith("Class", StringComparison.OrdinalIgnoreCase);
        if (face == "RF")
        {
            var c = type == "整体法兰" ? integralThickness : otherThickness;
            return isClass ? Add(c, rfHeight) : c;
        }
        if (type != "整体法兰") return null;
        if (face == "F") return isClass ? integralThickness : Subtract(integralThickness, f1);
        if (face == "M")
        {
            var c = standard is "GB9124.1-19" or "HG20592-09" ? integralThickness : otherThickness;
            return isClass ? c : Subtract(c, f2);
        }
        if (face == "RJ")
        {
            if (isClass || ((standard == "GB9124.1-19" || standard == "HG20592-09") && dn == "DN125")) return integralThickness;
            return Subtract(integralThickness, ringFaceHeight);
        }
        return null;
    }
    private static decimal? Add(decimal? a, decimal? b) => a.HasValue && b.HasValue ? a + b : null;
    private static decimal? Subtract(decimal? a, decimal? b) => a.HasValue && b.HasValue ? a - b : null;
}
