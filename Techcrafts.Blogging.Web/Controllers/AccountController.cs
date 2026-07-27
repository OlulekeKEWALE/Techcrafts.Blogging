using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Techcrafts.Blogging.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost("external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin([FromForm] string provider, [FromForm] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            return RedirectToAction("Index", "Home", new { error = remoteError });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        
        if (signInResult.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        // Auto-provision user if they don't exist
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded) return RedirectToAction("Index", "Home");
                await CheckAndAssignAdminRoleAsync(user);
            }

            await _userManager.AddLoginAsync(user, info);
            
            // Add extra claims from external provider (like avatar url from GitHub)
            var avatarUrl = info.Principal.FindFirstValue("urn:github:avatar_url");
            if (avatarUrl != null)
            {
                await _userManager.AddClaimAsync(user, new Claim("urn:github:avatar_url", avatarUrl));
            }
            
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            if (name != null)
            {
                await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("quick-email-auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickEmailAuth([FromForm] string email, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Json(new { success = false, message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Auto register
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            await CheckAndAssignAdminRoleAsync(user);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
        
        if (signInResult.Succeeded)
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, message = "Invalid login attempt." });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout([FromForm] string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect(returnUrl ?? "/");
    }

    private async Task CheckAndAssignAdminRoleAsync(IdentityUser user)
    {
        // If this is the first user in the system, make them an Admin
        if (_userManager.Users.Count() == 1)
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            await _userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
