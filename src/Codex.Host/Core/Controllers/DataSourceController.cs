using System.Text.Json;
using Codex.Host.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Host.Core.Controllers;

[ApiController]
[Route("api/core/datasources")]
public class DataSourceController(DataSourceService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var sources = await svc.ListAsync();
        return Ok(sources.Select(s => new { s.Id, s.Type, s.DisplayName }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDataSourceRequest req)
    {
        var ds = await svc.CreateAsync(req.Id, req.Type, req.DisplayName, req.Credentials);
        return CreatedAtAction(nameof(List), new { id = ds.Id }, new { ds.Id, ds.Type, ds.DisplayName });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDataSourceRequest req)
    {
        await svc.UpdateAsync(id, req.DisplayName, req.Credentials);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await svc.DeleteAsync(id);
        return NoContent();
    }

    public record CreateDataSourceRequest(string Id, string Type, string DisplayName, JsonElement Credentials);
    public record UpdateDataSourceRequest(string DisplayName, JsonElement Credentials);
}
