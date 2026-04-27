using Codex.ModuleSDK;
using Microsoft.AspNetCore.Http;

namespace Codex.Host.Tests.Modules;

public class ModuleRouteMapTests
{
    // -----------------------------------------------------------------------
    // TryMatch behaviour (tested indirectly via TryHandleAsync)
    // -----------------------------------------------------------------------

    private static DefaultHttpContext MakeContext(string method, string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        return ctx;
    }

    [Fact]
    public async Task EmptyPattern_MatchesEmptyPath()
    {
        bool called = false;
        var map = new ModuleRouteMap();
        map.Get("", (ctx, sp, ct) => { called = true; return Task.CompletedTask; });

        var ctx = MakeContext("GET", "");
        var result = await map.TryHandleAsync("GET", "", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.True(called);
    }

    [Fact]
    public async Task EmptyPattern_DoesNotMatchNonEmptyPath()
    {
        var map = new ModuleRouteMap();
        map.Get("", (ctx, sp, ct) => Task.CompletedTask);

        var ctx = MakeContext("GET", "/foo");
        var result = await map.TryHandleAsync("GET", "/foo", ctx, NullServiceProvider.Instance);

        Assert.False(result);
    }

    [Fact]
    public async Task NonEmptyPattern_DoesNotMatchEmptyPath()
    {
        var map = new ModuleRouteMap();
        map.Get("items", (ctx, sp, ct) => Task.CompletedTask);

        var ctx = MakeContext("GET", "");
        var result = await map.TryHandleAsync("GET", "", ctx, NullServiceProvider.Instance);

        Assert.False(result);
    }

    [Fact]
    public async Task ParamCapture_PopulatesRouteValues()
    {
        string? captured = null;
        var map = new ModuleRouteMap();
        map.Get("items/{id}", (ctx, sp, ct) =>
        {
            captured = ctx.Request.RouteValues["id"] as string;
            return Task.CompletedTask;
        });

        var ctx = MakeContext("GET", "/items/42");
        var result = await map.TryHandleAsync("GET", "/items/42", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.Equal("42", captured);
    }

    [Fact]
    public async Task LiteralMatch_IsCaseInsensitive()
    {
        bool called = false;
        var map = new ModuleRouteMap();
        map.Get("Hello/World", (ctx, sp, ct) => { called = true; return Task.CompletedTask; });

        var ctx = MakeContext("GET", "/hello/world");
        var result = await map.TryHandleAsync("GET", "/hello/world", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.True(called);
    }

    [Fact]
    public async Task SegmentCountMismatch_ReturnsFalse()
    {
        var map = new ModuleRouteMap();
        map.Get("a/b/c", (ctx, sp, ct) => Task.CompletedTask);

        var ctx = MakeContext("GET", "/a/b");
        var result = await map.TryHandleAsync("GET", "/a/b", ctx, NullServiceProvider.Instance);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // HTTP verb dispatch
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteVerb_IsDispatched()
    {
        bool called = false;
        var map = new ModuleRouteMap();
        map.Delete("items/{id}", (ctx, sp, ct) => { called = true; return Task.CompletedTask; });

        var ctx = MakeContext("DELETE", "/items/7");
        var result = await map.TryHandleAsync("DELETE", "/items/7", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.True(called);
    }

    [Fact]
    public async Task PutVerb_IsDispatched()
    {
        bool called = false;
        var map = new ModuleRouteMap();
        map.Put("items/{id}", (ctx, sp, ct) => { called = true; return Task.CompletedTask; });

        var ctx = MakeContext("PUT", "/items/3");
        var result = await map.TryHandleAsync("PUT", "/items/3", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.True(called);
    }

    [Fact]
    public async Task PatchVerb_IsDispatched()
    {
        bool called = false;
        var map = new ModuleRouteMap();
        map.Patch("items/{id}", (ctx, sp, ct) => { called = true; return Task.CompletedTask; });

        var ctx = MakeContext("PATCH", "/items/5");
        var result = await map.TryHandleAsync("PATCH", "/items/5", ctx, NullServiceProvider.Instance);

        Assert.True(result);
        Assert.True(called);
    }

    [Fact]
    public async Task WrongVerb_DoesNotMatch()
    {
        var map = new ModuleRouteMap();
        map.Get("items", (ctx, sp, ct) => Task.CompletedTask);

        var ctx = MakeContext("POST", "/items");
        var result = await map.TryHandleAsync("POST", "/items", ctx, NullServiceProvider.Instance);

        Assert.False(result);
    }

    [Fact]
    public async Task CancellationToken_IsForwardedToHandler()
    {
        CancellationToken received = default;
        var map = new ModuleRouteMap();
        map.Get("ping", (ctx, sp, ct) => { received = ct; return Task.CompletedTask; });

        using var cts = new CancellationTokenSource();
        var ctx = MakeContext("GET", "/ping");
        await map.TryHandleAsync("GET", "/ping", ctx, NullServiceProvider.Instance, cts.Token);

        Assert.Equal(cts.Token, received);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static readonly NullServiceProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
