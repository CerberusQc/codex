using Codex.Host.Modules;
using Codex.ModuleSDK;

namespace Codex.Host.Tests.Modules;

public class ModuleRegistryTests
{
    private static ModuleEntry MakeEntry(string id) => new()
    {
        Id = id,
        DisplayName = "Test Module",
        Version = "1.0",
        PageRoute = $"/{id}",
        ManifestHash = "abc123"
    };

    [Fact]
    public void Register_adds_entry_with_discovered_status()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));

        var entry = registry.Get("mod1");
        Assert.NotNull(entry);
        Assert.Equal(ModuleStatus.Discovered, entry.Status);
    }

    [Fact]
    public void UpdateStatus_changes_status()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));

        registry.UpdateStatus("mod1", ModuleStatus.Building);

        Assert.Equal(ModuleStatus.Building, registry.Get("mod1")!.Status);
    }

    [Fact]
    public void UpdateStatus_sets_LoadedAt_when_Loaded()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));

        var before = DateTimeOffset.UtcNow;
        registry.UpdateStatus("mod1", ModuleStatus.Loaded);
        var after = DateTimeOffset.UtcNow;

        var entry = registry.Get("mod1")!;
        Assert.Equal(ModuleStatus.Loaded, entry.Status);
        Assert.NotNull(entry.LoadedAt);
        Assert.InRange(entry.LoadedAt!.Value, before, after);
    }

    [Fact]
    public void GetRouteMap_returns_null_for_non_loaded_module()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));
        // Status is Discovered by default
        var routeMap = registry.GetRouteMap("mod1");
        Assert.Null(routeMap);
    }

    [Fact]
    public void GetRouteMap_returns_routeMap_when_status_is_loaded()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));

        var map = new ModuleRouteMap();
        var disposable = new NoopDisposable();
        registry.SetRouteMap("mod1", map, disposable);
        registry.UpdateStatus("mod1", ModuleStatus.Loaded);

        Assert.Same(map, registry.GetRouteMap("mod1"));
    }

    [Fact]
    public void Remove_deletes_entry()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));

        registry.Remove("mod1");

        Assert.Null(registry.Get("mod1"));
    }

    [Fact]
    public void All_returns_all_registered_entries()
    {
        var registry = new ModuleRegistry();
        registry.Register(MakeEntry("mod1"));
        registry.Register(MakeEntry("mod2"));
        registry.Register(MakeEntry("mod3"));

        var all = registry.All().ToList();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, e => e.Id == "mod1");
        Assert.Contains(all, e => e.Id == "mod2");
        Assert.Contains(all, e => e.Id == "mod3");
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
