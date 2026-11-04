using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Infrastructure.Persistence;

namespace Techcrafts.Blogging.Web.Controllers;

public class TelemetryController : Controller
{
    private readonly BloggingDbContext _dbContext;

    public TelemetryController(BloggingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Get the latest 100 telemetry logs
        var logs = await _dbContext.TelemetryLogs
            .OrderByDescending(t => t.Timestamp)
            .Take(100)
            .ToListAsync();

        return View(logs);
    }
}
