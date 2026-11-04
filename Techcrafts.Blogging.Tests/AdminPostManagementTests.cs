using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Common.Utils;
using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Application.Features.Posts.Queries;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Xunit;

namespace Techcrafts.Blogging.Tests;

public class AdminPostManagementTests
{
    private BloggingDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new BloggingDbContext(options);
    }

    [Fact]
    public async Task ADM_03_AdminOpensListingWithMixedStatuses_AllEligibleStatusesPresent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_03_AdminOpensListingWithMixedStatuses_AllEligibleStatusesPresent));
        var author = new Author { DisplayName = "Admin Author", Email = "admin@techcrafts.com" }.WithId(Guid.NewGuid());
        context.Authors.Add(author);

        context.Posts.AddRange(
            new Post { Title = "Draft 1", Slug = "draft-1", Summary = "Summary 1", Status = PostStatus.Draft, AuthorId = author.Id }.WithId(Guid.NewGuid()),
            new Post { Title = "Published 1", Slug = "pub-1", Summary = "Summary 2", Status = PostStatus.Published, AuthorId = author.Id, PublishedDate = DateTimeOffset.UtcNow }.WithId(Guid.NewGuid()),
            new Post { Title = "Scheduled 1", Slug = "sched-1", Summary = "Summary 3", Status = PostStatus.Scheduled, AuthorId = author.Id }.WithId(Guid.NewGuid())
        );
        await context.SaveChangesAsync();

        var handler = new GetAdminPostsQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetAdminPostsQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.TotalDrafts);
        Assert.Equal(1, result.TotalPublished);
        Assert.Equal(1, result.TotalScheduled);
        Assert.Contains(result.Posts, p => p.Status == PostStatus.Draft);
        Assert.Contains(result.Posts, p => p.Status == PostStatus.Published);
        Assert.Contains(result.Posts, p => p.Status == PostStatus.Scheduled);
    }

    [Fact]
    public async Task ADM_04_SearchFilterSortCombined_AccurateDeterministicResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_04_SearchFilterSortCombined_AccurateDeterministicResults));
        var author = new Author { DisplayName = "Author", Email = "author@techcrafts.com" }.WithId(Guid.NewGuid());
        context.Authors.Add(author);

        context.Posts.AddRange(
            new Post { Title = "ASP.NET Core Architecture", Slug = "aspnet-arch", Summary = "Clean arch guide", Status = PostStatus.Published, AuthorId = author.Id, ViewCount = 100 }.WithId(Guid.NewGuid()),
            new Post { Title = "Python Basics", Slug = "python-basics", Summary = "Python intro", Status = PostStatus.Published, AuthorId = author.Id, ViewCount = 50 }.WithId(Guid.NewGuid()),
            new Post { Title = "ASP.NET Core Security", Slug = "aspnet-security", Summary = "Web security guide", Status = PostStatus.Draft, AuthorId = author.Id, ViewCount = 10 }.WithId(Guid.NewGuid())
        );
        await context.SaveChangesAsync();

        var handler = new GetAdminPostsQueryHandler(context);

        // Act: Filter by status Published + search "ASP.NET"
        var result = await handler.Handle(new GetAdminPostsQuery(Search: "ASP.NET", Status: PostStatus.Published), CancellationToken.None);

        // Assert
        Assert.Single(result.Posts);
        Assert.Equal("ASP.NET Core Architecture", result.Posts[0].Title);
    }

    [Fact]
    public async Task ADM_05_PaginationWithNoLargeInvalidPageInputs_ValidatedClampedNoCrashes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_05_PaginationWithNoLargeInvalidPageInputs_ValidatedClampedNoCrashes));
        var handler = new GetAdminPostsQueryHandler(context);

        // Act: Negative page and huge pageSize
        var resultNegative = await handler.Handle(new GetAdminPostsQuery(Page: -5, PageSize: 5000), CancellationToken.None);
        var resultZero = await handler.Handle(new GetAdminPostsQuery(Page: 0, PageSize: -10), CancellationToken.None);

        // Assert
        Assert.Equal(1, resultNegative.Page);
        Assert.Equal(50, resultNegative.PageSize); // Clamped to max 50
        Assert.Equal(1, resultZero.Page);
        Assert.Equal(10, resultZero.PageSize); // Defaulted to 10
    }

    [Fact]
    public async Task ADM_06_AdminEditsADraft_ChangesPersistAndRemainsDraft()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_06_AdminEditsADraft_ChangesPersistAndRemainsDraft));
        var postId = Guid.NewGuid();
        context.Posts.Add(new Post
        {
            Title = "Original Draft",
            Slug = "orig-draft",
            Summary = "Original Summary",
            Content = "Original Content",
            Status = PostStatus.Draft
        }.WithId(postId));
        await context.SaveChangesAsync();

        var handler = new UpdatePostCommandHandler(context);

        // Act
        var result = await handler.Handle(new UpdatePostCommand(
            postId,
            Title: "Updated Draft Title",
            Summary: "Updated Summary",
            Content: "Updated Content with more words here to compute reading time.",
            FeaturedImageUrl: "/img/new.jpg",
            IsFeatured: true,
            TagIds: new List<Guid>(),
            Action: "save"
        ), CancellationToken.None);

        // Assert
        Assert.Equal(UpdatePostResultStatus.Success, result.Status);
        var updated = await context.Posts.FindAsync(postId);
        Assert.NotNull(updated);
        Assert.Equal("Updated Draft Title", updated.Title);
        Assert.Equal(PostStatus.Draft, updated.Status);
        Assert.Null(updated.PublishedDate);
        Assert.True(updated.IsFeatured);
    }

    [Fact]
    public async Task ADM_07_AdminEditsPublishedPost_ChangesPersistPublicationSemanticsPreserved()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_07_AdminEditsPublishedPost_ChangesPersistPublicationSemanticsPreserved));
        var postId = Guid.NewGuid();
        var initialPublishedDate = DateTimeOffset.UtcNow.AddDays(-5);
        context.Posts.Add(new Post
        {
            Title = "Published Article",
            Slug = "published-article",
            Summary = "Summary",
            Content = "Content",
            Status = PostStatus.Published,
            PublishedDate = initialPublishedDate
        }.WithId(postId));
        await context.SaveChangesAsync();

        var handler = new UpdatePostCommandHandler(context);

        // Act: Save Changes
        var result = await handler.Handle(new UpdatePostCommand(
            postId,
            Title: "Published Article (Edited)",
            Summary: "Updated Summary",
            Content: "Updated Content",
            FeaturedImageUrl: null,
            IsFeatured: false,
            TagIds: null,
            Action: "save"
        ), CancellationToken.None);

        // Assert
        Assert.Equal(UpdatePostResultStatus.Success, result.Status);
        var updated = await context.Posts.FindAsync(postId);
        Assert.NotNull(updated);
        Assert.Equal("Published Article (Edited)", updated.Title);
        Assert.Equal(PostStatus.Published, updated.Status);
        Assert.Equal(initialPublishedDate, updated.PublishedDate); // PublishedDate is NOT reset
    }

    [Fact]
    public async Task ADM_08_AdminSavesNewPostAsDraft_NotPubliclyVisible()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_08_AdminSavesNewPostAsDraft_NotPubliclyVisible));
        var authorId = Guid.NewGuid();
        var createHandler = new CreatePostCommandHandler(context);

        // Act: Create as draft (Publish = false)
        var newPostId = await createHandler.Handle(new CreatePostCommand(
            Title: "Brand New Draft",
            Summary: "Draft Summary",
            Content: "Top secret internal draft content.",
            FeaturedImageUrl: null,
            AuthorId: authorId,
            Publish: false
        ), CancellationToken.None);

        // Assert in DB
        var post = await context.Posts.FindAsync(newPostId);
        Assert.NotNull(post);
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.Null(post.PublishedDate);

        // Verify public query does NOT return it
        var publicListHandler = new GetPublicPostsQueryHandler(context);
        var publicList = await publicListHandler.Handle(new GetPublicPostsQuery(1), CancellationToken.None);
        Assert.DoesNotContain(publicList.Posts, p => p.Id == newPostId);

        // Verify public slug query rejects it
        var slugHandler = new GetPostBySlugQueryHandler(context);
        var publicDetail = await slugHandler.Handle(new GetPostBySlugQuery(post.Slug, AllowDraft: false), CancellationToken.None);
        Assert.Null(publicDetail);
    }

    [Fact]
    public async Task ADM_09_AdminPublishesDraft_PublicViewShowsIt()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_09_AdminPublishesDraft_PublicViewShowsIt));
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        context.Posts.Add(new Post
        {
            Title = "Draft to Publish",
            Slug = "draft-to-publish",
            Summary = "Summary",
            Content = "Content",
            AuthorId = authorId,
            Status = PostStatus.Draft
        }.WithId(postId));
        await context.SaveChangesAsync();

        var publishHandler = new PublishPostCommandHandler(context);

        // Act
        var result = await publishHandler.Handle(new PublishPostCommand(postId), CancellationToken.None);

        // Assert
        Assert.True(result);
        var post = await context.Posts.FindAsync(postId);
        Assert.NotNull(post);
        Assert.Equal(PostStatus.Published, post.Status);
        Assert.NotNull(post.PublishedDate);

        // Public slug query now finds it
        var slugHandler = new GetPostBySlugQueryHandler(context);
        var publicDetail = await slugHandler.Handle(new GetPostBySlugQuery(post.Slug, AllowDraft: false), CancellationToken.None);
        Assert.NotNull(publicDetail);
        Assert.Equal("Draft to Publish", publicDetail.Title);
    }

    [Fact]
    public async Task ADM_10_AdminUnpublishesPost_PublicListingAndDirectUrlDenyIt()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_10_AdminUnpublishesPost_PublicListingAndDirectUrlDenyIt));
        var postId = Guid.NewGuid();
        context.Posts.Add(new Post
        {
            Title = "Article to Unpublish",
            Slug = "article-to-unpublish",
            Summary = "Summary",
            Content = "Content",
            Status = PostStatus.Published,
            PublishedDate = DateTimeOffset.UtcNow
        }.WithId(postId));
        await context.SaveChangesAsync();

        var unpublishHandler = new UnpublishPostCommandHandler(context);

        // Act
        var result = await unpublishHandler.Handle(new UnpublishPostCommand(postId), CancellationToken.None);

        // Assert
        Assert.True(result);
        var post = await context.Posts.FindAsync(postId);
        Assert.NotNull(post);
        Assert.Equal(PostStatus.Draft, post.Status);

        // Denied on public list
        var listHandler = new GetPublicPostsQueryHandler(context);
        var list = await listHandler.Handle(new GetPublicPostsQuery(1), CancellationToken.None);
        Assert.DoesNotContain(list.Posts, p => p.Id == postId);

        // Denied on public direct URL
        var slugHandler = new GetPostBySlugQueryHandler(context);
        var publicDetail = await slugHandler.Handle(new GetPostBySlugQuery("article-to-unpublish", AllowDraft: false), CancellationToken.None);
        Assert.Null(publicDetail);
    }

    [Fact]
    public async Task ADM_11_RepeatedPublish_NoAccidentalPublishedDateReset()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_11_RepeatedPublish_NoAccidentalPublishedDateReset));
        var postId = Guid.NewGuid();
        var initialPublishedDate = DateTimeOffset.UtcNow.AddDays(-30);
        context.Posts.Add(new Post
        {
            Title = "Historic Post",
            Slug = "historic-post",
            Summary = "Summary",
            Content = "Content",
            Status = PostStatus.Published,
            PublishedDate = initialPublishedDate
        }.WithId(postId));
        await context.SaveChangesAsync();

        var publishHandler = new PublishPostCommandHandler(context);

        // Act: Publish again
        await publishHandler.Handle(new PublishPostCommand(postId), CancellationToken.None);

        // Assert
        var post = await context.Posts.FindAsync(postId);
        Assert.NotNull(post);
        Assert.Equal(initialPublishedDate, post.PublishedDate);
    }

    [Fact]
    public async Task ADM_12_DuplicateOrInvalidSlug_SafeDeterministicResolution()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_12_DuplicateOrInvalidSlug_SafeDeterministicResolution));
        var authorId = Guid.NewGuid();
        var createHandler = new CreatePostCommandHandler(context);

        // Act: Create 3 posts with identical title
        var id1 = await createHandler.Handle(new CreatePostCommand("Microservices In .NET", "Sum 1", "Content 1", null, authorId), CancellationToken.None);
        var id2 = await createHandler.Handle(new CreatePostCommand("Microservices In .NET", "Sum 2", "Content 2", null, authorId), CancellationToken.None);
        var id3 = await createHandler.Handle(new CreatePostCommand("Microservices In .NET", "Sum 3", "Content 3", null, authorId), CancellationToken.None);

        // Assert
        var post1 = await context.Posts.FindAsync(id1);
        var post2 = await context.Posts.FindAsync(id2);
        var post3 = await context.Posts.FindAsync(id3);

        Assert.NotNull(post1);
        Assert.NotNull(post2);
        Assert.NotNull(post3);

        Assert.Equal("microservices-in-net", post1.Slug);
        Assert.Equal("microservices-in-net-1", post2.Slug);
        Assert.Equal("microservices-in-net-2", post3.Slug);
    }

    [Fact]
    public async Task ADM_18_NonexistentPostId_ReturnsControlledFalseOrNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_18_NonexistentPostId_ReturnsControlledFalseOrNull));
        var nonExistentId = Guid.NewGuid();

        var getDetailHandler = new GetAdminPostByIdQueryHandler(context);
        var updateHandler = new UpdatePostCommandHandler(context);
        var publishHandler = new PublishPostCommandHandler(context);
        var deleteHandler = new DeletePostCommandHandler(context);

        // Act & Assert
        var updateResult = await updateHandler.Handle(new UpdatePostCommand(nonExistentId, "T", "S", "C", null, false, null, "save"), CancellationToken.None);
        Assert.Null(await getDetailHandler.Handle(new GetAdminPostByIdQuery(nonExistentId), CancellationToken.None));
        Assert.Equal(UpdatePostResultStatus.NotFound, updateResult.Status);
        Assert.False(await publishHandler.Handle(new PublishPostCommand(nonExistentId), CancellationToken.None));
        Assert.False(await deleteHandler.Handle(new DeletePostCommand(nonExistentId), CancellationToken.None));
    }

    [Fact]
    public async Task ADM_19_EditRecalculatesReadingTime_CorrectPersistedResult()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_19_EditRecalculatesReadingTime_CorrectPersistedResult));
        var postId = Guid.NewGuid();
        context.Posts.Add(new Post
        {
            Title = "Title",
            Slug = "slug",
            Summary = "Summary",
            Content = "Few words",
            ReadingTimeMinutes = 1
        }.WithId(postId));
        await context.SaveChangesAsync();

        var updateHandler = new UpdatePostCommandHandler(context);

        // 600 words -> ~3 minutes reading time (at 200 wpm)
        var longContent = string.Join(" ", Enumerable.Repeat("word", 600));

        // Act
        await updateHandler.Handle(new UpdatePostCommand(postId, "Title", "Summary", longContent, null, false, null, "save"), CancellationToken.None);

        // Assert
        var post = await context.Posts.FindAsync(postId);
        Assert.NotNull(post);
        Assert.Equal(3, post.ReadingTimeMinutes);
    }

    [Fact]
    public async Task ADM_21_DeletePost_CleansUpRelatedDependentDataCleanly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ADM_21_DeletePost_CleansUpRelatedDependentDataCleanly));
        var postId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        context.Posts.Add(new Post { Title = "To Delete", Slug = "to-delete", Summary = "S", Content = "C" }.WithId(postId));
        context.Tags.Add(new Tag { Name = "C#", Slug = "csharp" }.WithId(tagId));
        context.PostTags.Add(new PostTag { PostId = postId, TagId = tagId });
        context.Comments.Add(new Comment { PostId = postId, Content = "Great article!", AuthorName = "Reader" }.WithId(Guid.NewGuid()));
        context.PostReactions.Add(new PostReaction { PostId = postId, Reaction = ReactionType.Like, ReaderHash = "hash1" }.WithId(Guid.NewGuid()));
        context.PostClaps.Add(new PostClap { PostId = postId, Count = 5, ReaderIdentifier = "hash1" }.WithId(Guid.NewGuid()));
        context.Bookmarks.Add(new Bookmark { PostId = postId, UserId = Guid.NewGuid() }.WithId(Guid.NewGuid()));
        context.PageViews.Add(new PageView { PostId = postId, Timestamp = DateTimeOffset.UtcNow }.WithId(Guid.NewGuid()));
        await context.SaveChangesAsync();

        var deleteHandler = new DeletePostCommandHandler(context);

        // Act
        var success = await deleteHandler.Handle(new DeletePostCommand(postId), CancellationToken.None);

        // Assert
        Assert.True(success);
        Assert.Null(await context.Posts.FindAsync(postId));
        Assert.Empty(await context.Comments.Where(c => c.PostId == postId).ToListAsync());
        Assert.Empty(await context.PostReactions.Where(r => r.PostId == postId).ToListAsync());
        Assert.Empty(await context.PostClaps.Where(c => c.PostId == postId).ToListAsync());
        Assert.Empty(await context.Bookmarks.Where(b => b.PostId == postId).ToListAsync());
        Assert.Empty(await context.PageViews.Where(p => p.PostId == postId).ToListAsync());
    }
}
