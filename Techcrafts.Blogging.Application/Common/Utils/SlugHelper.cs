using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;

namespace Techcrafts.Blogging.Application.Common.Utils;

public static class SlugHelper
{
    public static string GenerateSlug(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return "post-" + Guid.NewGuid().ToString("N")[..8];
        }

        string str = phrase.Trim().ToLowerInvariant();
        // Remove invalid chars
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // Convert multiple spaces into one space
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // Cut and trim
        str = str.Length <= 45 ? str : str[..45].Trim();
        // Hyphens
        str = Regex.Replace(str, @"\s", "-");

        if (string.IsNullOrWhiteSpace(str))
        {
            return "post-" + Guid.NewGuid().ToString("N")[..8];
        }

        return str;
    }

    public static async Task<string> GenerateUniqueSlugAsync(IBloggingDbContext context, string title, Guid? existingPostId = null, CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(title);
        var candidateSlug = baseSlug;
        var counter = 1;

        while (await context.Posts.AnyAsync(p => p.Slug == candidateSlug && (!existingPostId.HasValue || p.Id != existingPostId.Value), cancellationToken))
        {
            candidateSlug = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidateSlug;
    }

    public static int CalculateReadingTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var wordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(wordCount / 200.0);
    }
}
