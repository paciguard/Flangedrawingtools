var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/data", async (IWebHostEnvironment env) => Results.Text(await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Data", "flanges.json")), "application/json; charset=utf-8"));
app.MapGet("/api/options", async (IWebHostEnvironment env) => Results.Text(await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Data", "options.json")), "application/json; charset=utf-8"));
app.Run();
