using Codex.ModuleSDK;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace PostgresBrowser;

public class Module : ICodexModule
{
    public IEnumerable<DataSourceRequirement> Requires => new[]
    {
        new DataSourceRequirement("postgres:demo", DataSourceType.Postgres)
    };

    public void Register(IServiceCollection services) { }

    public void MapRoutes(ModuleRouteMap routes)
    {
        routes.Get("tables", async (ctx, sp, ct) =>
        {
            var ds = sp.GetService<NpgsqlDataSource>();
            if (ds is null)
            {
                ctx.Response.StatusCode = 503;
                await ctx.Response.WriteAsJsonAsync(new { error = "Datasource 'postgres:demo' not configured." }, ct);
                return;
            }

            await using var conn = await ds.OpenConnectionAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT table_name,
                       (SELECT reltuples::bigint FROM pg_class WHERE relname = t.table_name) AS row_estimate
                FROM information_schema.tables t
                WHERE table_schema = 'public'
                ORDER BY table_name";

            var tables = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add(new { name = reader.GetString(0), rowEstimate = reader.GetInt64(1) });

            await ctx.Response.WriteAsJsonAsync(new { tables }, ct);
        });
    }
}
