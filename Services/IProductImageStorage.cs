namespace Laptop.Services;

public interface IProductImageStorage
{
    Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task DeleteRemovedAsync(
        string? previousImages,
        string? currentImages,
        CancellationToken cancellationToken = default);

    Task DeleteAllAsync(string? images, CancellationToken cancellationToken = default);
}
