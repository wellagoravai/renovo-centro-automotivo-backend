using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Api.Helpers;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

// Histórico de orçamentos de guincho (tela "Orçamento" do app e "Orçamentos" do
// painel): o app salva o orçamento ao gerar o PDF, e marca como aprovado quando
// abre a OS de guincho a partir dele.
[ApiController]
[Route("api/tow-quotes")]
[Authorize(Policy = "CanReadOrders")]
public class TowQuotesController : ControllerBase
{
    private const long MaxPdfSizeBytes = 10 * 1024 * 1024;
    private const int MaxFormDataLength = 200_000;

    private readonly RenovoWorkshopDbContext _context;

    public TowQuotesController(RenovoWorkshopDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search)
    {
        var query = _context.TowQuotes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(q => q.Status == status);

        // Projeção direta: a listagem nunca carrega o PDF nem o formulário completo.
        var quotes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Take(500)
            .Select(ToSummary)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            quotes = quotes.Where(q =>
                SearchNormalizer.Contains(q.Number, term) ||
                SearchNormalizer.Contains(q.CustomerName, term) ||
                SearchNormalizer.Contains(q.VehiclePlate, term) ||
                SearchNormalizer.Contains(q.RouteSummary, term)).ToList();
        }

        return Ok(quotes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var summary = await _context.TowQuotes.AsNoTracking().Where(q => q.Id == id).Select(ToSummary).FirstOrDefaultAsync();
        if (summary is null) return NotFound();

        var formData = await _context.TowQuotes.Where(q => q.Id == id).Select(q => q.FormData).FirstAsync();
        return Ok(WithFormData(summary, formData));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Create([FromBody] SaveTowQuoteRequest request)
    {
        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        var now = DateTime.UtcNow;
        var quote = new TowQuote
        {
            Id = Guid.NewGuid(),
            Number = $"ORC-{now:yyyyMMddHHmmss}",
            Status = TowQuoteStatuses.Pendente,
            CreatedAt = now,
            CreatedBy = User.Identity?.Name ?? "Desconhecido",
        };
        Apply(quote, request, now);

        _context.TowQuotes.Add(quote);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = quote.Id }, ToDto(quote));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveTowQuoteRequest request)
    {
        var quote = await _context.TowQuotes.Include(q => q.ServiceOrder).FirstOrDefaultAsync(q => q.Id == id);
        if (quote is null) return NotFound();
        // Aprovado vira OS: a partir daí o que vale é a OS, o orçamento fica congelado.
        if (quote.Status == TowQuoteStatuses.Aprovado)
            return Conflict(new { message = "Orçamento já aprovado não pode ser alterado." });

        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        Apply(quote, request, DateTime.UtcNow);
        // Editar um orçamento recusado é renegociar: volta a ficar pendente.
        if (quote.Status == TowQuoteStatuses.Recusado)
        {
            quote.Status = TowQuoteStatuses.Pendente;
            quote.DecidedAt = null;
            quote.DecidedBy = null;
        }

        await _context.SaveChangesAsync();
        return Ok(ToDto(quote));
    }

    // PDF gerado no app (mesmo arquivo enviado ao cliente), guardado para
    // download posterior pelo painel e pelo próprio app.
    [HttpPut("{id:guid}/pdf")]
    [Authorize(Policy = "CanManageOrders")]
    [RequestSizeLimit(MaxPdfSizeBytes + 64 * 1024)]
    public async Task<IActionResult> UploadPdf(Guid id, IFormFile? file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Arquivo PDF não enviado." });
        if (file.Length > MaxPdfSizeBytes) return BadRequest(new { message = "Arquivo excede o limite de 10MB." });

        var quote = await _context.TowQuotes.FindAsync(id);
        if (quote is null) return NotFound();

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, HttpContext.RequestAborted);
        var bytes = buffer.ToArray();
        if (bytes.Length < 5 || bytes[0] != '%' || bytes[1] != 'P' || bytes[2] != 'D' || bytes[3] != 'F')
            return BadRequest(new { message = "O arquivo enviado não é um PDF." });

        quote.PdfContent = bytes;
        quote.PdfGeneratedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var file = await _context.TowQuotes.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new { q.Number, q.PdfContent })
            .FirstOrDefaultAsync();
        if (file is null) return NotFound();
        if (file.PdfContent is null) return NotFound(new { message = "Este orçamento ainda não tem PDF salvo." });

        return File(file.PdfContent, "application/pdf", $"Orcamento-{file.Number}.pdf");
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveTowQuoteRequest request)
    {
        var quote = await _context.TowQuotes.FindAsync(id);
        if (quote is null) return NotFound();

        var order = await _context.ServiceOrders.FirstOrDefaultAsync(o => o.Id == request.ServiceOrderId);
        if (order is null) return BadRequest(new { message = "OS vinculada não encontrada." });

        // Repetir a aprovação com a mesma OS (retry do app) não é erro.
        if (quote.Status == TowQuoteStatuses.Aprovado && quote.ServiceOrderId != order.Id)
            return Conflict(new { message = "Orçamento já aprovado e ligado a outra OS." });

        if (quote.Status != TowQuoteStatuses.Aprovado)
        {
            quote.Status = TowQuoteStatuses.Aprovado;
            quote.DecidedAt = DateTime.UtcNow;
            quote.DecidedBy = User.Identity?.Name ?? "Desconhecido";
            quote.ServiceOrderId = order.Id;
            quote.ServiceOrder = order;
            await _context.SaveChangesAsync();
        }

        quote.ServiceOrder = order;
        return Ok(ToDto(quote));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var quote = await _context.TowQuotes.FindAsync(id);
        if (quote is null) return NotFound();
        if (quote.Status == TowQuoteStatuses.Aprovado)
            return Conflict(new { message = "Orçamento já aprovado não pode ser recusado." });

        if (quote.Status != TowQuoteStatuses.Recusado)
        {
            quote.Status = TowQuoteStatuses.Recusado;
            quote.DecidedAt = DateTime.UtcNow;
            quote.DecidedBy = User.Identity?.Name ?? "Desconhecido";
            await _context.SaveChangesAsync();
        }

        return Ok(ToDto(quote));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var quote = await _context.TowQuotes.FindAsync(id);
        if (quote is null) return NotFound();
        if (quote.Status == TowQuoteStatuses.Aprovado)
            return Conflict(new { message = "Orçamento aprovado faz parte do histórico da OS e não pode ser excluído." });

        _context.TowQuotes.Remove(quote);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string? Validate(SaveTowQuoteRequest request)
    {
        if (request.Total < 0) return "Total do orçamento inválido.";
        if (request.TotalKm is < 0) return "KM total inválido.";
        if (string.IsNullOrWhiteSpace(request.FormData) || request.FormData.Length > MaxFormDataLength)
            return "Dados do orçamento inválidos.";

        try
        {
            using var doc = JsonDocument.Parse(request.FormData);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return "Dados do orçamento inválidos.";
        }
        catch (JsonException)
        {
            return "Dados do orçamento inválidos.";
        }

        return null;
    }

    private static string Clip(string? value, int max)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static void Apply(TowQuote quote, SaveTowQuoteRequest request, DateTime now)
    {
        quote.CustomerName = Clip(request.CustomerName, 200);
        quote.CustomerPhone = Clip(request.CustomerPhone, 50);
        quote.VehiclePlate = Clip(request.VehiclePlate, 20);
        quote.VehicleDescription = Clip(request.VehicleDescription, 200);
        quote.RouteSummary = Clip(request.RouteSummary, 1000);
        quote.TotalKm = request.TotalKm.HasValue ? Math.Round(request.TotalKm.Value, 1) : null;
        quote.Total = Math.Round(request.Total, 2);
        quote.FormData = request.FormData;
        quote.UpdatedAt = now;
    }

    private static readonly Expression<Func<TowQuote, TowQuoteSummaryDto>> ToSummary = q => new TowQuoteSummaryDto
    {
        Id = q.Id,
        Number = q.Number,
        Status = q.Status,
        CustomerName = q.CustomerName,
        CustomerPhone = q.CustomerPhone,
        VehiclePlate = q.VehiclePlate,
        VehicleDescription = q.VehicleDescription,
        RouteSummary = q.RouteSummary,
        TotalKm = q.TotalKm,
        Total = q.Total,
        HasPdf = q.PdfContent != null,
        CreatedAt = q.CreatedAt,
        CreatedBy = q.CreatedBy,
        UpdatedAt = q.UpdatedAt,
        DecidedAt = q.DecidedAt,
        DecidedBy = q.DecidedBy,
        ServiceOrderId = q.ServiceOrderId,
        ServiceOrderNumber = q.ServiceOrder != null ? q.ServiceOrder.Number : null,
    };

    private static readonly Func<TowQuote, TowQuoteSummaryDto> ToSummaryCompiled = ToSummary.Compile();

    private static TowQuoteDto WithFormData(TowQuoteSummaryDto s, string formData) => new()
    {
        Id = s.Id,
        Number = s.Number,
        Status = s.Status,
        CustomerName = s.CustomerName,
        CustomerPhone = s.CustomerPhone,
        VehiclePlate = s.VehiclePlate,
        VehicleDescription = s.VehicleDescription,
        RouteSummary = s.RouteSummary,
        TotalKm = s.TotalKm,
        Total = s.Total,
        HasPdf = s.HasPdf,
        CreatedAt = s.CreatedAt,
        CreatedBy = s.CreatedBy,
        UpdatedAt = s.UpdatedAt,
        DecidedAt = s.DecidedAt,
        DecidedBy = s.DecidedBy,
        ServiceOrderId = s.ServiceOrderId,
        ServiceOrderNumber = s.ServiceOrderNumber,
        FormData = formData,
    };

    private static TowQuoteDto ToDto(TowQuote quote) => WithFormData(ToSummaryCompiled(quote), quote.FormData);
}
