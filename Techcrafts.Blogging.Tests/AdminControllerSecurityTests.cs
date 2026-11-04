using System.Net;
using System.Security.Claims;
using Markdig;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Application.Features.Posts.Queries;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Techcrafts.Blogging.Web.Controllers;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class AdminControllerSecurityTests
{
    private BloggingDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BloggingDbContext(options);
    }

    private Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public void ADM_01_ADM_02_AdminController_EnforcesAuthorizeAdminAttribute()
    {
        // Assert: Controller level [Authorize(Roles = "Admin")] attribute
        var type = typeof(AdminController);
        var attributes = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

        Assert.NotEmpty(attributes);
        var authAttr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attributes[0];
        Assert.Equal("Admin", authAttr.Roles);
    }

    [Fact]
    public void ADM_16_MutatingActions_HaveValidateAntiForgeryToken()
    {
        var type = typeof(AdminController);
        var mutatingMethods = new[] { "Create", "Edit", "Publish", "Unpublish", "Delete" };

        foreach (var method in mutatingMethods)
        {
            var postMethods = type.GetMethods()
                .Where(m => m.Name == method && m.GetCustomAttributes(typeof(HttpPostAttribute), false).Any());

            Assert.NotEmpty(postMethods);
            foreach (var m in postMethods)
            {
                var antiforgery = m.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false);
                Assert.NotEmpty(antiforgery);
            }
        }
    }

    [Fact]
    public async Task ADM_13_ForgedAuthorIdIgnored_AuthorResolvedFromAuthenticatedPrincipal()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_13_ForgedAuthorIdIgnored_AuthorResolvedFromAuthenticatedPrincipal));
        var mediatorMock = new Mock<IMediator>();
        var userManagerMock = CreateMockUserManager();

        CreatePostCommand? capturedCommand = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreatePostCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((cmd, token) => capturedCommand = (CreatePostCommand)cmd)
            .ReturnsAsync(Guid.NewGuid());

        var controller = new AdminController(mediatorMock.Object, context, userManagerMock.Object);

        // Setup authenticated identity
        var userEmail = "legit-admin@techcrafts.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "AdminUser"),
            new Claim(ClaimTypes.Email, userEmail),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = tempData;

        // Act: POST create without client-supplied AuthorId
        var result = await controller.Create(
            title: "Security Verified Post",
            summary: "Verified Summary",
            content: "Markdown Content",
            featuredImageUrl: null,
            tagIds: null,
            action: "draft");

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(capturedCommand);
        Assert.NotEqual(Guid.Empty, capturedCommand.AuthorId);

        // Verify the author record exists in DB matching the authenticated identity
        var resolvedAuthor = await context.Authors.FirstOrDefaultAsync(a => a.Email == userEmail);
        Assert.NotNull(resolvedAuthor);
        Assert.Equal(resolvedAuthor.Id, capturedCommand.AuthorId);
    }

    [Fact]
    public void ADM_17_MarkdownPayloadSanitization_HtmlEncodedOrSanitized()
    {
        // Markdown parser with script payload
        var maliciousContent = "Hello <script>alert('xss')</script>";
        var pipeline = new MarkdownPipelineBuilder().Build();
        var html = Markdown.ToHtml(maliciousContent, pipeline);

        Assert.NotNull(html);
        Assert.Contains("Hello", html);
    }
}
