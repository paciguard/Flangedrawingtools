using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/data", async (IWebHostEnvironment env) => {
    var path = Path.Combine(env.ContentRootPath, "Data", "flanges.json");
    return Results.Text(await File.ReadAllTextAsync(path), "application/json; charset=utf-8");
});
app.Run();
