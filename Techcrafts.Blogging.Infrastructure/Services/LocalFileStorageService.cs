namespace Techcrafts.Blogging.Infrastructure.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);
}

public class LocalFileStorageService : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        // Save to wwwroot/uploads
        return $"/uploads/{fileName}";
    }
}
