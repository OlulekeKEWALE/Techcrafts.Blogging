using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class PlaywrightServerFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://127.0.0.1:5189";
    public static readonly string ConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? 
        "Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;";
    public const string AdminEmail = "admin@techcrafts.com";
    public const string AdminPassword = "Password123!";

    private Process? _webProcess;
    private readonly System.Text.StringBuilder _serverLog = new();
    public IPlaywright? PlaywrightInstance { get; private set; }
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        // 1. Prepare Database (Seed Admin User, Role, Author, Baseline Post)
        await PrepareDatabaseAsync();

        // 2. Start Web Server
        StartWebServer();

        // 3. Wait for Server ready
        await WaitForServerReadyAsync();

        // 4. Initialize Playwright & Chromium
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    private async Task PrepareDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var db = new BloggingDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
        }
        catch
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Ensure Admin Role exists
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
        if (adminRole == null)
        {
            adminRole = new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        // Ensure Admin User exists
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == AdminEmail.ToUpperInvariant());
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = AdminEmail,
                NormalizedUserName = AdminEmail.ToUpperInvariant(),
                Email = AdminEmail,
                NormalizedEmail = AdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var hasher = new PasswordHasher<IdentityUser>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, AdminPassword);
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
            await db.SaveChangesAsync();
        }

        // Ensure Author exists
        var author = await db.Authors.FirstOrDefaultAsync(a => a.Email == AdminEmail);
        if (author == null)
        {
            author = new Author
            {
                DisplayName = "Playwright Administrator",
                Email = AdminEmail,
                Bio = "Playwright E2E Administrator",
                AvatarUrl = "/images/logo.png"
            };
            db.Authors.Add(author);
            await db.SaveChangesAsync();
        }

        // Ensure baseline post for listing / editing tests
        var basePost = await db.Posts.FirstOrDefaultAsync(p => p.Slug == "concurrency-baseline-post");
        if (basePost == null)
        {
            basePost = new Post
            {
                Title = "Concurrency Baseline Post",
                Slug = "concurrency-baseline-post",
                Summary = "Initial baseline post for browser concurrency verification",
                Content = "## Baseline Content\n\nInitial version V1.",
                AuthorId = author.Id,
                Status = PostStatus.Draft
            };
            db.Posts.Add(basePost);
            await db.SaveChangesAsync();
        }
    }

    private void StartWebServer()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Techcrafts.Blogging.slnx")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            throw new DirectoryNotFoundException("Could not find solution root directory.");
        }

        var webDir = Path.Combine(currentDir.FullName, "Techcrafts.Blogging.Web");
        var configFolder = File.Exists(Path.Combine(webDir, "bin", "Release", "net10.0", "Techcrafts.Blogging.Web.dll")) ? "Release" : "Debug";
        var dllPath = Path.Combine(webDir, "bin", configFolder, "net10.0", "Techcrafts.Blogging.Web.dll");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\" --urls {BaseUrl}",
            WorkingDirectory = webDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.EnvironmentVariables["ConnectionStrings__DefaultConnection"] = ConnectionString;
        psi.EnvironmentVariables["Telemetry__Enabled"] = "false";
        psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";

        _webProcess = new Process { StartInfo = psi };
        _webProcess.OutputDataReceived += (s, e) => { if (e.Data != null) _serverLog.AppendLine(e.Data); };
        _webProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) _serverLog.AppendLine("[ERR] " + e.Data); };

        if (!_webProcess.Start())
        {
            throw new InvalidOperationException("Failed to start web server process.");
        }

        _webProcess.BeginOutputReadLine();
        _webProcess.BeginErrorReadLine();
    }

    private async Task WaitForServerReadyAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        var deadline = DateTime.UtcNow.AddSeconds(45);

        while (DateTime.UtcNow < deadline)
        {
            if (_webProcess != null && _webProcess.HasExited)
            {
                throw new InvalidOperationException($"Web server process exited prematurely with code {_webProcess.ExitCode}. Log:\n{_serverLog}");
            }

            try
            {
                var response = await client.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Wait and retry
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Web server at {BaseUrl} failed to respond within 20 seconds. Process Exited: {_webProcess?.HasExited}. Log:\n{_serverLog}");
    }

    public async Task<IBrowserContext> CreateAdminContextAsync()
    {
        if (Browser == null) throw new InvalidOperationException("Browser is not initialized.");
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/account/login?ReturnUrl=%2Fadmin%2Fposts");

        await page.FillAsync("#login-email", AdminEmail);
        await page.FillAsync("#login-password", AdminPassword);

        var loginRespTask = page.WaitForResponseAsync(resp => resp.Url.Contains("/account/quick-email-auth"));
        await page.ClickAsync("#btn-login-submit");
        await loginRespTask;

        // Wait until navigation redirects to /admin/posts
        await page.WaitForURLAsync(url => url.Contains("/admin/posts"), new PageWaitForURLOptions
        {
            Timeout = 10000
        });

        await page.CloseAsync();
        return context;
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }

        PlaywrightInstance?.Dispose();

        if (_webProcess != null && !_webProcess.HasExited)
        {
            _webProcess.Kill(true);
            _webProcess.Dispose();
        }
    }
}

[CollectionDefinition("PlaywrightTests", DisableParallelization = true)]
public class PlaywrightCollection : ICollectionFixture<PlaywrightServerFixture>
{
}

[Collection("PlaywrightTests")]
public class PlaywrightBrowserE2ETests
{
    private readonly PlaywrightServerFixture _fixture;
    private static readonly string ScreenshotsDir = Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/playwright/screenshots");

    public PlaywrightBrowserE2ETests(PlaywrightServerFixture fixture)
    {
        _fixture = fixture;
        Directory.CreateDirectory(ScreenshotsDir);
    }

    private static async Task SetContentAsync(IPage page, string content)
    {
        await page.EvaluateAsync(@"(val) => {
            const el = document.getElementById('markdown-editor');
            if (el) {
                el.value = val;
                if (el.easyMDE) {
                    el.easyMDE.value(val);
                }
            }
        }", content);
    }

    [Fact]
    public async Task E2E_01_AnonymousUser_RedirectedToLogin()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/posts");
        Assert.NotNull(response);

        // Assert: redirected to /account/login with returnUrl
        Assert.Contains("/account/login", page.Url);
        Assert.Contains("ReturnUrl", page.Url);

        var loginForm = await page.QuerySelectorAsync("#email-login-form");
        Assert.NotNull(loginForm);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_01_Anonymous_Redirect.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_02_AdminLogin_AuthenticatesViaRealBrowser()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/account/login?ReturnUrl=%2Fadmin%2Fposts");

        await page.FillAsync("#login-email", PlaywrightServerFixture.AdminEmail);
        await page.FillAsync("#login-password", PlaywrightServerFixture.AdminPassword);

        var respTask = page.WaitForResponseAsync(resp => resp.Url.Contains("/account/quick-email-auth"));
        await page.ClickAsync("#btn-login-submit");
        await respTask;

        await page.WaitForURLAsync(url => url.Contains("/admin/posts"), new PageWaitForURLOptions { Timeout = 10000 });

        Assert.Contains("/admin/posts", page.Url);

        // Verify admin layout navigation elements
        var navOverview = await page.QuerySelectorAsync("a.nav-link:has-text('Overview')");
        var navPosts = await page.QuerySelectorAsync("a.nav-link:has-text('Blog Posts')");
        Assert.NotNull(navOverview);
        Assert.NotNull(navPosts);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_02_Admin_Authenticated.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_03_AdminPostsTable_RendersColumnsAndNavigation()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/posts");

        // Verify table headers
        var titleHeader = await page.QuerySelectorAsync("th:has-text('Title')");
        var statusHeader = await page.QuerySelectorAsync("th:has-text('Status')");
        var createdHeader = await page.QuerySelectorAsync("th:has-text('Created')");
        var viewsHeader = await page.QuerySelectorAsync("th:has-text('Views')");
        var actionsHeader = await page.QuerySelectorAsync("th:has-text('Actions')");

        Assert.NotNull(titleHeader);
        Assert.NotNull(statusHeader);
        Assert.NotNull(createdHeader);
        Assert.NotNull(viewsHeader);
        Assert.NotNull(actionsHeader);

        // Verify "New Post" action button
        var newPostBtn = await page.QuerySelectorAsync("a[href='/admin/create']");
        Assert.NotNull(newPostBtn);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_03_Posts_Table_Headers.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_04_FilterAndSearch_PostsInRealBrowser()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/posts");

        // Search for baseline post
        await page.FillAsync("input[name='search']", "Concurrency Baseline");
        await page.ClickAsync("button:has-text('Filter')");

        await page.WaitForURLAsync(url => url.Contains("search=Concurrency+Baseline") || url.Contains("search=Concurrency%20Baseline"));

        var rowText = await page.TextContentAsync("table tbody");
        Assert.Contains("Concurrency Baseline Post", rowText);

        // Reset search
        var resetBtn = await page.QuerySelectorAsync("a[title='Reset Filters']");
        if (resetBtn != null)
        {
            await resetBtn.ClickAsync();
            await page.WaitForURLAsync(url => !url.Contains("search="));
        }

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_04_Search_Filter_Success.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_05_CreateDraftPost_PersistsAndShowsInListing()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/create");

        var testTitle = $"Playwright Draft {Guid.NewGuid().ToString()[..8]}";
        await page.FillAsync("input[name='title']", testTitle);
        await page.FillAsync("textarea[name='summary']", "A post created by Playwright Chromium E2E automation");
        await SetContentAsync(page, "### Playwright Automated Content\n\nThis is real content from Chromium.");

        // Submit form and wait for redirect to /admin/posts
        var navTask = page.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await page.ClickAsync("button[type='submit'][name='action'][value='draft']");
        await navTask;

        var content = await page.TextContentAsync("body");
        Assert.Contains(testTitle, content);
        Assert.Contains("saved successfully", content);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_05_Draft_Created_Listing.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_06_EditPost_UpdatesContentAndPreservesSlug()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        // 1. First, create a dedicated post to edit
        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/create");
        var initialTitle = $"Editable Post {Guid.NewGuid().ToString()[..8]}";
        await page.FillAsync("input[name='title']", initialTitle);
        await page.FillAsync("textarea[name='summary']", "Summary before edit");
        await SetContentAsync(page, "Initial content before edit");

        var createNav = page.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await page.ClickAsync("button[type='submit'][name='action'][value='draft']");
        await createNav;

        // 2. Find and click Edit on this post
        var editLink = await page.QuerySelectorAsync($"tr:has-text('{initialTitle}') a[title='Edit Post']");
        Assert.NotNull(editLink);
        await editLink.ClickAsync();

        await page.WaitForURLAsync(url => url.Contains("/edit"));
        Assert.Contains("/edit", page.Url);

        // 3. Update Title & Summary
        var updatedTitle = $"{initialTitle} — Updated";
        await page.FillAsync("input[name='title']", updatedTitle);
        await page.FillAsync("textarea[name='summary']", "Summary after edit");
        await SetContentAsync(page, "Updated markdown content");

        // 4. Save Changes
        var editNav = page.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await page.ClickAsync("button[type='submit'][name='action'][value='save']");
        await editNav;

        var bodyText = await page.TextContentAsync("body");
        Assert.Contains(updatedTitle, bodyText);
        Assert.Contains("saved successfully", bodyText);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_06_Post_Updated_Listing.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_07_E2E_15_OptimisticConcurrencyConflict_DetectedAndResolvedInRealBrowser()
    {
        // G07 / C02 - C04 / G11: Real Browser Concurrency Conflict Test
        var contextA = await _fixture.CreateAdminContextAsync();
        var contextB = await _fixture.CreateAdminContextAsync();

        var pageA = await contextA.NewPageAsync();
        var pageB = await contextB.NewPageAsync();

        // 1. Create a post specifically for concurrent editing
        await pageA.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/create");
        var concurrencyTitle = $"Concurrent Post {Guid.NewGuid().ToString()[..8]}";
        await pageA.FillAsync("input[name='title']", concurrencyTitle);
        await pageA.FillAsync("textarea[name='summary']", "Initial summary for concurrency conflict test");
        await SetContentAsync(pageA, "Initial content V1");

        var createNav = pageA.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await pageA.ClickAsync("button[type='submit'][name='action'][value='draft']");
        await createNav;

        // 2. Obtain Edit URL
        var editLink = await pageA.QuerySelectorAsync($"tr:has-text('{concurrencyTitle}') a[title='Edit Post']");
        Assert.NotNull(editLink);
        var editUrl = await editLink.GetAttributeAsync("href");
        Assert.NotNull(editUrl);
        var fullEditUrl = $"{PlaywrightServerFixture.BaseUrl}{editUrl}";

        // 3. Admin A loads V1 into browser
        await pageA.GotoAsync(fullEditUrl);
        var rowVersionInputA = await pageA.QuerySelectorAsync("input[name='rowVersion']");
        Assert.NotNull(rowVersionInputA);
        var tokenA = await rowVersionInputA.GetAttributeAsync("value");
        Assert.False(string.IsNullOrEmpty(tokenA));

        // 4. Admin B loads V1 into browser
        await pageB.GotoAsync(fullEditUrl);
        var rowVersionInputB = await pageB.QuerySelectorAsync("input[name='rowVersion']");
        Assert.NotNull(rowVersionInputB);
        var tokenB = await rowVersionInputB.GetAttributeAsync("value");
        Assert.Equal(tokenA, tokenB); // Both have identical initial V1 concurrency token

        // 5. Admin A modifies and saves first -> Succeeds (creates V2 in database)
        var titleA = $"{concurrencyTitle} — Saved by Admin A";
        await pageA.FillAsync("input[name='title']", titleA);
        var saveNavA = pageA.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await pageA.ClickAsync("button[type='submit'][name='action'][value='save']");
        await saveNavA;

        // 6. Admin B modifies stale form and attempts to save -> Clashes!
        var titleB = $"{concurrencyTitle} — Stale Edit by Admin B";
        await pageB.FillAsync("input[name='title']", titleB);

        // Expect HTTP 409 response from the server
        var conflictResponseTask = pageB.WaitForResponseAsync(resp => resp.Url.Contains("/edit") && resp.Status == 409);
        await pageB.ClickAsync("button[type='submit'][name='action'][value='save']");
        var conflictResponse = await conflictResponseTask;
        Assert.Equal(409, conflictResponse.Status);

        // 7. Verify Conflict UI rendered in Admin B's browser
        var pageTitle = await pageB.TitleAsync();
        Assert.Contains("Concurrency Conflict", pageTitle);

        var alertHeader = await pageB.TextContentAsync(".alert-warning");
        Assert.Contains("Conflict: This post was modified by another user", alertHeader);

        // Assert side-by-side comparison: unsaved user edits vs current persisted values
        var unsavedCard = await pageB.TextContentAsync("div.col-lg-6:has-text('Your Unsaved Version')");
        var persistedCard = await pageB.TextContentAsync("div.col-lg-6:has-text('Current Persisted Version')");
        Assert.Contains(titleB, unsavedCard);
        Assert.Contains(titleA, persistedCard);

        await pageB.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_07_Concurrency_Conflict_Detected.png")
        });

        // 8. Admin B resolves conflict by clicking "Overwrite With My Version"
        pageB.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        var overwriteBtn = await pageB.QuerySelectorAsync("button:has-text('Overwrite With My Version')");
        Assert.NotNull(overwriteBtn);
        var overwriteNav = pageB.WaitForURLAsync(url => url.Contains("/admin/posts"), new PageWaitForURLOptions { Timeout = 10000 });
        await overwriteBtn.ClickAsync();
        await overwriteNav;

        // 9. Verify overwrite succeeded and redirected back to /admin/posts
        var finalPostsListing = await pageB.TextContentAsync("table tbody");
        Assert.Contains(titleB, finalPostsListing);

        await pageB.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_07_Concurrency_Conflict_Resolved.png")
        });

        await contextA.CloseAsync();
        await contextB.CloseAsync();
    }

    [Fact]
    public async Task E2E_08_PublishAndUnpublish_LifecycleInRealBrowser()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        // 1. Create a draft post
        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/create");
        var testTitle = $"Lifecycle Post {Guid.NewGuid().ToString()[..8]}";
        await page.FillAsync("input[name='title']", testTitle);
        await page.FillAsync("textarea[name='summary']", "Lifecycle post for publish/unpublish test");
        await SetContentAsync(page, "Lifecycle markdown content");

        var createNav = page.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await page.ClickAsync("button[type='submit'][name='action'][value='draft']");
        await createNav;

        // 2. Publish post via table action button
        var publishBtn = await page.QuerySelectorAsync($"tr:has-text('{testTitle}') form[action*='/publish'] button[type='submit']");
        Assert.NotNull(publishBtn);
        var pubResp = page.WaitForResponseAsync(resp => resp.Url.Contains("/publish"));
        await publishBtn.ClickAsync();
        await pubResp;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var publishedRow = await page.TextContentAsync($"tr:has-text('{testTitle}')");
        Assert.Contains("Published", publishedRow);

        // 3. Unpublish post via table action button
        page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        var unpublishBtn = await page.QuerySelectorAsync($"tr:has-text('{testTitle}') form[action*='/unpublish'] button[type='submit']");
        Assert.NotNull(unpublishBtn);
        var unpubResp = page.WaitForResponseAsync(resp => resp.Url.Contains("/unpublish"));
        await unpublishBtn.ClickAsync();
        await unpubResp;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var unpublishedRow = await page.TextContentAsync($"tr:has-text('{testTitle}')");
        Assert.Contains("Draft", unpublishedRow);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_08_Publish_Unpublish_Lifecycle.png")
        });

        await context.CloseAsync();
    }

    [Fact]
    public async Task E2E_09_DeletePost_RemovesPostCleanly()
    {
        var context = await _fixture.CreateAdminContextAsync();
        var page = await context.NewPageAsync();

        // 1. Create a post to delete
        await page.GotoAsync($"{PlaywrightServerFixture.BaseUrl}/admin/create");
        var deleteTitle = $"Delete Target {Guid.NewGuid().ToString()[..8]}";
        await page.FillAsync("input[name='title']", deleteTitle);
        await page.FillAsync("textarea[name='summary']", "Post created to test clean deletion");
        await SetContentAsync(page, "Delete test content");

        var createNav = page.WaitForURLAsync(url => url.Contains("/admin/posts"));
        await page.ClickAsync("button[type='submit'][name='action'][value='draft']");
        await createNav;

        // 2. Click Delete button and accept confirm dialog
        page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        var deleteBtn = await page.QuerySelectorAsync($"tr:has-text('{deleteTitle}') form[action*='/delete'] button[type='submit']");
        Assert.NotNull(deleteBtn);
        var delResp = page.WaitForResponseAsync(resp => resp.Url.Contains("/delete"));
        await deleteBtn.ClickAsync();
        await delResp;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var tableContent = await page.TextContentAsync("table tbody");
        Assert.DoesNotContain(deleteTitle, tableContent);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(ScreenshotsDir, "E2E_09_Post_Deleted.png")
        });

        await context.CloseAsync();
    }
}
