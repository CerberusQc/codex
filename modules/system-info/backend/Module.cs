using System.Diagnostics;
using Codex.ModuleSDK;
using Microsoft.Extensions.DependencyInjection;

namespace SystemInfo;

public class Module : ICodexModule
{
    public IEnumerable<DataSourceRequirement> Requires => Array.Empty<DataSourceRequirement>();

    public void Register(IServiceCollection services) { }

    public void MapRoutes(ModuleRouteMap routes)
    {
        routes.Get("stats", async (ctx, _, ct) =>
        {
            var proc = Process.GetCurrentProcess();
            await ctx.Response.WriteAsJsonAsync(new
            {
                machineName = Environment.MachineName,
                processorCount = Environment.ProcessorCount,
                osVersion = Environment.OSVersion.ToString(),
                uptimeSeconds = (long)(DateTime.UtcNow - proc.StartTime.ToUniversalTime()).TotalSeconds,
                workingSetMb = Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1),
                dotnetVersion = Environment.Version.ToString()
            }, ct);
        });
    }
}
