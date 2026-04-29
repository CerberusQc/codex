using Codex.Host.Core.Data;
using Codex.Host.Core.Services;
using Codex.Host.Modules;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("data/dp-keys"));

builder.Services.AddDbContext<CodexDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=data/codex.db"));

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DataSourceService>();

builder.Services.AddSingleton<ModuleRegistry>();
builder.Services.AddSingleton<ModuleLoader>();
builder.Services.AddSingleton<ModuleBuilder>();
builder.Services.AddHostedService<GitPoller>();

var app = builder.Build();

// Ensure directories exist and run migrations
Directory.CreateDirectory("data");
Directory.CreateDirectory("data/dp-keys");
Directory.CreateDirectory("wwwroot/assets/modules");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CodexDbContext>();
    await db.Database.MigrateAsync();
}

// Module API dispatch middleware
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    const string prefix = "/api/mod/";
    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        var rest = path[prefix.Length..];
        var slash = rest.IndexOf('/');
        var moduleId = slash < 0 ? rest : rest[..slash];
        var relativePath = slash < 0 ? "" : rest[(slash + 1)..];

        var registry = ctx.RequestServices.GetRequiredService<ModuleRegistry>();
        var routeMap = registry.GetRouteMap(moduleId);
        if (routeMap is not null)
        {
            using var scope = ctx.RequestServices.CreateScope();
            var handled = await routeMap.TryHandleAsync(
                ctx.Request.Method, relativePath, ctx, scope.ServiceProvider, ctx.RequestAborted);
            if (handled) return;
        }
    }
    await next(ctx);
});

app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
