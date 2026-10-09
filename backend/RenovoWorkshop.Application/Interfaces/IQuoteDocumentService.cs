using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Application.Interfaces;

public interface IQuoteDocumentService
{
    // Gera o PDF do orçamento (peças, serviços, desconto, totais, fotos, aprovação por
    // WhatsApp e rodapé institucional) a partir dos dados já preenchidos na OS — sem
    // depender de nenhum documento fiscal.
    byte[] GenerateQuotePdf(ServiceOrder order, Customer customer, QuotePdfImages? images = null);
}

// Fotos já baixadas que entram no PDF: a do veículo (de preferência a da placa) e as
// das peças a trocar.
public record QuotePdfImages(byte[]? VehiclePhoto, IReadOnlyList<byte[]> PartPhotos)
{
    public static QuotePdfImages None { get; } = new(null, Array.Empty<byte[]>());
}

public interface IQuoteImageLoader
{
    // Baixa as fotos escolhidas na tela Orçamentos (ServiceOrder.QuoteVehiclePhotoId e
    // ServiceOrderPhoto.IncludeInQuote). Melhor esforço: foto que falhar fica de fora.
    Task<QuotePdfImages> LoadAsync(ServiceOrder order, CancellationToken cancellationToken = default);
}
