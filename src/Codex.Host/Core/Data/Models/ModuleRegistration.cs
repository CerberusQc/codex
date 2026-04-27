namespace Codex.Host.Core.Data.Models;

public class ModuleRegistration
{
    public string Id { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string PageRoute { get; set; } = default!;
    public string Status { get; set; } = "Discovered";
    public string? BuildLog { get; set; }
    public string? LoadedAt { get; set; }
    public string ManifestHash { get; set; } = default!;
}
