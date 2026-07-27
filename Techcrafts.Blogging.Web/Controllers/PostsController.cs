using MediatR;
using Microsoft.AspNetCore.Mvc;
using Techcrafts.Blogging.Application.Features.Posts.Queries;

namespace Techcrafts.Blogging.Web.Controllers;

public class PostsController : Controller
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var result = await _mediator.Send(new GetPublicPostsQuery(page));
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_PostListPartial", result.Posts);
        }
        
        return View(result);
    }

    [HttpGet("posts/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        var userAgent = Request.Headers.UserAgent.ToString();
        var readerHash = ComputeSha256Hash($"{ip}|{userAgent}");

        var post = await _mediator.Send(new GetPostBySlugQuery(slug, readerHash));
        if (post == null) return NotFound();
        return View(post);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawData));
        var builder = new System.Text.StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }
        return builder.ToString();
    }
}
