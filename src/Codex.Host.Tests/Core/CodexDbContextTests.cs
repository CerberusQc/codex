using Codex.Host.Core.Data;
using Codex.Host.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Tests.Core;

public class CodexDbContextTests
{
    private static CodexDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<CodexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CodexDbContext(opts);
    }

    [Fact]
    public async Task Can_save_and_retrieve_dashboard()
    {
        using var db = CreateDb();
        db.Dashboards.Add(new Dashboard { Id = "default", PagesJson = "[{\"moduleId\":\"hello-world\"}]" });
        await db.SaveChangesAsync();

        var loaded = await db.Dashboards.FindAsync("default");
        Assert.NotNull(loaded);
        Assert.Contains("hello-world", loaded.PagesJson);
    }

    [Fact]
    public async Task Can_save_and_retrieve_datasource()
    {
        using var db = CreateDb();
        db.DataSources.Add(new DataSource { Id = "test-db", Type = "postgres", DisplayName = "Test DB", CredentialsJson = "encrypted" });
        await db.SaveChangesAsync();

        var loaded = await db.DataSources.FindAsync("test-db");
        Assert.NotNull(loaded);
        Assert.Equal("postgres", loaded.Type);
    }
}
