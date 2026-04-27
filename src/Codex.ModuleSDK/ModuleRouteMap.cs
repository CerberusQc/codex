using Microsoft.AspNetCore.Http;

namespace Codex.ModuleSDK;

public class ModuleRouteMap
{
    private readonly List<(string Method, string Pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> Handler)> _routes = new();

    public ModuleRouteMap Get(string pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> handler)
    {
        _routes.Add(("GET", pattern, handler));
        return this;
    }

    public ModuleRouteMap Post(string pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> handler)
    {
        _routes.Add(("POST", pattern, handler));
        return this;
    }

    public ModuleRouteMap Put(string pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> handler)
    {
        _routes.Add(("PUT", pattern, handler));
        return this;
    }

    public ModuleRouteMap Delete(string pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> handler)
    {
        _routes.Add(("DELETE", pattern, handler));
        return this;
    }

    public ModuleRouteMap Patch(string pattern, Func<HttpContext, IServiceProvider, CancellationToken, Task> handler)
    {
        _routes.Add(("PATCH", pattern, handler));
        return this;
    }

    public async Task<bool> TryHandleAsync(string method, string relativePath, HttpContext ctx, IServiceProvider sp, CancellationToken ct = default)
    {
        foreach (var (m, pattern, handler) in _routes)
        {
            if (m.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                TryMatch(pattern, relativePath, out var routeValues))
            {
                foreach (var (k, v) in routeValues)
                    ctx.Request.RouteValues[k] = v;
                await handler(ctx, sp, ct);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatch(string pattern, string path, out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>();

        var patternTrimmed = pattern.Trim('/');
        var pathTrimmed = path.Trim('/');

        // Short-circuit: if both are empty, it's a match; if only one is empty, it's a miss.
        if (patternTrimmed.Length == 0 && pathTrimmed.Length == 0)
            return true;
        if (patternTrimmed.Length == 0 || pathTrimmed.Length == 0)
            return false;

        var pp = patternTrimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var ap = pathTrimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);

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
