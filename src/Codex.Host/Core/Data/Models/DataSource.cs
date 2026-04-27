namespace Codex.Host.Core.Data.Models;

public class DataSource
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string CredentialsJson { get; set; } = default!;
}
