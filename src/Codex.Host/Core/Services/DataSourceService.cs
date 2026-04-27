using System.Text.Json;
using Codex.Host.Core.Data;
using Codex.Host.Core.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Core.Services;

public record DataSourceSummary(string Id, string Type, string DisplayName);

public class DataSourceService(CodexDbContext db, IDataProtectionProvider dp)
{
    private readonly IDataProtector _protector = dp.CreateProtector("Codex.DataSources.v1");

    public async Task<DataSourceSummary> CreateAsync(string id, string type, string displayName, object credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        var encrypted = _protector.Protect(json);
        var ds = new DataSource { Id = id, Type = type, DisplayName = displayName, CredentialsJson = encrypted };
        db.DataSources.Add(ds);
        await db.SaveChangesAsync();
        return new DataSourceSummary(ds.Id, ds.Type, ds.DisplayName);
    }

    public async Task<DataSourceSummary?> GetAsync(string id)
    {
        var ds = await db.DataSources.FindAsync(id);
        if (ds is null) return null;
        return new DataSourceSummary(ds.Id, ds.Type, ds.DisplayName);
    }

    public async Task<List<DataSourceSummary>> ListAsync()
    {
        var list = await db.DataSources.ToListAsync();
        return list.Select(s => new DataSourceSummary(s.Id, s.Type, s.DisplayName)).ToList();
    }

    public async Task<JsonElement?> GetCredentialsAsync(string id)
    {
        var ds = await db.DataSources.FindAsync(id);
        if (ds is null) return null;
        try
        {
            var json = _protector.Unprotect(ds.CredentialsJson);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Key ring changed (e.g. container restarted without persisted keys).
            // Treat as missing so the module gets MissingDatasource status.
            return null;
        }
    }

    public async Task UpdateAsync(string id, string displayName, object credentials)
    {
        var ds = await db.DataSources.FindAsync(id) ?? throw new KeyNotFoundException(id);
        ds.DisplayName = displayName;
        ds.CredentialsJson = _protector.Protect(JsonSerializer.Serialize(credentials));
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var ds = await db.DataSources.FindAsync(id);
        if (ds is not null)
        {
            db.DataSources.Remove(ds);
            await db.SaveChangesAsync();
        }
    }
}
