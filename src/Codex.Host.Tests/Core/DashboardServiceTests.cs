using Codex.Host.Core.Data;
using Codex.Host.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Tests.Core;

public class DashboardServiceTests
{
    private static (CodexDbContext db, DashboardService svc) Create()
    {
        var opts = new DbContextOptionsBuilder<CodexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CodexDbContext(opts);
        return (db, new DashboardService(db));
    }

    [Fact]
    public async Task GetPages_returns_empty_list_when_no_dashboard()
    {
        var (_, svc) = Create();
        var pages = await svc.GetPagesAsync();
        Assert.Empty(pages);
    }

    [Fact]
    public async Task SetPages_persists_order_and_favorites()
    {
        var (_, svc) = Create();
        var pages = new List<DashboardPage>
        {
            new("hello-world", 0, true),
            new("system-info", 1, false)
        };
        await svc.SetPagesAsync(pages);
        var loaded = await svc.GetPagesAsync();
        Assert.Equal(2, loaded.Count);
        Assert.True(loaded[0].IsFavorite);
        Assert.Equal("hello-world", loaded[0].ModuleId);
    }

    [Fact]
    public async Task EnableModule_adds_page_at_end()
    {
        var (_, svc) = Create();
        await svc.EnableModuleAsync("hello-world");
        await svc.EnableModuleAsync("system-info");
        var pages = await svc.GetPagesAsync();
        Assert.Equal(2, pages.Count);
        Assert.Equal("system-info", pages[1].ModuleId);
    }

    [Fact]
    public async Task EnableModule_is_idempotent()
    {
        var (_, svc) = Create();
        await svc.EnableModuleAsync("hello-world");
        await svc.EnableModuleAsync("hello-world");
        var pages = await svc.GetPagesAsync();
        Assert.Single(pages);
    }

    [Fact]
    public async Task DisableModule_removes_page()
    {
        var (_, svc) = Create();
        await svc.EnableModuleAsync("hello-world");
        await svc.DisableModuleAsync("hello-world");
        var pages = await svc.GetPagesAsync();
        Assert.Empty(pages);
    }
}
