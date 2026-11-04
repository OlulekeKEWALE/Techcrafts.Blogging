using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Web.Middlewares;
using Serilog;
using Microsoft.AspNetCore.Identity;

namespace Techcrafts.Blogging.Web.Controllers;

public class SetupController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly BloggingDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public SetupController(IWebHostEnvironment env, BloggingDbContext dbContext, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _env = env;
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string connectionString, string adminEmail, string adminPassword, bool enableTelemetry = true)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            ModelState.AddModelError("", "Connection string, admin email, and admin password are all required.");
            return View();
        }

        try
        {
            // 1. Update appsettings.json
            string appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            string json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
            
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode != null)
            {
                var connectionStringsNode = jsonNode["ConnectionStrings"] as JsonObject;
                if (connectionStringsNode == null)
                {
                    connectionStringsNode = new JsonObject();
                    jsonNode["ConnectionStrings"] = connectionStringsNode;
                }
                connectionStringsNode["DefaultConnection"] = connectionString;

                var telemetryNode = jsonNode["Telemetry"] as JsonObject;
                if (telemetryNode == null)
                {
                    telemetryNode = new JsonObject();
                    jsonNode["Telemetry"] = telemetryNode;
                }
                telemetryNode["Enabled"] = enableTelemetry;

                var options = new JsonSerializerOptions { WriteIndented = true };
                await System.IO.File.WriteAllTextAsync(appSettingsPath, jsonNode.ToJsonString(options));
            }
            
            // Allow time for Configuration to reload (reloadOnChange is true by default)
            await Task.Delay(1000); 

            // 2. Validate connection and apply migrations
            bool canConnect = await _dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                ModelState.AddModelError("", "Could not connect to the database with the provided connection string.");
                return View();
            }

            await _dbContext.Database.MigrateAsync();
            
            // 3. Seed Database
            await DbSeeder.SeedAsync(_dbContext, adminEmail);

            // 4. Create Identity Roles and Admin User
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    return View();
                }
            }

            // 5. Reset Middleware state
            SetupMiddleware.ResetState();

            return RedirectToAction("Index", "Posts");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to configure database during setup.");
            ModelState.AddModelError("", $"Setup failed: {ex.Message}");
            return View();
        }
    }
}
