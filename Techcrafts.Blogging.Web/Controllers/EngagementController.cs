using MediatR;
using Microsoft.AspNetCore.Mvc;
using Techcrafts.Blogging.Application.Features.Engagement.Commands;
using Techcrafts.Blogging.Domain.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace Techcrafts.Blogging.Web.Controllers;

[Route("api/engagement")]
public class EngagementController : Controller
{
    private readonly IMediator _mediator;

    public EngagementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("clap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clap([FromForm] Guid postId)
    {
        var command = new ClapPostCommand(postId, Request.Headers.UserAgent.ToString());
        var newCount = await _mediator.Send(command);
        return Json(new { success = true, count = newCount });
    }

    [HttpPost("bookmark")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bookmark([FromForm] Guid postId)
    {
        // Mock user id for demonstration since Auth is not fully wired
        Guid userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); 
        var isBookmarked = await _mediator.Send(new ToggleBookmarkCommand(postId, userId));
        return Json(new { success = true, isBookmarked = isBookmarked });
    }

    [HttpPost("ToggleReaction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReaction([FromForm] Guid postId, [FromForm] ReactionType reaction)
    {
        // Prevent invalid enums
        if (reaction != ReactionType.Like && reaction != ReactionType.Dislike)
        {
            return BadRequest(new { success = false, message = "Invalid reaction type." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        var userAgent = Request.Headers.UserAgent.ToString();
        var readerHash = ComputeSha256Hash($"{ip}|{userAgent}");

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        var command = new ToggleReactionCommand
        {
            PostId = postId,
            ReaderHash = readerHash,
            UserId = userId,
            Reaction = reaction
        };

        var result = await _mediator.Send(command);

        return Json(new
        {
            success = result.Success,
            likesCount = result.LikesCount,
            dislikesCount = result.DislikesCount,
            userReaction = result.UserReaction
        });
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }
        return builder.ToString();
    }
}
