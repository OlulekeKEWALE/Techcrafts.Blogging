using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(BloggingDbContext context, string adminEmail = "admin@techcrafts.com")
    {
        if (await context.Posts.AnyAsync())
        {
            return; // DB has been seeded
        }

        var author = new Author
        {
            DisplayName = "Okewale Akintunde Oluleke",
            Email = adminEmail,
            Bio = "Software Engineer, Software Architect, and Engineering Leader",
            AvatarUrl = "/images/logo.png"
        };

        var posts = new List<Post>
        {
            new Post
            {
                Title = "The Evolution of Software Architecture in the Cloud Native Era",
                Slug = "evolution-software-architecture-cloud-native",
                Summary = "Exploring how microservices, serverless, and kubernetes have radically shifted the way we design scalable systems over the last decade.",
                Content = @"# The Evolution of Software Architecture

The shift from monolithic applications to cloud-native architectures represents one of the most profound changes in software engineering history. 

In this article, we'll explore:
1. The limitations of traditional monoliths
2. The rise of Microservices and API gateways
3. How Kubernetes became the de-facto orchestrator
4. The future: Serverless and Event-Driven systems

## Why Move Away From Monoliths?
As engineering teams grow, a single codebase becomes a bottleneck. Compile times increase, merge conflicts become a daily struggle, and deployments are inherently risky. 

## The Cloud Native Promise
By breaking domains into isolated bounded contexts, we achieve independent deployability. This allows teams to iterate rapidly without fear of breaking unrelated features.

```csharp
// Example of a minimal API endpoint
app.MapGet(""/api/products"", async (IProductService service) => 
{
    return await service.GetAllAsync();
});
```

*More content coming soon.*",
                Status = PostStatus.Published,
                FeaturedImageUrl = "/images/header1.jpg",
                PublishedDate = DateTimeOffset.UtcNow.AddDays(-10),
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-12),
                ReadingTimeMinutes = 5,
                IsFeatured = true
            },
            new Post
            {
                Title = "Mastering Clean Architecture in ASP.NET Core 9",
                Slug = "mastering-clean-architecture-aspnet-core-9",
                Summary = "A deep dive into separating concerns, decoupling from frameworks, and building highly testable applications using the MediatR pattern.",
                Content = @"# Mastering Clean Architecture

Clean architecture is all about putting your domain at the center of the universe. The framework, the database, and the UI are just details.

## The Layers
1. **Domain**: Entities, Enums, Exceptions, and Interfaces.
2. **Application**: Use Cases, CQRS Commands/Queries, and DTOs.
3. **Infrastructure**: Database Contexts, External API Clients, and Services.
4. **Web/API**: Controllers, Middlewares, and Views.

By structuring our ASP.NET Core projects this way, we make our business logic independently testable without spinning up a database.

```csharp
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
        return Guid.NewGuid();
    }
}
```",
                Status = PostStatus.Published,
                FeaturedImageUrl = "/images/header2.jpg",
                PublishedDate = DateTimeOffset.UtcNow.AddDays(-8),
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-9),
                ReadingTimeMinutes = 4,
                IsFeatured = false
            },
            new Post
            {
                Title = "Introduction to AI-Native Application Development",
                Slug = "intro-ai-native-app-development",
                Summary = "How to integrate Large Language Models (LLMs) into your existing enterprise software to create intelligent, context-aware features.",
                Content = @"# Building AI-Native Apps

Artificial Intelligence is no longer just for data scientists. With the advent of accessible APIs from OpenAI, Anthropic, and Google, every software engineer is now an AI engineer.

## Prompt Engineering in Code
Instead of writing complex algorithms to parse natural language, we now construct prompts dynamically.

## Semantic Search and RAG
Retrieval-Augmented Generation (RAG) allows us to ground language models in our private data. By converting text to vector embeddings and storing them in a vector database like Pinecone or pgvector, we can drastically reduce hallucinations.",
                Status = PostStatus.Published,
                FeaturedImageUrl = "/images/header3.jpg",
                PublishedDate = DateTimeOffset.UtcNow.AddDays(-5),
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-6),
                ReadingTimeMinutes = 6,
                IsFeatured = true
            },
            new Post
            {
                Title = "Building Resilient Distributed Systems",
                Slug = "building-resilient-distributed-systems",
                Summary = "Understanding circuit breakers, retries, fallbacks, and the bulkhead pattern to ensure your microservices survive catastrophic failures.",
                Content = @"# Designing for Failure

In a distributed system, failure is not an anomaly—it's a certainty. Network partitions happen, servers crash, and third-party APIs experience downtime.

## The Circuit Breaker Pattern
When a downstream service is struggling, continually sending it traffic will only make things worse. A circuit breaker detects failures and ""trips"", returning an immediate error (or fallback) instead of waiting for a timeout.

We can use libraries like **Polly** in .NET to handle this seamlessly.

```csharp
var policy = Policy.Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );
```",
                Status = PostStatus.Published,
                FeaturedImageUrl = "/images/header4.jpg",
                PublishedDate = DateTimeOffset.UtcNow.AddDays(-2),
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-3),
                ReadingTimeMinutes = 4,
                IsFeatured = false
            },
            new Post
            {
                Title = "The Journey to Engineering Leadership",
                Slug = "journey-to-engineering-leadership",
                Summary = "Transitioning from a Senior Software Engineer to an Engineering Manager or Tech Lead requires a profound mindset shift.",
                Content = @"# From Code to People

The skills that got you to Senior Engineer are rarely the same skills that make a great Engineering Leader.

## Amplifying Others
As a developer, your output is measured by the code you write. As a leader, your output is measured by the success and productivity of your team. You transition from a maker to a multiplier.

## Strategic Thinking
Leaders must balance technical debt against product velocity. You have to learn how to communicate technical risk to non-technical stakeholders effectively.

1. Listen more than you speak.
2. Delegate completely.
3. Protect your team's focus time.",
                Status = PostStatus.Published,
                FeaturedImageUrl = "/images/header5.jpg",
                PublishedDate = DateTimeOffset.UtcNow,
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-1),
                ReadingTimeMinutes = 3,
                IsFeatured = false
            }
        };

        var csharpTag = new Tag { Name = "C#", Slug = "csharp" };
        var netTag = new Tag { Name = ".NET", Slug = "dotnet" };
        var archTag = new Tag { Name = "Architecture", Slug = "architecture" };
        var aiTag = new Tag { Name = "AI", Slug = "ai" };
        var leadershipTag = new Tag { Name = "Leadership", Slug = "leadership" };

        var tags = new List<Tag> { csharpTag, netTag, archTag, aiTag, leadershipTag };
        await context.Tags.AddRangeAsync(tags);

        // Assign tags to posts using object references instead of IDs
        posts[0].PostTags.Add(new PostTag { Tag = archTag });
        posts[0].PostTags.Add(new PostTag { Tag = netTag });

        posts[1].PostTags.Add(new PostTag { Tag = csharpTag });
        posts[1].PostTags.Add(new PostTag { Tag = archTag });

        posts[2].PostTags.Add(new PostTag { Tag = aiTag });
        posts[2].PostTags.Add(new PostTag { Tag = archTag });

        posts[3].PostTags.Add(new PostTag { Tag = archTag });

        posts[4].PostTags.Add(new PostTag { Tag = leadershipTag });

        author.Posts = posts;
        await context.Authors.AddAsync(author);
        await context.SaveChangesAsync();
    }
}
