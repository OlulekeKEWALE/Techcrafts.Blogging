using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Techcrafts.Blogging.Application.Features.Comments.Commands;

namespace Techcrafts.Blogging.Web.Controllers;

[Route("comments")]
public class CommentsController : Controller
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] SubmitCommentCommand command)
    {
        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var enrichedCommand = command with { IpAddress = ip ?? "127.0.0.1" };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name ?? "auth@user.com";
            var name = User.FindFirstValue("urn:github:name") ?? User.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
            var avatarUrl = User.FindFirstValue("urn:github:avatar_url") ?? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(name)}&background=random";

            enrichedCommand = enrichedCommand with
            {
                UserId = userId,
                AuthorEmail = email,
                AuthorName = name,
                AuthorAvatarUrl = avatarUrl
            };
        }
        
        var id = await _mediator.Send(enrichedCommand);
        
        var dto = new Techcrafts.Blogging.Application.Features.Comments.Queries.CommentDto
        {
            Id = id,
            ParentCommentId = enrichedCommand.ParentCommentId,
            AuthorName = enrichedCommand.AuthorName,
            AuthorAvatarUrl = enrichedCommand.AuthorAvatarUrl,
            Content = enrichedCommand.Content,
            CreatedDate = DateTimeOffset.UtcNow,
            Status = "Pending"
        };
        
        return PartialView("_CommentItem", dto);
    }
}
