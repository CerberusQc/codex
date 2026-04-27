using Codex.Host.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace Codex.Host.Tests.Modules;

public class ModuleBuilderTests
{
    [Fact]
    public async Task BuildAsync_returns_false_when_source_path_missing()
    {
        var builder = new ModuleBuilder(NullLogger<ModuleBuilder>.Instance);
        var (success, log, _) = await builder.BuildAsync("test", "/nonexistent", "/tmp/output");

        Assert.False(success);
        Assert.Contains("not found", log, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_returns_false_when_no_backend_directory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var builder = new ModuleBuilder(NullLogger<ModuleBuilder>.Instance);
            var (success, log, _) = await builder.BuildAsync("test", dir, "/tmp/output");

            Assert.False(success);
            Assert.Contains("No backend", log, StringComparison.OrdinalIgnoreCase);
        }
        finally { Directory.Delete(dir, true); }
    }
}
