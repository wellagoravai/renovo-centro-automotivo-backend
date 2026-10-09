using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Infrastructure.Services;

public class QuoteImageLoader : IQuoteImageLoader
{
    // Mais que isso deixa o PDF pesado demais para o WhatsApp.
    public const int MaxPartPhotos = 6;

    private readonly RenovoWorkshopDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuoteImageLoader> _logger;

    public QuoteImageLoader(RenovoWorkshopDbContext context, HttpClient httpClient, ILogger<QuoteImageLoader> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<QuotePdfImages> LoadAsync(ServiceOrder order, CancellationToken cancellationToken = default)
    {
        var photos = await _context.ServiceOrderPhotos
            .Where(p => p.ServiceOrderId == order.Id && (p.Id == order.QuoteVehiclePhotoId || p.IncludeInQuote))
            .OrderBy(p => p.UploadedAt)
            .ToListAsync(cancellationToken);

        if (photos.Count == 0) return QuotePdfImages.None;

        var vehiclePhoto = photos.FirstOrDefault(p => p.Id == order.QuoteVehiclePhotoId);
        var vehicleBytes = vehiclePhoto is null ? null : await DownloadAsync(vehiclePhoto.Url, cancellationToken);

        var partBytes = new List<byte[]>();
        foreach (var photo in photos.Where(p => p.IncludeInQuote && p.Id != order.QuoteVehiclePhotoId).Take(MaxPartPhotos))
        {
            var bytes = await DownloadAsync(photo.Url, cancellationToken);
            if (bytes is not null) partBytes.Add(bytes);
        }

        return new QuotePdfImages(vehicleBytes, partBytes);
    }

    private async Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(ToPdfFriendlyUrl(url), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            _logger.LogWarning(ex, "Não foi possível baixar a foto {Url} para o orçamento.", url);
            return null;
        }
    }

    // Fotos do celular chegam em tamanho cheio (e às vezes em HEIC, que o gerador de
    // PDF não lê). O Cloudinary converte e reduz na própria URL.
    public static string ToPdfFriendlyUrl(string url)
    {
        const string marker = "/image/upload/";
        var index = url.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0 || !url.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return url;

        var insertAt = index + marker.Length;
        return url[..insertAt] + "f_jpg,w_1000,c_limit,q_80/" + url[insertAt..];
    }
}
