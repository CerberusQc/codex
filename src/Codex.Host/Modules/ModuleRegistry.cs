using System.Collections.Concurrent;
using Codex.ModuleSDK;

namespace Codex.Host.Modules;

public class ModuleRegistry
{
    private readonly ConcurrentDictionary<string, ModuleEntry> _entries = new();

    public void Register(ModuleEntry entry) =>
        _entries[entry.Id] = entry;

    public ModuleEntry? Get(string id) =>
        _entries.TryGetValue(id, out var e) ? e : null;

    public IEnumerable<ModuleEntry> All() =>
        _entries.Values;

    public void UpdateStatus(string id, ModuleStatus status, string? buildLog = null)
    {
        if (_entries.TryGetValue(id, out var e))
        {
            e.Status = status;
            if (buildLog is not null) e.BuildLog = buildLog;
            if (status == ModuleStatus.Loaded) e.LoadedAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetRouteMap(string id, ModuleRouteMap routeMap, IDisposable loadContext)
    {
        if (_entries.TryGetValue(id, out var e))
        {
            e.RouteMap = routeMap;
            e.LoadContext = loadContext;
        }
    }

    public ModuleRouteMap? GetRouteMap(string id) =>
        _entries.TryGetValue(id, out var e) && e.Status == ModuleStatus.Loaded ? e.RouteMap : null;

    public void Remove(string id)
    {
        if (_entries.TryRemove(id, out var e))
            e.LoadContext?.Dispose();
    }
}
