using Microsoft.AspNetCore.Mvc;

namespace Techcrafts.Blogging.Web.Controllers;

[Route("newsletter")]
public class NewsletterController : Controller
{
    [HttpPost("subscribe")]
    [ValidateAntiForgeryToken]
    public IActionResult Subscribe([FromForm] string email)
    {
        return Content("<div class='alert alert-success'>Subscribed successfully!</div>", "text/html");
    }
}
