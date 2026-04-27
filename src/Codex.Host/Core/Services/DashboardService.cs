using System.Text.Json;
using Codex.Host.Core.Data;
using Codex.Host.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Core.Services;

public record DashboardPage(string ModuleId, int Order, bool IsFavorite);

public class DashboardService(CodexDbContext db)
{
    public async Task<List<DashboardPage>> GetPagesAsync()
    {
        var dash = await db.Dashboards.FindAsync("default");
        if (dash is null) return new List<DashboardPage>();
        return JsonSerializer.Deserialize<List<DashboardPage>>(dash.PagesJson) ?? new();
    }

    public async Task SetPagesAsync(List<DashboardPage> pages)
    {
        var json = JsonSerializer.Serialize(pages);
        var dash = await db.Dashboards.FindAsync("default");
        if (dash is null)
        {
            db.Dashboards.Add(new Dashboard { Id = "default", PagesJson = json });
        }
        else
        {
            dash.PagesJson = json;
        }
        await db.SaveChangesAsync();
    }

    public async Task EnableModuleAsync(string moduleId)
    {
        var pages = await GetPagesAsync();
        if (pages.Any(p => p.ModuleId == moduleId)) return;
        var nextOrder = pages.Count == 0 ? 0 : pages.Max(p => p.Order) + 1;
        pages.Add(new DashboardPage(moduleId, nextOrder, false));
        await SetPagesAsync(pages);
    }

    public async Task DisableModuleAsync(string moduleId)
    {
        var pages = await GetPagesAsync();
        var filtered = pages.Where(p => p.ModuleId != moduleId).ToList();
        if (filtered.Count == pages.Count) return;
        var reordered = filtered.Select((p, i) => p with { Order = i }).ToList();
        await SetPagesAsync(reordered);
    }
}
