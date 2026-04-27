using Codex.Host.Core.Data;
using Codex.Host.Core.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Tests.Core;

public class DataSourceServiceTests
{
    private static (CodexDbContext db, DataSourceService svc) Create()
    {
        var opts = new DbContextOptionsBuilder<CodexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CodexDbContext(opts);
        var dp = new EphemeralDataProtectionProvider();
        var svc = new DataSourceService(db, dp);
        return (db, svc);
    }

    [Fact]
    public async Task Create_encrypts_credentials()
    {
        var (db, svc) = Create();
        await svc.CreateAsync("my-db", "postgres", "My DB", new { host = "localhost", port = 5432, user = "app", password = "secret" });

        var raw = await db.DataSources.FindAsync("my-db");
        Assert.NotNull(raw);
        Assert.DoesNotContain("secret", raw.CredentialsJson);
    }

    [Fact]
    public async Task GetCredentials_decrypts_correctly()
    {
        var (_, svc) = Create();
        await svc.CreateAsync("my-db", "postgres", "My DB", new { host = "localhost", port = 5432, user = "app", password = "secret" });

        var creds = await svc.GetCredentialsAsync("my-db");
        Assert.NotNull(creds);
        Assert.Equal("secret", creds.Value.GetProperty("password").GetString());
    }

    [Fact]
    public async Task Delete_removes_datasource()
    {
        var (db, svc) = Create();
        await svc.CreateAsync("my-db", "postgres", "My DB", new { host = "localhost" });
        await svc.DeleteAsync("my-db");

        Assert.Equal(0, await db.DataSources.CountAsync());
    }
}
