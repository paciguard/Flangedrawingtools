using System.Text.Json.Nodes;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
string FileInData(string name) => Path.Combine(app.Environment.ContentRootPath, "Data", name);
app.MapGet("/api/data", async () => Results.Text(await File.ReadAllTextAsync(FileInData("flanges.json")), "application/json; charset=utf-8"));
app.MapGet("/api/options", async () => Results.Text(await File.ReadAllTextAsync(FileInData("options.json")), "application/json; charset=utf-8"));
app.MapGet("/api/lookup", async (string standard, string pressure, string dn, string face, string type) => {
    var kind = face.EndsWith("/RF") || face.EndsWith("/B") ? "RF" : face.EndsWith("/F") ? "F" : face.EndsWith("/M") || face.EndsWith("/E") ? "M" : face.EndsWith("/RJ") || face.EndsWith("/J") ? "RJ" : "";
    if (kind == "" || !new[]{"整体法兰","对焊法兰","承插焊法兰"}.Contains(type)) return Results.BadRequest(new { error = "不支持的密封面或连接型式" });
    if (standard.Contains("夹套") && type != "整体法兰") return Results.BadRequest(new { error = "夹套配对仅支持整体法兰" });
    string sheet = kind == "RF" ? "突面法兰数据源" : kind == "RJ" ? "环连接面法兰数据源" : "凹凸面法兰数据源";
    var root = JsonNode.Parse(await File.ReadAllTextAsync(FileInData("flanges.json")))!;
    var table = root[sheet]!; var rows = table["rows"]!.AsArray();
    var hits = rows.Where(r => r?[1]?.ToString() == standard && r?[2]?.ToString() == pressure && r?[3]?.ToString() == dn).ToList();
    if (hits.Count != 1) return Results.NotFound(new { error = hits.Count == 0 ? "数据源中没有该组合" : "数据源中存在重复组合" });
    var row = hits[0]!; double? displayedThickness = null;
    if (kind == "RF") {
        int thicknessIndex = type == "整体法兰" ? 9 : 8;
        if (double.TryParse(row[thicknessIndex]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double thickness)) {
            if (pressure.StartsWith("Class", StringComparison.OrdinalIgnoreCase)) {
                if (double.TryParse(row[11]?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double faceHeight)) displayedThickness = thickness + faceHeight;
            } else displayedThickness = thickness;
        }
    }
    return Results.Ok(new { sheet, headers = table["headers"], row, displayedThickness, note = "仅RF厚度试算；数据和绘图尚需核对，不可用于制造" });
});
app.Run();
