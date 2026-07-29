using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Infrastructure.Services;

public class CloudinaryPhotoStorageService : IPhotoStorageService
{
    private readonly IConfiguration _configuration;

    public CloudinaryPhotoStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
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
