namespace RenovoWorkshop.Application.Interfaces;

public interface IPhotoStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
