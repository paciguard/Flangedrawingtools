namespace FlangeWeb;
public static class CombinationRules
{
    public static string? GetWorkbookKey(string standard, string pressure, string dn, string face, string type,
        bool insulation, ISet<string> genericKeys, ISet<string> allKeys, bool insulationDimensionsAvailable)
    {
        var faceCode = face switch { "RF" => "RF", "F" or "M" => "MF", "RJ" => "RJ", _ => null };
        var typeCode = type switch { "整体法兰" => "IF", "对焊法兰" => "WN", "承插焊法兰" => "SW", _ => null };
        if (faceCode is null || typeCode is null) return null;
        if (standard.Contains("夹套", StringComparison.Ordinal) && typeCode != "IF") return null;
        if (standard == "GB9124.1-19" && pressure == "PN250" && dn == "DN300" && faceCode == "RF" && typeCode != "IF") return null;
        var key = standard + dn + pressure + faceCode;
        if (genericKeys.Contains(key)) return allKeys.Contains(key) ? key : null;
        if (insulation && (typeCode != "IF" || !insulationDimensionsAvailable)) return null;
        var typed = key + typeCode;
        return allKeys.Contains(typed) ? typed : null;
    }
}
