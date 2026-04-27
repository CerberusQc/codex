using Codex.Host.Modules;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Host.Core.Controllers;

[ApiController]
[Route("api/core/modules")]
public class ModuleStoreController(ModuleRegistry registry) : ControllerBase
{
    [HttpGet]
    public IActionResult List() =>
        Ok(registry.All().Select(e => new
        {
            e.Id,
            e.DisplayName,
            e.Version,
            e.Description,
            e.Icon,
            e.PageRoute,
            Status = e.Status.ToString(),
            e.BuildLog,
            LoadedAt = e.LoadedAt?.ToString("o")
        }));

    [HttpGet("{id}/buildlog")]
    public IActionResult GetBuildLog(string id)
    {
        var entry = registry.Get(id);
        if (entry is null) return NotFound();
        return Ok(new { entry.BuildLog });
    }
}
