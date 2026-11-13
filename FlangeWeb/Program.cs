using System.Globalization;
using System.Text.Json.Nodes;
using FlangeWeb;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles(); app.UseStaticFiles();
string FileInData(string name) => Path.Combine(app.Environment.ContentRootPath, "Data", name);
app.MapGet("/api/health", () => Results.Text("FlangeWeb", "text/plain"));
app.MapPost("/api/quit", (HttpContext context, IHostApplicationLifetime lifetime) => {
    if (!ShutdownPolicy.IsAllowed(context)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    context.Response.OnCompleted(() => { lifetime.StopApplication(); return Task.CompletedTask; });
    return Results.Ok(new { stopped = true });
});
app.MapGet("/api/data", async () => Results.Text(await File.ReadAllTextAsync(FileInData("flanges.json")), "application/json; charset=utf-8"));
app.MapGet("/api/counterbores", async () => Results.Text(await File.ReadAllTextAsync(FileInData("counterbores.json")), "application/json; charset=utf-8"));
app.MapGet("/api/annotation-bindings", async () => Results.Text(await File.ReadAllTextAsync(FileInData("annotation-bindings.json")), "application/json; charset=utf-8"));
app.MapGet("/api/drawing-metadata", async () => Results.Text(await File.ReadAllTextAsync(FileInData("drawing-metadata.json")), "application/json; charset=utf-8"));
app.MapGet("/api/options", async () => Results.Text(await File.ReadAllTextAsync(FileInData("options.json")), "application/json; charset=utf-8"));
app.MapGet("/api/lookup", async (string standard, string pressure, string dn, string face, string type, string? insulation) => {
    var kind = face.EndsWith("/RF") || face.EndsWith("/B") ? "RF" : face.EndsWith("/F") ? "F" : face.EndsWith("/M") || face.EndsWith("/E") ? "M" : face.EndsWith("/RJ") || face.EndsWith("/J") ? "RJ" : "";
    if (kind == "" || type != "整体法兰") return Results.BadRequest(new { error = "此工具只支持整体法兰（IF）及其密封面" });
    string sheet = kind == "RF" ? "突面法兰数据源" : kind == "RJ" ? "环连接面法兰数据源" : "凹凸面法兰数据源";
    var root = JsonNode.Parse(await File.ReadAllTextAsync(FileInData("flanges.json")))!;
    var table = root[sheet]!; var rows = table["rows"]!.AsArray();
    var hits = rows.Where(r => r?[1]?.ToString() == standard && r?[2]?.ToString() == pressure && r?[3]?.ToString() == dn).ToList();
    if (hits.Count != 1) return Results.NotFound(new { error = hits.Count == 0 ? "数据源中没有该组合" : "数据源中存在重复组合" });
    var validation = JsonNode.Parse(await File.ReadAllTextAsync(FileInData("workbook-validation.json")))!;
    var genericKeys = validation["genericKeys"]!.AsArray().Select(x => x!.ToString()).ToHashSet(StringComparer.Ordinal);
    var allKeys = validation["allKeys"]!.AsArray().Select(x => x!.ToString()).ToHashSet(StringComparer.Ordinal);
    var insulationRows = root["保温法兰数据源"]!["rows"]!.AsArray().Where(r => r?[1]?.ToString() == pressure && r?[2]?.ToString() == dn).ToList();
    var insulationOk = insulationRows.Count == 1 && Enumerable.Range(3,3).All(i => double.TryParse(insulationRows[0]?[i]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)) && Enumerable.Range(6,2).All(i => !string.IsNullOrWhiteSpace(insulationRows[0]?[i]?.ToString()) && insulationRows[0]?[i]?.ToString() != "—");
    var key = CombinationRules.GetWorkbookKey(standard, pressure, dn, kind, type, insulation == "有", genericKeys, allKeys, insulationOk);
    if (key is null) return Results.BadRequest(new { error = "工作簿数据验证未列出此组合，或保温尺寸不完整" });
    var row = hits[0]!;
    decimal? Number(int index) => decimal.TryParse(row[index]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    var displayedThickness = ThicknessRules.Calculate(standard, pressure, dn, kind, type, Number(8), Number(9), kind == "RF" ? Number(11) : null, kind is "F" or "M" ? Number(23) : null, kind is "F" or "M" ? Number(24) : null, kind == "RJ" ? Number(27) : null);
    var heightIndex = kind == "RF" ? (type == "对焊法兰" ? 19 : 20) : (type == "对焊法兰" ? 18 : 19);
    var displayedHeight = ThicknessRules.Height(standard, pressure, dn, kind, type, Number(heightIndex), kind == "RF" ? Number(11) : null, kind is "F" or "M" ? Number(23) : null, kind is "F" or "M" ? Number(24) : null, kind == "RJ" ? Number(25) : null);
    return Results.Ok(new { sheet, headers = table["headers"], row, displayedThickness, displayedHeight, note = "尺寸试算待核对，不可用于制造" });
});
app.Run();



