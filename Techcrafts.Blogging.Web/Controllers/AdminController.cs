using MediatR;
using Microsoft.AspNetCore.Mvc;
using Techcrafts.Blogging.Application.Features.Posts.Commands;

using Microsoft.AspNetCore.Authorization;

namespace Techcrafts.Blogging.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromServices] Techcrafts.Blogging.Application.Interfaces.IBloggingDbContext context,
        [FromServices] Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
    {
        var totalPosts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Posts);
        var totalViews = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.PageViews);
        var totalComments = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Comments);
        var totalUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(userManager.Users);
        var totalLikes = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            context.PostReactions.Where(r => r.Reaction == Techcrafts.Blogging.Domain.Enums.ReactionType.Like));

        ViewBag.TotalPosts = totalPosts;
        ViewBag.TotalViews = totalViews;
        ViewBag.TotalComments = totalComments;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalLikes = totalLikes;

        return View();
    }
    
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromServices] Techcrafts.Blogging.Application.Interfaces.IBloggingDbContext context)
    {
        var totalViews = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.PageViews);
        var totalClaps = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SumAsync(context.PostClaps, c => (int?)c.Count) ?? 0;
        
        // Fetch real daily data for the last 7 days
        var startDate = DateTimeOffset.UtcNow.AddDays(-6).Date;
        
        var dailyViews = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            context.PageViews
            .Where(pv => pv.Timestamp >= startDate)
            .GroupBy(pv => pv.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
        );

        var labelsList = new List<string>();
        var viewDataList = new List<int>();

        for (int i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i);
            labelsList.Add(date.ToString("ddd"));
            
            var match = dailyViews.FirstOrDefault(d => d.Date == date);
            viewDataList.Add(match?.Count ?? 0);
        }

        ViewBag.TotalViews = totalViews;
        ViewBag.TotalClaps = totalClaps;
        ViewBag.ViewData = System.Text.Json.JsonSerializer.Serialize(viewDataList);
        ViewBag.Labels = System.Text.Json.JsonSerializer.Serialize(labelsList);

        return View();
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreatePostCommand command)
    {
        var postId = await _mediator.Send(command);
        // Automatically publish it for demonstration purposes
        await _mediator.Send(new PublishPostCommand(postId));
        
        return RedirectToAction("Index", "Posts");
    }

    [HttpGet("logs")]
    public IActionResult Logs(string file = null)
    {
        var logsPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "logs");
        var model = new List<string>();
        ViewBag.LogFiles = new List<string>();
        
        if (!System.IO.Directory.Exists(logsPath))
        {
            return View(model);
        }

        var logFiles = System.IO.Directory.GetFiles(logsPath, "log-*.txt")
            .OrderByDescending(f => f).ToList();
            
        ViewBag.LogFiles = logFiles.Select(System.IO.Path.GetFileName).ToList();
        
        var selectedFile = string.IsNullOrEmpty(file) ? logFiles.FirstOrDefault() : System.IO.Path.Combine(logsPath, file);
        ViewBag.CurrentFile = selectedFile != null ? System.IO.Path.GetFileName(selectedFile) : null;
        
        if (selectedFile != null && System.IO.File.Exists(selectedFile))
        {
            try
            {
                using var fs = new System.IO.FileStream(selectedFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                using var sr = new System.IO.StreamReader(fs);
                var allLines = new List<string>();
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) allLines.Add(line);
                }
                model = allLines.TakeLast(1000).ToList();
            }
            catch (Exception ex)
            {
                model.Add($"Error reading log file: {ex.Message}");
            }
        }
        
        return View(model);
    }
}
