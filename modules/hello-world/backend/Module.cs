using Codex.ModuleSDK;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorld;

public class Module : ICodexModule
{
    public IEnumerable<DataSourceRequirement> Requires => Array.Empty<DataSourceRequirement>();

    public void Register(IServiceCollection services) { }

    public void MapRoutes(ModuleRouteMap routes)
    {
        routes.Get("ping", async (ctx, _, ct) =>
        {
            await ctx.Response.WriteAsJsonAsync(new { message = "Hello from the hello-world module!", timestamp = DateTimeOffset.UtcNow }, ct);
        });
    }
}
