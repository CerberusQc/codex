using Codex.ModuleSDK;

namespace Codex.Host.Modules;

public class ModuleEntry
{
    public string Id { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string PageRoute { get; set; } = default!;
    public string ManifestHash { get; set; } = default!;
    public ModuleStatus Status { get; set; } = ModuleStatus.Discovered;
    public string? BuildLog { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public ModuleRouteMap? RouteMap { get; set; }
    public IDisposable? LoadContext { get; set; }
}
