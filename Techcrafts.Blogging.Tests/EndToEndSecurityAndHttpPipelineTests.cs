using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class EndToEndSecurityAndHttpPipelineTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EndToEndSecurityAndHttpPipelineTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> EnsureTestAuthorIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
        var author = await db.Authors.FirstOrDefaultAsync();
        if (author != null)
        {
            return author.Id;
        }

        author = new Author
        {
            DisplayName = "Test E2E Author",
            Email = "test-e2e@techcrafts.com",
            Bio = "Author for E2E Tests"
        };
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return author.Id;
    }

    [Fact]
    public async Task E2E_01_AnonymousVisitsAdminPosts_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/admin/posts");

        // Assert: 302 Redirect to /account/login (or /Setup if initial check occurs)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location.ToString();
        Assert.True(location.Contains("/account/login") || location.Contains("/Setup"));
    }

    [Fact]
    public async Task E2E_01_AnonymousVisitsAdminCreate_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/admin/create");

        // Assert: 302 Redirect to /account/login (or /Setup)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location.ToString();
        Assert.True(location.Contains("/account/login") || location.Contains("/Setup"));
    }

    [Fact]
    public async Task E2E_05_PublicPostDetail_DoesNotDiscloseDraftPost()
    {
        // Arrange: Seed a draft post into the database
        var authorId = await EnsureTestAuthorIdAsync();
        var slug = "top-secret-draft-" + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
            db.Posts.Add(new Post
            {
                Title = "Top Secret Draft",
                Slug = slug,
                Summary = "Secret Summary",
                Content = "Secret Markdown Content",
                AuthorId = authorId,
                Status = PostStatus.Draft
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: Public user tries to fetch draft by slug
        var response = await client.GetAsync($"/posts/{slug}");

        // Assert: Public route returns 404 NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task E2E_08_PublicPostDetail_DisclosesPublishedPost()
    {
        // Arrange: Seed a published post into the database
        var authorId = await EnsureTestAuthorIdAsync();
        var slug = "published-article-" + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
            db.Posts.Add(new Post
            {
                Title = "Published Article",
                Slug = slug,
                Summary = "Public Summary",
                Content = "Public Markdown Content",
                AuthorId = authorId,
                Status = PostStatus.Published,
                PublishedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: Public user fetches published post
        var response = await client.GetAsync($"/posts/{slug}");

        // Assert: 200 OK and content contains title
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Published Article", html);
    }

    [Fact]
    public async Task E2E_18_MutationsWithoutCsrfToken_AreRejected()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: Direct POST without token to unpublish
        var fakeId = Guid.NewGuid();
        var response = await client.PostAsync($"/admin/posts/{fakeId}/unpublish", new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert: Should either redirect to login (if unauthenticated) or return 400 Bad Request (antiforgery failure)
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task E2E_19_SanitizedHtml_RendersSafelyWithoutExecutingScript()
    {
        // Arrange: Create a post with stored XSS payload
        var authorId = await EnsureTestAuthorIdAsync();
        var slug = "xss-test-" + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
            db.Posts.Add(new Post
            {
                Title = "XSS Test Post",
                Slug = slug,
                Summary = "Testing XSS",
                Content = "Here is safe text. <script>alert('pwned')</script> <img src=x onerror=alert(1)> [click me](javascript:alert(1))",
                AuthorId = authorId,
                Status = PostStatus.Published,
                PublishedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync($"/posts/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        
        // Assert malicious script tag and event handlers are neutralized
        Assert.DoesNotContain("<script>alert('pwned')</script>", html);
        Assert.DoesNotContain("onerror=alert(1)", html);
        Assert.DoesNotContain("href=\"javascript:", html);
        Assert.Contains("Here is safe text.", html);
    }
}
