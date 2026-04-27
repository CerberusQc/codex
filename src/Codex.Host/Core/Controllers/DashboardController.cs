using Codex.Host.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Host.Core.Controllers;

[ApiController]
[Route("api/core/dashboard")]
public class DashboardController(DashboardService svc) : ControllerBase
{
    [HttpGet("pages")]
    public async Task<IActionResult> GetPages() =>
        Ok(await svc.GetPagesAsync());

    [HttpPut("pages")]
    public async Task<IActionResult> SetPages([FromBody] List<DashboardPage> pages)
    {
        await svc.SetPagesAsync(pages);
        return NoContent();
    }

    [HttpPost("pages/{moduleId}/enable")]
    public async Task<IActionResult> Enable(string moduleId)
    {
        await svc.EnableModuleAsync(moduleId);
        return NoContent();
    }

    [HttpDelete("pages/{moduleId}")]
    public async Task<IActionResult> Disable(string moduleId)
    {
        await svc.DisableModuleAsync(moduleId);
        return NoContent();
    }
}
