using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Infrastructure.Services;

public class CloudinaryPhotoStorageService : IPhotoStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudinaryPhotoStorageService> _logger;

    public CloudinaryPhotoStorageService(IConfiguration configuration, ILogger<CloudinaryPhotoStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var cloudName = _configuration["Cloudinary:CloudName"];
        var publicId = ExtractPublicId(url);
        if (string.IsNullOrWhiteSpace(cloudName) || publicId is null) return;

        try
        {
            var account = new Account(cloudName, _configuration["Cloudinary:ApiKey"], _configuration["Cloudinary:ApiSecret"]);
            var result = await new Cloudinary(account).DestroyAsync(new DeletionParams(publicId));
            if (result.Error is not null)
                _logger.LogWarning("Cloudinary não apagou {PublicId}: {Error}", publicId, result.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao apagar {PublicId} no Cloudinary", publicId);
        }
    }

    // https://res.cloudinary.com/<cloud>/image/upload/v123/renovo-workshop/service-orders/abc.jpg
    //   -> renovo-workshop/service-orders/abc  (só arquivos da nossa pasta; nada fora dela é apagado)
    public static string? ExtractPublicId(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)) return null;
        var marker = url.IndexOf("/upload/", StringComparison.Ordinal);
        if (marker < 0) return null;

        var path = url[(marker + "/upload/".Length)..];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && segments[0].Length > 1 && segments[0][0] == 'v' && segments[0][1..].All(char.IsDigit))
            segments.RemoveAt(0);

        var publicPath = string.Join('/', segments);
        var dot = publicPath.LastIndexOf('.');
        if (dot > publicPath.LastIndexOf('/')) publicPath = publicPath[..dot];

        return publicPath.StartsWith("renovo-workshop/", StringComparison.Ordinal) ? publicPath : null;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        // Construído sob demanda (não no construtor): esta classe é injetada em
        // controllers que lidam com muito mais do que fotos, então uma conta
        // Cloudinary ainda não configurada não pode derrubar todo o controller.
        var cloudName = _configuration["Cloudinary:CloudName"];
        if (string.IsNullOrWhiteSpace(cloudName))
            throw new InvalidOperationException("Cloudinary não está configurado (defina Cloudinary:CloudName/ApiKey/ApiSecret).");

        var account = new Account(cloudName, _configuration["Cloudinary:ApiKey"], _configuration["Cloudinary:ApiSecret"]);
        var cloudinary = new Cloudinary(account);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = "renovo-workshop/service-orders"
        };

        var result = await cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
            throw new InvalidOperationException($"Falha ao enviar imagem para o Cloudinary: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }
}
