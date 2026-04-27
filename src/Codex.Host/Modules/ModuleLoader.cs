using System.Reflection;
using Codex.Host.Core.Services;
using Codex.ModuleSDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Codex.Host.Modules;

public class ModuleLoader(ModuleRegistry registry, ILogger<ModuleLoader> logger)
{
    public bool TryLoad(string moduleId, string builtBackendPath, IServiceProvider hostServiceProvider)
    {
        var entry = registry.Get(moduleId);
        if (entry is null) return false;

        try
        {
            registry.UpdateStatus(moduleId, ModuleStatus.Building);

            if (!Directory.Exists(builtBackendPath))
            {
                registry.UpdateStatus(moduleId, ModuleStatus.LoadFailed, "Backend output path not found.");
                return false;
            }

            var dlls = Directory.GetFiles(builtBackendPath, "*.dll")
                .Where(f => !IsSystemAssembly(Path.GetFileName(f)))
                .ToArray();

            if (dlls.Length == 0)
            {
                registry.UpdateStatus(moduleId, ModuleStatus.LoadFailed, "No module DLL found in output path.");
                return false;
            }

            // Unload previous ALC if any
            entry.LoadContext?.Dispose();
            entry.LoadContext = null;

            var alc = new CodexAssemblyLoadContext(moduleId, builtBackendPath);

            ICodexModule? module = null;
            foreach (var dll in dlls)
            {
                try
                {
                    var asm = alc.LoadFromAssemblyPath(dll);
                    var moduleType = asm.GetTypes()
                        .FirstOrDefault(t => typeof(ICodexModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    if (moduleType is not null)
                    {
                        module = (ICodexModule)Activator.CreateInstance(moduleType)!;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Skipping {Dll} — no ICodexModule found", dll);
                }
            }

            if (module is null)
            {
                alc.Dispose();
                registry.UpdateStatus(moduleId, ModuleStatus.LoadFailed, "No ICodexModule implementation found.");
                return false;
            }

            // Check datasource requirements
            var dsSvc = hostServiceProvider.GetRequiredService<DataSourceService>();
            var missingDs = new List<string>();
            foreach (var req in module.Requires)
            {
                var creds = dsSvc.GetCredentialsAsync(req.Id).GetAwaiter().GetResult();
                if (creds is null)
                    missingDs.Add(req.Id);
            }

            if (missingDs.Count > 0)
            {
                alc.Dispose();
                registry.UpdateStatus(moduleId, ModuleStatus.MissingDatasource,
                    $"Missing datasources: {string.Join(", ", missingDs)}");
                return false;
            }

            // Build module's isolated service container
            var moduleServices = new ServiceCollection();
            InjectDatasources(module, moduleServices, dsSvc);
            module.Register(moduleServices);
            var moduleSp = moduleServices.BuildServiceProvider();

            // Build route map using the module's service provider
            var routeMap = new ModuleRouteMap();
            module.MapRoutes(routeMap);

            registry.SetRouteMap(moduleId, routeMap, new ModuleLoadHandle(alc, moduleSp));
            registry.UpdateStatus(moduleId, ModuleStatus.Loaded);
            logger.LogInformation("Loaded module {ModuleId} v{Version}", moduleId, entry.Version);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load module {ModuleId}", moduleId);
            registry.UpdateStatus(moduleId, ModuleStatus.LoadFailed, ex.Message);
            return false;
        }
    }

    public void Unload(string moduleId)
    {
        registry.UpdateStatus(moduleId, ModuleStatus.Unloading);
        registry.Remove(moduleId);
        logger.LogInformation("Unloaded module {ModuleId}", moduleId);
    }

    private static void InjectDatasources(ICodexModule module, IServiceCollection services, DataSourceService dsSvc)
    {
        foreach (var req in module.Requires)
        {
            var creds = dsSvc.GetCredentialsAsync(req.Id).GetAwaiter().GetResult();
            if (creds is null) continue;

            switch (req.Type)
            {
                case DataSourceType.Postgres:
                    var host = creds.Value.TryGetProperty("host", out var h) ? h.GetString() ?? "localhost" : "localhost";
                    var port = creds.Value.TryGetProperty("port", out var p) ? p.GetInt32() : 5432;
                    var user = creds.Value.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "";
                    var pass = creds.Value.TryGetProperty("password", out var pw) ? pw.GetString() ?? "" : "";
                    var cs = $"Host={host};Port={port};Username={user};Password={pass}";
                    services.AddSingleton(NpgsqlDataSource.Create(cs));
                    break;
                case DataSourceType.Http:
                    var url = creds.Value.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "";
                    services.AddSingleton(new HttpClient { BaseAddress = new Uri(url) });
                    break;
            }
        }
    }

    private static bool IsSystemAssembly(string fileName) =>
        fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
        fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
        fileName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
        fileName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase);
}

// Combines ALC + module ServiceProvider so both are disposed together
file sealed class ModuleLoadHandle(CodexAssemblyLoadContext alc, ServiceProvider sp) : IDisposable
{
    public void Dispose()
    {
        sp.Dispose();
        alc.Dispose();
    }
}
