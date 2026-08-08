using System.Reflection;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Infrastructure.Services;

// Gera o PDF do orçamento no mesmo layout aprovado do modelo (logo, dados do
// cliente/veículo, peças, serviços, totais e versículo no rodapé), pra ser
// anexado no WhatsApp ou baixado manualmente pelo painel.
public class QuoteDocumentService : IQuoteDocumentService
{
    private const string CompanyName = "Renovo Centro Automotivo";
    private const string CompanyAddress = "Rua Marechal Deodoro, 2305 — Andradina/SP, 16901-455";
    private const string CompanyPhone = "(18) 3722-2388";
    private const string AccentColor = "#8F1414";

    private const string VerseText = "“E tudo quanto fizerdes, fazei-o de todo o coração, como ao Senhor, e não aos homens.”";
    private const string VerseReference = "Colossenses 3:23";

    private static readonly byte[] LogoBytes = LoadEmbeddedResource("renovo-logo.png");

    static QuoteDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        using var regular = GetResourceStream("Roboto-Regular.ttf");
        FontManager.RegisterFont(regular);
        using var bold = GetResourceStream("Roboto-Bold.ttf");
        FontManager.RegisterFont(bold);
    }

    private static Stream GetResourceStream(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"RenovoWorkshop.Infrastructure.Resources.{fileName}";
        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso embutido não encontrado: {resourceName}");
    }

    private static byte[] LoadEmbeddedResource(string fileName)
    {
        using var stream = GetResourceStream(fileName);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public byte[] GenerateQuotePdf(ServiceOrder order, Customer customer)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Roboto").FontSize(10));

                page.Header().Element(c => ComposeHeader(c, order));
                page.Content().Element(c => ComposeContent(c, order, customer));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ServiceOrder order)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(56).Image(LogoBytes);

                row.RelativeItem().PaddingLeft(12).Column(col =>
                {
                    col.Item().Text(CompanyName).FontSize(15).Bold();
                    col.Item().Text(CompanyAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(CompanyPhone).FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(160).Column(col =>
                {
                    col.Item().AlignRight().Text("ORÇAMENTO").FontSize(13).Bold().FontColor(AccentColor);
                    col.Item().AlignRight().Text($"Nº {order.Number}").FontSize(9);
                    col.Item().AlignRight().Text($"Data: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                });
            });

            column.Item().PaddingTop(10).BorderBottom(2).BorderColor(Colors.Black);
        });
    }

    private static void ComposeContent(IContainer container, ServiceOrder order, Customer customer)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(14);

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("CLIENTE").FontSize(8).Bold().FontColor(AccentColor);
                    col.Item().PaddingTop(4).Text(customer.Name).Bold();
                    if (!string.IsNullOrWhiteSpace(customer.Document))
                        col.Item().Text($"CPF/CNPJ: {customer.Document}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(customer.Phone))
                        col.Item().Text($"Telefone: {customer.Phone}").FontSize(9);
                });

                row.ConstantItem(12);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("VEÍCULO").FontSize(8).Bold().FontColor(AccentColor);
                    col.Item().PaddingTop(4).Text($"{order.Vehicle.Brand} {order.Vehicle.Model}".Trim()).Bold();
                    col.Item().Text($"Placa: {order.Vehicle.Plate}   Ano: {order.Vehicle.Year}").FontSize(9);
                    if (order.Vehicle.Mileage > 0)
                        col.Item().Text($"KM: {order.Vehicle.Mileage:0}").FontSize(9);
                });
            });

            if (order.Items.Count > 0)
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("PEÇAS E PRODUTOS").FontSize(9).Bold();
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Descrição");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Qtd");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Valor unit.");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                        });

                        foreach (var item in order.Items)
                        {
                            var description = item.InventoryItem?.Description ?? "Item";
                            var total = item.Quantity * item.UnitValue;
                            table.Cell().Element(BodyCell).Text(description);
                            table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(item.UnitValue));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(total));
                        }
                    });
                });
            }

            if (!string.IsNullOrWhiteSpace(order.Services) || order.LaborValue > 0)
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("SERVIÇOS").FontSize(9).Bold();
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(string.IsNullOrWhiteSpace(order.Services) ? "Mão de obra" : order.Services);
                        row.ConstantItem(90).AlignRight().Text(FormatCurrency(order.LaborValue));
                    });
                });
            }

            if (!string.IsNullOrWhiteSpace(order.Diagnosis))
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("DIAGNÓSTICO").FontSize(9).Bold();
                    col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(8).Text(order.Diagnosis).FontSize(9);
                });
            }

            var totalPecas = order.Items.Sum(i => i.Quantity * i.UnitValue);
            column.Item().AlignRight().Width(220).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total de peças").FontSize(9);
                    r.ConstantItem(90).AlignRight().Text($"R$ {FormatCurrency(totalPecas)}").FontSize(9);
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total de serviços").FontSize(9);
                    r.ConstantItem(90).AlignRight().Text($"R$ {FormatCurrency(order.LaborValue)}").FontSize(9);
                });
                col.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Black).PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                    r.ConstantItem(90).AlignRight().Text($"R$ {FormatCurrency(order.Value)}").Bold().FontSize(12).FontColor(AccentColor);
                });
            });

            column.Item().Background("#DCEFE4").Padding(10).Text(text =>
            {
                text.Span("Aprovação pelo WhatsApp: ").Bold().FontSize(9.5f);
                text.Span("responda SIM para aprovar este orçamento ou NÃO para recusar. Também é possível assinar e devolver este documento.").FontSize(9.5f);
            });

            column.Item().PaddingTop(24).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(4)
                        .Text("Assinatura do cliente").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(30);
                row.RelativeItem().Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(4)
                        .Text("Responsável técnico — Renovo Centro Automotivo").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(x => x.FontSize(8).Bold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(5);

    private static void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(16).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10).AlignCenter().Text(VerseText).Italic().FontSize(9);
            column.Item().AlignCenter().Text(VerseReference).FontSize(8).Bold().FontColor(AccentColor);
        });
    }

    // ':N2' depende de dados de cultura que o runtime não carrega com
    // InvariantGlobalization=true (mesma situação de WhatsAppService.FormatCurrency).
    private static string FormatCurrency(decimal value) =>
        value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
}
