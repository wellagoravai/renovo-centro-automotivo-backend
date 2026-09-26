namespace RenovoWorkshop.Application.Interfaces;

public interface IPhotoStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    // Melhor esforço: apaga o arquivo no provedor. Nunca lança — falha aqui não pode
    // impedir a exclusão do registro (o arquivo órfão só ocupa espaço).
    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}
