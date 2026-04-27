using Microsoft.AspNetCore.Http;

namespace Codex.ModuleSDK;

public class ModuleRouteMap
{
    private readonly List<(string Method, string Pattern, Func<HttpContext, IServiceProvider, Task> Handler)> _routes = new();

    public ModuleRouteMap Get(string pattern, Func<HttpContext, IServiceProvider, Task> handler)
    {
        _routes.Add(("GET", pattern, handler));
        return this;
    }

    public ModuleRouteMap Post(string pattern, Func<HttpContext, IServiceProvider, Task> handler)
    {
        _routes.Add(("POST", pattern, handler));
        return this;
    }

    public async Task<bool> TryHandleAsync(string method, string relativePath, HttpContext ctx, IServiceProvider sp)
    {
        foreach (var (m, pattern, handler) in _routes)
        {
            if (m.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                TryMatch(pattern, relativePath, out var routeValues))
            {
                foreach (var (k, v) in routeValues)
                    ctx.Request.RouteValues[k] = v;
                await handler(ctx, sp);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatch(string pattern, string path, out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>();
        var pp = pattern.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var ap = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pp.Length != ap.Length) return false;
        for (int i = 0; i < pp.Length; i++)
        {
            if (pp[i].StartsWith('{') && pp[i].EndsWith('}'))
                values[pp[i][1..^1]] = ap[i];
            else if (!string.Equals(pp[i], ap[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
