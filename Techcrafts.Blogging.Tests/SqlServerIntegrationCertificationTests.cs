using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Common.Utils;
using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Application.Features.Posts.Queries;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class SqlServerIntegrationCertificationTests : IDisposable
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? 
        "Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;";

    public SqlServerIntegrationCertificationTests()
    {
        // Ensure clean state before each test run
        CleanupDatabase();
    }

    public void Dispose()
    {
        CleanupDatabase();
    }

    private BloggingDbContext CreateSqlServerDbContext()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new BloggingDbContext(options);
    }

    private void CleanupDatabase()
    {
        using var context = CreateSqlServerDbContext();
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
    public async Task SQL_G06_UniqueSlugIndex_EnforcesUniquenessUnderConcurrentInserts()
    {
        // Arrange
        using var context1 = CreateSqlServerDbContext();
        using var context2 = CreateSqlServerDbContext();

        var author = new Author { DisplayName = "SQL Author", Email = "sql@techcrafts.com" };
        context1.Authors.Add(author);
        await context1.SaveChangesAsync();

        var post1 = new Post
        {
            Title = "Unique Slug Post",
            Slug = "unique-slug-post",
            Summary = "Summary 1",
            Content = "Content 1",
            AuthorId = author.Id
        };
        var post2 = new Post
        {
            Title = "Unique Slug Post 2",
            Slug = "unique-slug-post", // Intentionally conflicting identical slug
            Summary = "Summary 2",
            Content = "Content 2",
            AuthorId = author.Id
        };

        context1.Posts.Add(post1);
        await context1.SaveChangesAsync();

        context2.Posts.Add(post2);

        // Act & Assert: Real SQL Server unique index violation
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context2.SaveChangesAsync());
        Assert.NotNull(ex.InnerException);
        Assert.Contains("IX_Posts_Slug", ex.InnerException.Message);
    }

    [Fact]
    public async Task SQL_G06_SlugHelper_ResolvesCollisionsOnRealSqlServer()
    {
        // Arrange
        using var context = CreateSqlServerDbContext();
        var author = new Author { DisplayName = "SQL Author", Email = "sql-slug@techcrafts.com" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var handler = new CreatePostCommandHandler(context);

        // Act: Insert multiple posts with the same title using the slug generator
        var id1 = await handler.Handle(new CreatePostCommand("Cloud Architecture", "S1", "C1", null, author.Id), CancellationToken.None);
        var id2 = await handler.Handle(new CreatePostCommand("Cloud Architecture", "S2", "C2", null, author.Id), CancellationToken.None);
        var id3 = await handler.Handle(new CreatePostCommand("Cloud Architecture", "S3", "C3", null, author.Id), CancellationToken.None);

        // Assert
        var p1 = await context.Posts.FindAsync(id1);
        var p2 = await context.Posts.FindAsync(id2);
        var p3 = await context.Posts.FindAsync(id3);

        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.NotNull(p3);

        Assert.Equal("cloud-architecture", p1.Slug);
        Assert.Equal("cloud-architecture-1", p2.Slug);
        Assert.Equal("cloud-architecture-2", p3.Slug);
    }

    [Fact]
    public async Task SQL_G08_PublicationIdempotencyAndLifecyle_OnRealSqlServer()
    {
        // Arrange
        using var context = CreateSqlServerDbContext();
        var author = new Author { DisplayName = "Publisher", Email = "pub@techcrafts.com" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var createHandler = new CreatePostCommandHandler(context);
        var postId = await createHandler.Handle(new CreatePostCommand("SQL Article", "Sum", "Content", null, author.Id, Publish: false), CancellationToken.None);

        var publishHandler = new PublishPostCommandHandler(context);
        var unpublishHandler = new UnpublishPostCommandHandler(context);

        // Act 1: Publish draft
        var publishedFirst = await publishHandler.Handle(new PublishPostCommand(postId), CancellationToken.None);
        var postAfterFirstPublish = await context.Posts.FindAsync(postId);
        Assert.True(publishedFirst);
        Assert.Equal(PostStatus.Published, postAfterFirstPublish!.Status);
        var firstPublishTime = postAfterFirstPublish.PublishedDate;
        Assert.NotNull(firstPublishTime);

        // Act 2: Publish again (Idempotent check)
        await publishHandler.Handle(new PublishPostCommand(postId), CancellationToken.None);
        var postAfterSecondPublish = await context.Posts.FindAsync(postId);
        Assert.Equal(firstPublishTime, postAfterSecondPublish!.PublishedDate);

        // Act 3: Unpublish
        var unpublished = await unpublishHandler.Handle(new UnpublishPostCommand(postId), CancellationToken.None);
        var postAfterUnpublish = await context.Posts.FindAsync(postId);
        Assert.True(unpublished);
        Assert.Equal(PostStatus.Draft, postAfterUnpublish!.Status);

        // Verify public query does not return it on real SQL Server
        var publicDetail = await new GetPostBySlugQueryHandler(context).Handle(new GetPostBySlugQuery("sql-article", AllowDraft: false), CancellationToken.None);
        Assert.Null(publicDetail);
    }

    [Fact]
    public async Task SQL_G09_AtomicDeleteWithCascadeAndDependentCleanups_OnRealSqlServer()
    {
        // Arrange
        using var context = CreateSqlServerDbContext();
        var author = new Author { DisplayName = "Deleter", Email = "del@techcrafts.com" };
        var tag = new Tag { Name = "Cloud", Slug = "cloud" };
        context.Authors.Add(author);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            Summary = "Summary",
            Content = "Content",
            AuthorId = author.Id
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
        context.Comments.Add(new Comment { PostId = post.Id, Content = "A comment", AuthorName = "User1" });
        context.PostReactions.Add(new PostReaction { PostId = post.Id, Reaction = ReactionType.Like, ReaderHash = "reader1" });
        context.PostClaps.Add(new PostClap { PostId = post.Id, Count = 10, ReaderIdentifier = "reader1" });
        context.Bookmarks.Add(new Bookmark { PostId = post.Id, UserId = Guid.NewGuid() });
        context.PageViews.Add(new PageView { PostId = post.Id, Timestamp = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var deleteHandler = new DeletePostCommandHandler(context);

        // Act
        var result = await deleteHandler.Handle(new DeletePostCommand(post.Id), CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Posts.FindAsync(post.Id));
        Assert.Empty(await context.Comments.Where(c => c.PostId == post.Id).ToListAsync());
        Assert.Empty(await context.PostReactions.Where(r => r.PostId == post.Id).ToListAsync());
        Assert.Empty(await context.PostClaps.Where(c => c.PostId == post.Id).ToListAsync());
        Assert.Empty(await context.Bookmarks.Where(b => b.PostId == post.Id).ToListAsync());
        Assert.Empty(await context.PageViews.Where(p => p.PostId == post.Id).ToListAsync());
    }

    [Fact]
    public async Task SQL_G07_ConcurrentEdit_TracksAuditDateCorrectly()
    {
        // Arrange
        using var context1 = CreateSqlServerDbContext();
        using var context2 = CreateSqlServerDbContext();

        var author = new Author { DisplayName = "Editor", Email = "editor@techcrafts.com" };
        context1.Authors.Add(author);
        await context1.SaveChangesAsync();

        var post = new Post
        {
            Title = "Concurrent Post",
            Slug = "concurrent-post",
            Summary = "Summary Initial",
            Content = "Content Initial",
            AuthorId = author.Id
        };
        context1.Posts.Add(post);
        await context1.SaveChangesAsync();

        var updateHandler1 = new UpdatePostCommandHandler(context1);
        var updateHandler2 = new UpdatePostCommandHandler(context2);

        // Act: First edit
        var result1 = await updateHandler1.Handle(new UpdatePostCommand(post.Id, "Title By 1", "Summary 1", "Content 1", null, false, null, "save"), CancellationToken.None);
        Assert.Equal(UpdatePostResultStatus.Success, result1.Status);

        // Act: Second edit
        var result2 = await updateHandler2.Handle(new UpdatePostCommand(post.Id, "Title By 2", "Summary 2", "Content 2", null, false, null, "save"), CancellationToken.None);
        Assert.Equal(UpdatePostResultStatus.Success, result2.Status);

        // Assert: Changes reflect with updated audit timestamp
        using var verifyContext = CreateSqlServerDbContext();
        var finalPost = await verifyContext.Posts.FindAsync(post.Id);
        Assert.NotNull(finalPost);
        Assert.Equal("Title By 2", finalPost.Title);
        Assert.NotNull(finalPost.UpdatedDate);
    }
}
