using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Services;

namespace RenovoWorkshop.Tests;

// O PDF é o que sai automaticamente no WhatsApp quando a OS entra em "Aguardando
// aprovação" e também o que o botão "Gerar Orçamento em PDF" baixa no painel —
// estes testes garantem que a geração não quebra em runtime (fonte/imagem
// embutidas, layout) para os casos reais de uma OS, não só que compila.
public class QuoteDocumentServiceTests
{
    private static ServiceOrder BuildOrder(bool withItems = true, bool withDiagnosis = true)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Maria Silva",
            Document = "123.456.789-00",
            Phone = "(18) 99999-0000",
            WhatsApp = "5518999990000"
        };

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Plate = "ABC1D23",
            Brand = "Volkswagen",
            Model = "Gol 1.6",
            Year = 2019,
            Mileage = 78400,
            CustomerId = customer.Id,
            Customer = customer
        };

        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = "OS-202608070001",
            Status = "Aguardando aprovação",
            Services = "Troca de óleo e filtros",
            Diagnosis = withDiagnosis ? "Óleo com sinais de desgaste e filtro de ar sujo." : string.Empty,
            LaborValue = 80m,
            CustomerId = customer.Id,
            Customer = customer,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
        };

        if (withItems)
        {
            var inventoryItem = new InventoryItem { Id = Guid.NewGuid(), Description = "Óleo de motor sintético" };
            order.Items.Add(new ServiceOrderItem
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = order.Id,
                Quantity = 4,
                UnitValue = 64m,
                InventoryItem = inventoryItem
            });
        }

        order.Value = order.LaborValue + order.Items.Sum(i => i.Quantity * i.UnitValue);

        return order;
    }

    [Fact]
    public void GenerateQuotePdf_ShouldReturnValidPdfBytes_ForFullOrder()
    {
        var service = new QuoteDocumentService();
        var order = BuildOrder();

        var pdfBytes = service.GenerateQuotePdf(order, order.Customer);

        Assert.NotEmpty(pdfBytes);
        // Todo PDF válido começa com essa assinatura ("%PDF-") nos primeiros bytes.
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5));
    }

    [Fact]
    public void GenerateQuotePdf_ShouldNotThrow_WhenOrderHasNoItemsOrDiagnosis()
    {
        var service = new QuoteDocumentService();
        var order = BuildOrder(withItems: false, withDiagnosis: false);

        var pdfBytes = service.GenerateQuotePdf(order, order.Customer);

        Assert.NotEmpty(pdfBytes);
    }

    // Tela Orçamentos: desconto, peça avulsa (sem item de estoque) e fotos do veículo
    // e das peças — inclusive uma foto ilegível, que deve ficar de fora sem derrubar o PDF.
    [Fact]
    public void GenerateQuotePdf_ShouldRender_DiscountLoosePartAndPhotos()
    {
        var service = new QuoteDocumentService();
        var order = BuildOrder();
        order.Items.Add(new ServiceOrderItem { Id = Guid.NewGuid(), ServiceOrderId = order.Id, Description = "Pastilha de freio dianteira", Quantity = 1, UnitValue = 150m });
        order.Discount = 30m;
        order.RecalculateValue();

        using var logoStream = typeof(QuoteDocumentService).Assembly
            .GetManifestResourceStream("RenovoWorkshop.Infrastructure.Resources.renovo-logo.png")!;
        using var memory = new MemoryStream();
        logoStream.CopyTo(memory);
        var photo = memory.ToArray();
        var images = new RenovoWorkshop.Application.Interfaces.QuotePdfImages(photo, new[] { photo, new byte[] { 1, 2, 3 }, photo });

        var pdfBytes = service.GenerateQuotePdf(order, order.Customer, images);

        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5));
        // 80 (mão de obra) + 4 x 64 + 150 (peças) − 30 (desconto)
        Assert.Equal(456m, order.Value);
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1/renovo/foto.heic", "https://res.cloudinary.com/demo/image/upload/f_jpg,w_1000,c_limit,q_80/v1/renovo/foto.heic")]
    [InlineData("https://example.com/image/upload/foto.jpg", "https://example.com/image/upload/foto.jpg")]
    public void ToPdfFriendlyUrl_ConvertsOnlyCloudinaryUrls(string url, string expected)
    {
        Assert.Equal(expected, QuoteImageLoader.ToPdfFriendlyUrl(url));
    }
}
