using Microsoft.Extensions.DependencyInjection;

namespace Codex.ModuleSDK;

public interface ICodexModule
{
    IEnumerable<DataSourceRequirement> Requires { get; }
    void Register(IServiceCollection services);
    void MapRoutes(ModuleRouteMap routes);
}
