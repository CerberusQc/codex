using System.Text.Json;
using Codex.Host.Core.Data;
using Codex.Host.Core.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Codex.Host.Core.Services;

public class DataSourceService(CodexDbContext db, IDataProtectionProvider dp)
{
    private readonly IDataProtector _protector = dp.CreateProtector("Codex.DataSources.v1");

    public async Task<DataSource> CreateAsync(string id, string type, string displayName, object credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        var encrypted = _protector.Protect(json);
        var ds = new DataSource { Id = id, Type = type, DisplayName = displayName, CredentialsJson = encrypted };
        db.DataSources.Add(ds);
        await db.SaveChangesAsync();
        return ds;
    }

    public async Task<DataSource?> GetAsync(string id) =>
        await db.DataSources.FindAsync(id);

    public async Task<List<DataSource>> ListAsync() =>
        await db.DataSources.ToListAsync();

    public async Task<JsonElement?> GetCredentialsAsync(string id)
    {
        var ds = await db.DataSources.FindAsync(id);
        if (ds is null) return null;
        var json = _protector.Unprotect(ds.CredentialsJson);
        return JsonSerializer.Deserialize<JsonElement>(json);
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
