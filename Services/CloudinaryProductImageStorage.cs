using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Laptop.Services;

public sealed class CloudinaryProductImageStorage : IProductImageStorage
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const string ProductFolder = "jamtarvishop/products";
    private const string DefaultCloudName = "yy8fsocw";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudinaryProductImageStorage> _logger;

    public CloudinaryProductImageStorage(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CloudinaryProductImageStorage> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
        {
            throw new InvalidOperationException("Each product image must be smaller than 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Upload product images in JPG, PNG, or WebP format.");
        }

        var (cloudName, apiKey, apiSecret) = GetCredentials();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var publicId = $"{ProductFolder}/{Guid.NewGuid():N}";
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        using var content = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetImageContentType(extension));
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));

        foreach (var parameter in parameters)
        {
            content.Add(new StringContent(parameter.Value), parameter.Key);
        }

        content.Add(new StringContent(apiKey), "api_key");
        content.Add(new StringContent(CreateSignature(parameters, apiSecret)), "signature");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/image/upload",
                content,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Could not reach Cloudinary during an image upload.");
            throw new InvalidOperationException("Cloudinary could not be reached. Check the connection and try again.", exception);
        }

        using (response)
        {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Cloudinary upload failed with status {StatusCode}.", (int)response.StatusCode);
            throw new InvalidOperationException("Cloudinary could not upload the image. Check the Cloudinary settings and try again.");
        }

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (!document.RootElement.TryGetProperty("secure_url", out var secureUrl)
                    || string.IsNullOrWhiteSpace(secureUrl.GetString()))
                {
                    throw new InvalidOperationException("Cloudinary did not return an image URL.");
                }

                return secureUrl.GetString()!;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Cloudinary returned an unreadable upload response.");
                throw new InvalidOperationException("Cloudinary returned an unexpected response.", exception);
            }
        }
    }

    public async Task DeleteRemovedAsync(
        string? previousImages,
        string? currentImages,
        CancellationToken cancellationToken = default)
    {
        var previousIds = GetCloudinaryPublicIds(previousImages);
        var currentIds = GetCloudinaryPublicIds(currentImages).ToHashSet(StringComparer.Ordinal);

        foreach (var publicId in previousIds.Where(id => !currentIds.Contains(id)))
        {
            await TryDeleteAsync(publicId, cancellationToken);
        }
    }

    public async Task DeleteAllAsync(string? images, CancellationToken cancellationToken = default)
    {
        foreach (var publicId in GetCloudinaryPublicIds(images))
        {
            await TryDeleteAsync(publicId, cancellationToken);
        }
    }

    private async Task TryDeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        try
        {
            var (cloudName, apiKey, apiSecret) = GetCredentials();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["public_id"] = publicId,
                ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
            };

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["public_id"] = publicId,
                ["timestamp"] = parameters["timestamp"],
                ["api_key"] = apiKey,
                ["signature"] = CreateSignature(parameters, apiSecret)
            });

            using var response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/image/destroy",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not delete Cloudinary image {PublicId}; status {StatusCode}.",
                    publicId, (int)response.StatusCode);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Product data is already saved. Failed remote cleanup can be handled in the
            // Cloudinary Media Library without undoing the product change.
            _logger.LogWarning(exception, "Could not delete Cloudinary image {PublicId}.", publicId);
        }
    }

    private (string CloudName, string ApiKey, string ApiSecret) GetCredentials()
    {
        var cloudName = _configuration["Cloudinary:CloudName"] ?? DefaultCloudName;
        var apiKey = _configuration["Cloudinary:ApiKey"];
        var apiSecret = _configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary is not configured. Set Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret in the app environment.");
        }

        return (cloudName, apiKey, apiSecret);
    }

    private static string CreateSignature(IEnumerable<KeyValuePair<string, string>> parameters, string apiSecret)
    {
        var parameterString = string.Join("&", parameters
            .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
            .Select(parameter => $"{parameter.Key}={parameter.Value}"));
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(parameterString + apiSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static IEnumerable<string> GetCloudinaryPublicIds(string? images)
    {
        return (images ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TryGetCloudinaryPublicId)
            .Where(publicId => publicId != null)
            .Select(publicId => publicId!)
            .Distinct(StringComparer.Ordinal);
    }

    private static string? TryGetCloudinaryPublicId(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        var uploadIndex = Array.FindIndex(segments, segment => segment.Equals("upload", StringComparison.OrdinalIgnoreCase));
        if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
        {
            return null;
        }

        var publicIdSegments = segments.Skip(uploadIndex + 1).ToList();
        if (publicIdSegments[0].Length > 1
            && publicIdSegments[0][0] == 'v'
            && publicIdSegments[0][1..].All(char.IsDigit))
        {
            publicIdSegments.RemoveAt(0);
        }

        if (publicIdSegments.Count == 0)
        {
            return null;
        }

        var finalSegment = publicIdSegments[^1];
        var extensionIndex = finalSegment.LastIndexOf('.');
        if (extensionIndex > 0)
        {
            publicIdSegments[^1] = finalSegment[..extensionIndex];
        }

        var publicId = string.Join('/', publicIdSegments);
        return publicId.StartsWith(ProductFolder + "/", StringComparison.Ordinal)
            ? publicId
            : null;
    }

    private static string GetImageContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
