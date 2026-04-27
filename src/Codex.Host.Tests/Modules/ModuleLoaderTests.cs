using Codex.Host.Core.Data;
using Codex.Host.Core.Services;
using Codex.Host.Modules;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Codex.Host.Tests.Modules;

public class ModuleLoaderTests
{
    private static IServiceProvider CreateHostSp()
    {
        var opts = new DbContextOptionsBuilder<CodexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var services = new ServiceCollection();
        services.AddSingleton(new CodexDbContext(opts));
        services.AddScoped<DataSourceService>(sp =>
            new DataSourceService(sp.GetRequiredService<CodexDbContext>(), new EphemeralDataProtectionProvider()));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void TryLoad_returns_false_when_path_not_found()
    {
        var registry = new ModuleRegistry();
        registry.Register(new ModuleEntry
        {
            Id = "test", DisplayName = "Test", Version = "1.0",
            PageRoute = "/test", ManifestHash = "x"
        });

        var loader = new ModuleLoader(registry, NullLogger<ModuleLoader>.Instance);
        var result = loader.TryLoad("test", "/nonexistent/path", CreateHostSp());

        Assert.False(result);
        Assert.Equal(ModuleStatus.LoadFailed, registry.Get("test")!.Status);
    }

    [Fact]
    public void TryLoad_returns_false_for_unknown_moduleId()
    {
        var registry = new ModuleRegistry();
        var loader = new ModuleLoader(registry, NullLogger<ModuleLoader>.Instance);
        var result = loader.TryLoad("nonexistent", "/some/path", CreateHostSp());
        Assert.False(result);
    }

    [Fact]
    public void Unload_removes_entry_from_registry()
    {
        var registry = new ModuleRegistry();
        registry.Register(new ModuleEntry
        {
            Id = "test", DisplayName = "Test", Version = "1.0",
            PageRoute = "/test", ManifestHash = "x"
        });

        var loader = new ModuleLoader(registry, NullLogger<ModuleLoader>.Instance);
        loader.Unload("test");

        Assert.Null(registry.Get("test"));
    }
}
