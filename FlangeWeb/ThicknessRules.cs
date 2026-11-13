namespace FlangeWeb;

public static class ThicknessRules
{
    // Formulas transcribed from the workbook's twelve drawing templates.
    public static decimal? Calculate(string standard, string pressure, string dn, string face, string type,
        decimal? otherThickness, decimal? integralThickness, decimal? rfHeight,
        decimal? f1, decimal? f2, decimal? ringFaceHeight)
    {
        // Jacket rows use the same integral-flange thickness conventions as their base standard.
        var jacket = standard.EndsWith("-夹套", StringComparison.Ordinal);
        if (jacket) standard = standard[..^3];
        var isClass = pressure.StartsWith("Class", StringComparison.OrdinalIgnoreCase);
        if (face == "RF")
        {
            var c = type == "整体法兰" ? integralThickness : otherThickness;
            return isClass ? Add(c, rfHeight) : c;
        }
        var integral = type == "整体法兰";
        var baseThickness = integral ? integralThickness : otherThickness;
        if (face == "F") return isClass ? baseThickness : Subtract(baseThickness, f1);
        if (face == "M")
        {
            if (integral)
            {
                var c = jacket || standard is "GB9124.1-19" or "HG20592-09" ? integralThickness : otherThickness;
                return isClass ? c : Subtract(c, f2);
            }
            if (type == "对焊法兰") return isClass ? Add(otherThickness, f2) : otherThickness;
            return isClass ? otherThickness : Subtract(otherThickness, f2);
        }
        if (face == "RJ")
        {
            var exception = type != "承插焊法兰" && (standard == "GB9124.1-19" || standard == "HG20592-09") && dn == "DN125";
            if (isClass || exception) return baseThickness;
            return Subtract(baseThickness, ringFaceHeight);
        }
        return null;
    }
    public static decimal? Height(string standard, string pressure, string dn, string face, string type,
        decimal? height, decimal? rfHeight, decimal? f1, decimal? f2, decimal? ringGrooveDepth)
    {
        if (type == "整体法兰") return null;
        var isClass = pressure.StartsWith("Class", StringComparison.OrdinalIgnoreCase);
        if (face == "RF") return isClass ? Add(height, rfHeight) : height;
        if (face == "F") return isClass ? height : Subtract(height, f1);
        if (face == "M")
            return type == "对焊法兰" ? (isClass ? Add(height, f2) : height) : (isClass ? height : Subtract(height, f2));
        if (face == "RJ")
        {
            var exception = type == "对焊法兰" && (standard == "GB9124.1-19" || standard == "HG20592-09") && dn == "DN125";
            return isClass || exception ? height : Subtract(height, ringGrooveDepth);
        }
        return null;
    }
    private static decimal? Add(decimal? a, decimal? b) => a.HasValue && b.HasValue ? a + b : null;
    private static decimal? Subtract(decimal? a, decimal? b) => a.HasValue && b.HasValue ? a - b : null;
}
