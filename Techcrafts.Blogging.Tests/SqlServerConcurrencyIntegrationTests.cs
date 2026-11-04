using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Application.Features.Posts.Queries;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class SqlServerConcurrencyIntegrationTests : IDisposable
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? 
        "Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;";

    public SqlServerConcurrencyIntegrationTests()
    {
        CleanupDatabase();
    }

    public void Dispose()
    {
        CleanupDatabase();
    }

    private BloggingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new BloggingDbContext(options);
    }

    private void CleanupDatabase()
    {
        using var context = CreateContext();
        try { context.Database.Migrate(); } catch { context.Database.EnsureCreated(); }
        using var connection = context.Database.GetDbConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM PostReactions;
            DELETE FROM PostClaps;
            DELETE FROM Bookmarks;
            DELETE FROM PageViews;
            DELETE FROM PostEmbeddings;
            DELETE FROM PostTags;
            DELETE FROM Comments;
            DELETE FROM Posts;
            DELETE FROM Authors;
            DELETE FROM Tags;";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task SQL_C02_C03_ConcurrentEdit_RejectsStaleUpdateWithConcurrencyConflict()
    {
        // Arrange: Seed initial post
        Guid postId;
        using (var setupContext = CreateContext())
        {
            var author = new Author { DisplayName = "Concurrency Author", Email = "author@techcrafts.com" };
            setupContext.Authors.Add(author);
            await setupContext.SaveChangesAsync();

            var post = new Post
            {
                Title = "Original Version",
                Slug = "original-version",
                Summary = "Original Summary",
                Content = "Original Content",
                AuthorId = author.Id
            };
            setupContext.Posts.Add(post);
            await setupContext.SaveChangesAsync();
            postId = post.Id;
        }

        // Simulating Administrator A and Administrator B both loading the post at version V1
        string initialRowVersionA;
        string initialRowVersionB;

        using (var contextA = CreateContext())
        {
            var detailA = await new GetAdminPostByIdQueryHandler(contextA).Handle(new GetAdminPostByIdQuery(postId), CancellationToken.None);
            Assert.NotNull(detailA);
            Assert.NotNull(detailA.RowVersion);
            initialRowVersionA = detailA.RowVersion;
        }

        using (var contextB = CreateContext())
        {
            var detailB = await new GetAdminPostByIdQueryHandler(contextB).Handle(new GetAdminPostByIdQuery(postId), CancellationToken.None);
            Assert.NotNull(detailB);
            Assert.NotNull(detailB.RowVersion);
            initialRowVersionB = detailB.RowVersion;
        }

        Assert.Equal(initialRowVersionA, initialRowVersionB);

        // Act 1: Administrator A saves changes first -> Should succeed and generate V2
        using (var contextA = CreateContext())
        {
            var handlerA = new UpdatePostCommandHandler(contextA);
            var resultA = await handlerA.Handle(new UpdatePostCommand(
                postId,
                Title: "Version Updated By Admin A",
                Summary: "Summary By Admin A",
                Content: "Content By Admin A",
                FeaturedImageUrl: null,
                IsFeatured: false,
                TagIds: null,
                Action: "save",
                RowVersion: initialRowVersionA
            ), CancellationToken.None);

            Assert.Equal(UpdatePostResultStatus.Success, resultA.Status);
            Assert.NotNull(resultA.NewRowVersion);
            Assert.NotEqual(initialRowVersionA, resultA.NewRowVersion);
        }

        // Act 2: Administrator B attempts to save using stale version V1 -> Must be rejected with ConcurrencyConflict
        using (var contextB = CreateContext())
        {
            var handlerB = new UpdatePostCommandHandler(contextB);
            var resultB = await handlerB.Handle(new UpdatePostCommand(
                postId,
                Title: "Version Attempted By Admin B",
                Summary: "Summary By Admin B",
                Content: "Content By Admin B",
                FeaturedImageUrl: null,
                IsFeatured: false,
                TagIds: null,
                Action: "save",
                RowVersion: initialRowVersionB
            ), CancellationToken.None);

            // Assert: Administrator B encounters explicit ConcurrencyConflict and receives the current persisted state
            Assert.Equal(UpdatePostResultStatus.ConcurrencyConflict, resultB.Status);
            Assert.Equal("Version Updated By Admin A", resultB.CurrentTitle);
            Assert.Equal("Summary By Admin A", resultB.CurrentSummary);
            Assert.NotNull(resultB.NewRowVersion);
        }

        // Verify database state: Remains Admin A's changes, NOT overwritten by Admin B
        using (var verifyContext = CreateContext())
        {
            var finalPost = await verifyContext.Posts.FindAsync(postId);
            Assert.NotNull(finalPost);
            Assert.Equal("Version Updated By Admin A", finalPost.Title);
            Assert.Equal("Summary By Admin A", finalPost.Summary);
        }
    }

    [Fact]
    public async Task SQL_C04_ConflictResolution_OverwriteWithNewlyAcknowledgedRowVersionSucceeds()
    {
        // Arrange
        Guid postId;
        string initialVersion;
        using (var setup = CreateContext())
        {
            var author = new Author { DisplayName = "Author", Email = "res@techcrafts.com" };
            setup.Authors.Add(author);
            await setup.SaveChangesAsync();

            var post = new Post { Title = "Initial", Slug = "initial", Summary = "Sum", Content = "Body", AuthorId = author.Id };
            setup.Posts.Add(post);
            await setup.SaveChangesAsync();
            postId = post.Id;
            initialVersion = Convert.ToBase64String(post.RowVersion!);
        }

        // Admin A updates to V2
        string versionV2;
        using (var ctxA = CreateContext())
        {
            var resA = await new UpdatePostCommandHandler(ctxA).Handle(new UpdatePostCommand(
                postId, "V2 Title", "V2 Sum", "V2 Body", null, false, null, "save", initialVersion
            ), CancellationToken.None);
            Assert.Equal(UpdatePostResultStatus.Success, resA.Status);
            versionV2 = resA.NewRowVersion!;
        }

        // Admin B attempts save with V1 -> Fails with ConcurrencyConflict
        UpdatePostResult conflictResult;
        using (var ctxB = CreateContext())
        {
            conflictResult = await new UpdatePostCommandHandler(ctxB).Handle(new UpdatePostCommand(
                postId, "Admin B Wanted This", "Admin B Sum", "Admin B Body", null, false, null, "save", initialVersion
            ), CancellationToken.None);
            Assert.Equal(UpdatePostResultStatus.ConcurrencyConflict, conflictResult.Status);
            Assert.Equal(versionV2, conflictResult.NewRowVersion);
        }

        // Admin B resolves conflict by submitting with the acknowledged latest RowVersion (versionV2)
        using (var ctxBResolve = CreateContext())
        {
            var resolveResult = await new UpdatePostCommandHandler(ctxBResolve).Handle(new UpdatePostCommand(
                postId, "Admin B Resolved Title", "Admin B Resolved Sum", "Admin B Resolved Body", null, false, null, "save", conflictResult.NewRowVersion
            ), CancellationToken.None);

            Assert.Equal(UpdatePostResultStatus.Success, resolveResult.Status);
        }

        // Verify final state is Admin B's resolved version
        using (var finalCtx = CreateContext())
        {
            var finalPost = await finalCtx.Posts.FindAsync(postId);
            Assert.NotNull(finalPost);
            Assert.Equal("Admin B Resolved Title", finalPost.Title);
        }
    }

    [Fact]
    public async Task SQL_C04_SecondRaceConflict_AdminCOverwritesWhileAdminBViewsConflictScreen_AdminBMustConflictAgain()
    {
        // 1. Arrange: Create initial post at V1
        Guid postId;
        string initialVersion;
        using (var setup = CreateContext())
        {
            var author = new Author { DisplayName = "Author", Email = "race2@techcrafts.com" };
            setup.Authors.Add(author);
            await setup.SaveChangesAsync();

            var post = new Post { Title = "Version 1", Slug = "v1-post", Summary = "Sum 1", Content = "Body 1", AuthorId = author.Id };
            setup.Posts.Add(post);
            await setup.SaveChangesAsync();
            postId = post.Id;
            initialVersion = Convert.ToBase64String(post.RowVersion!);
        }

        // 2. Admin A updates to V2
        string versionV2;
        using (var ctxA = CreateContext())
        {
            var resA = await new UpdatePostCommandHandler(ctxA).Handle(new UpdatePostCommand(
                postId, "V2 Title By Admin A", "V2 Sum", "V2 Body", null, false, null, "save", initialVersion
            ), CancellationToken.None);
            Assert.Equal(UpdatePostResultStatus.Success, resA.Status);
            versionV2 = resA.NewRowVersion!;
        }

        // 3. Admin B attempts save with stale V1 -> Fails with ConcurrencyConflict and receives V2 token
        UpdatePostResult conflictResultB;
        using (var ctxB = CreateContext())
        {
            conflictResultB = await new UpdatePostCommandHandler(ctxB).Handle(new UpdatePostCommand(
                postId, "Admin B Stale Attempt", "B Sum", "B Body", null, false, null, "save", initialVersion
            ), CancellationToken.None);
            Assert.Equal(UpdatePostResultStatus.ConcurrencyConflict, conflictResultB.Status);
            Assert.Equal(versionV2, conflictResultB.NewRowVersion);
        }

        // 4. SECOND RACE: While Admin B is reviewing the conflict screen with V2, Admin C intervenes and updates to V3!
        string versionV3;
        using (var ctxC = CreateContext())
        {
            var resC = await new UpdatePostCommandHandler(ctxC).Handle(new UpdatePostCommand(
                postId, "V3 Title By Admin C", "V3 Sum", "V3 Body", null, false, null, "save", versionV2
            ), CancellationToken.None);
            Assert.Equal(UpdatePostResultStatus.Success, resC.Status);
            versionV3 = resC.NewRowVersion!;
            Assert.NotEqual(versionV2, versionV3);
        }

        // 5. Admin B now clicks "Overwrite With My Version" using V2 token -> MUST CONFLICT AGAIN!
        using (var ctxBOverwrite = CreateContext())
        {
            var secondConflictResult = await new UpdatePostCommandHandler(ctxBOverwrite).Handle(new UpdatePostCommand(
                postId, "Admin B Overwrite Attempt", "B Sum Overwrite", "B Body Overwrite", null, false, null, "save", conflictResultB.NewRowVersion
            ), CancellationToken.None);

            // Assert: Second race detected! B cannot silently overwrite C's newly committed changes!
            Assert.Equal(UpdatePostResultStatus.ConcurrencyConflict, secondConflictResult.Status);
            Assert.Equal("V3 Title By Admin C", secondConflictResult.CurrentTitle);
            Assert.Equal("V3 Body", secondConflictResult.CurrentContent);
            Assert.Equal(versionV3, secondConflictResult.NewRowVersion);
        }

        // 6. Final verification: Post remains at Admin C's V3 in database
        using (var finalCtx = CreateContext())
        {
            var postInDb = await finalCtx.Posts.FindAsync(postId);
            Assert.NotNull(postInDb);
            Assert.Equal("V3 Title By Admin C", postInDb.Title);
            Assert.Equal(versionV3, Convert.ToBase64String(postInDb.RowVersion!));
        }
    }
}
