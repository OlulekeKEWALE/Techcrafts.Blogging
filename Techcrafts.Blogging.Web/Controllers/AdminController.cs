using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Application.Features.Posts.Queries;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBloggingDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(
        IMediator mediator,
        IBloggingDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _mediator = mediator;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var totalPosts = await _context.Posts.CountAsync();
        var totalViews = await _context.PageViews.CountAsync();
        var totalComments = await _context.Comments.CountAsync();
        var totalUsers = await _userManager.Users.CountAsync();
        var totalLikes = await _context.PostReactions
            .CountAsync(r => r.Reaction == ReactionType.Like);

        ViewBag.TotalPosts = totalPosts;
        ViewBag.TotalViews = totalViews;
        ViewBag.TotalComments = totalComments;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalLikes = totalLikes;

        // Recent posts for dashboard shortcut
        var recentPosts = await _context.Posts
            .OrderByDescending(p => p.CreatedDate)
            .Take(5)
            .Select(p => new AdminPostListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Status = p.Status,
                CreatedDate = p.CreatedDate,
                PublishedDate = p.PublishedDate,
                ViewCount = p.ViewCount
            })
            .ToListAsync();

        ViewBag.RecentPosts = recentPosts;

        return View();
    }

    [HttpGet("posts")]
    public async Task<IActionResult> Posts(
        [FromQuery] string? search,
        [FromQuery] PostStatus? status,
        [FromQuery] string? sortBy = "created_desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminPostsQuery(search, status, sortBy, page, pageSize);
        var result = await _mediator.Send(query);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.SortBy = sortBy;

        return View(result);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics()
    {
        var totalViews = await _context.PageViews.CountAsync();
        var totalClaps = await _context.PostClaps.SumAsync(c => (int?)c.Count) ?? 0;

        var startDate = DateTimeOffset.UtcNow.AddDays(-6).Date;

        var dailyViews = await _context.PageViews
            .Where(pv => pv.Timestamp >= startDate)
            .GroupBy(pv => pv.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

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
    public async Task<IActionResult> Create()
    {
        ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
        return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] string title,
        [FromForm] string summary,
        [FromForm] string content,
        [FromForm] string? featuredImageUrl,
        [FromForm] List<Guid>? tagIds,
        [FromForm] string? action)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(string.Empty, "Title, Summary, and Content are required.");
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            return View();
        }

        var authorId = await EnsureAuthorIdForCurrentUserAsync();

        bool publish = string.Equals(action, "publish", StringComparison.OrdinalIgnoreCase);

        var command = new CreatePostCommand(
            title.Trim(),
            summary.Trim(),
            content,
            string.IsNullOrWhiteSpace(featuredImageUrl) ? null : featuredImageUrl.Trim(),
            authorId,
            tagIds,
            null,
            publish);

        var postId = await _mediator.Send(command);

        TempData["SuccessMessage"] = publish
            ? "Post published successfully!"
            : "Post draft saved successfully!";

        return RedirectToAction(nameof(Posts));
    }

    [HttpGet("posts/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var post = await _mediator.Send(new GetAdminPostByIdQuery(id));
        if (post == null)
        {
            return NotFound();
        }

        ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
        return View(post);
    }

    [HttpPost("posts/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm] string title,
        [FromForm] string summary,
        [FromForm] string content,
        [FromForm] string? featuredImageUrl,
        [FromForm] bool isFeatured,
        [FromForm] List<Guid>? tagIds,
        [FromForm] string action,
        [FromForm] string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(string.Empty, "Title, Summary, and Content are required.");
            var post = await _mediator.Send(new GetAdminPostByIdQuery(id));
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            return View(post);
        }

        var command = new UpdatePostCommand(
            id,
            title.Trim(),
            summary.Trim(),
            content,
            string.IsNullOrWhiteSpace(featuredImageUrl) ? null : featuredImageUrl.Trim(),
            isFeatured,
            tagIds,
            action,
            rowVersion);

        var result = await _mediator.Send(command);
        if (result.Status == UpdatePostResultStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == UpdatePostResultStatus.ConcurrencyConflict)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            var conflictModel = new Techcrafts.Blogging.Web.Models.ConcurrencyConflictViewModel
            {
                PostId = id,
                SubmittedTitle = title,
                SubmittedSummary = summary,
                SubmittedContent = content,
                SubmittedFeaturedImageUrl = featuredImageUrl,
                SubmittedIsFeatured = isFeatured,
                SubmittedTagIds = tagIds,
                CurrentTitle = result.CurrentTitle ?? string.Empty,
                CurrentSummary = result.CurrentSummary ?? string.Empty,
                CurrentContent = result.CurrentContent ?? string.Empty,
                CurrentFeaturedImageUrl = result.CurrentFeaturedImageUrl,
                CurrentIsFeatured = result.CurrentIsFeatured ?? false,
                CurrentTagNames = result.CurrentTagNames ?? new List<string>(),
                NewRowVersion = result.NewRowVersion ?? string.Empty
            };
            return View("Conflict", conflictModel);
        }

        TempData["SuccessMessage"] = action switch
        {
            "publish" => "Post published successfully!",
            "unpublish" => "Post unpublished and moved to Drafts.",
            _ => "Post changes saved successfully!"
        };

        return RedirectToAction(nameof(Posts));
    }

    [HttpPost("posts/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id)
    {
        var success = await _mediator.Send(new PublishPostCommand(id));
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Post published successfully!";
        return RedirectToAction(nameof(Posts));
    }

    [HttpPost("posts/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var success = await _mediator.Send(new UnpublishPostCommand(id));
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Post moved to Drafts.";
        return RedirectToAction(nameof(Posts));
    }

    [HttpPost("posts/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _mediator.Send(new DeletePostCommand(id));
        if (!success) return NotFound();

        TempData["SuccessMessage"] = "Post deleted successfully.";
        return RedirectToAction(nameof(Posts));
    }

    [HttpGet("posts/{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id)
    {
        var post = await _mediator.Send(new GetAdminPostByIdQuery(id));
        if (post == null) return NotFound();

        var postDto = new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            FeaturedImageUrl = post.FeaturedImageUrl,
            PublishedDate = post.PublishedDate,
            ReadingTimeMinutes = post.ReadingTimeMinutes,
            Tags = post.TagNames.Select(t => new TagDto { Name = t, Slug = t.ToLower() }).ToList()
        };

        ViewData["IsAdminPreview"] = true;
        return View("~/Views/Posts/Detail.cshtml", postDto);
    }

    [HttpGet("logs")]
    public IActionResult Logs(string? file = null)
    {
        var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        var model = new List<string>();
        ViewBag.LogFiles = new List<string>();

        if (!Directory.Exists(logsPath))
        {
            return View(model);
        }

        var logFiles = Directory.GetFiles(logsPath, "log-*.txt")
            .OrderByDescending(f => f).ToList();

        ViewBag.LogFiles = logFiles.Select(Path.GetFileName).ToList();

        var selectedFile = string.IsNullOrEmpty(file) ? logFiles.FirstOrDefault() : Path.Combine(logsPath, file);
        ViewBag.CurrentFile = selectedFile != null ? Path.GetFileName(selectedFile) : null;

        if (selectedFile != null && System.IO.File.Exists(selectedFile))
        {
            try
            {
                using var fs = new FileStream(selectedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
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

    [HttpGet("change-password")]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "The new password and confirmation password do not match.");
            return View();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        ViewBag.SuccessMessage = "Your password has been changed successfully.";
        return View();
    }

    private async Task<Guid> EnsureAuthorIdForCurrentUserAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.Identity?.Name
            ?? "admin@techcrafts.com";

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Email == email);
        if (author == null)
        {
            author = new Author
            {
                DisplayName = User.Identity?.Name ?? "Techcrafts Editor",
                Email = email,
                Bio = "Editorial Administrator"
            };
            _context.Authors.Add(author);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }

        return author.Id;
    }
}
