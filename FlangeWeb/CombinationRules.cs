namespace FlangeWeb;
public static class CombinationRules
{
    public static string? GetWorkbookKey(string standard, string pressure, string dn, string face, string type,
        bool insulation, ISet<string> genericKeys, ISet<string> allKeys, bool insulationDimensionsAvailable)
    {
        var faceCode = face switch { "RF" => "RF", "F" or "M" => "MF", "RJ" => "RJ", _ => null };
        var typeCode = type switch { "整体法兰" => "IF", "对焊法兰" => "WN", "承插焊法兰" => "SW", _ => null };
        if (faceCode is null || typeCode is null) return null;
        // Jacket choices are validated against their actual face-specific data row by
        // the lookup endpoint. The legacy workbook key list omits jacket F/M and RJ.
        if (standard.EndsWith("-夹套", StringComparison.Ordinal))
            return typeCode == "IF" && insulation ? standard + dn + pressure + faceCode + typeCode : null;
        if (standard == "GB9124.1-19" && pressure == "PN250" && dn == "DN300" && faceCode == "RF" && typeCode != "IF") return null;
        var key = standard + dn + pressure + faceCode;
        if (genericKeys.Contains(key)) return allKeys.Contains(key) ? key : null;
        if (insulation && (typeCode != "IF" || !insulationDimensionsAvailable)) return null;
        var typed = key + typeCode;
        return allKeys.Contains(typed) ? typed : null;
    }
}
