using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Api.Helpers;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

// Menu "Orçamentos": monta o orçamento de uma OS de oficina (peças, mão de obra,
// desconto e fotos), imprime o PDF e envia ao cliente pelo WhatsApp. Não é uma
// entidade à parte — o orçamento é a própria OS, então o que se edita aqui aparece
// também na tela da OS (e vice-versa).
[ApiController]
[Route("api/quotes")]
[Authorize]
public class QuotesController : ControllerBase
{
    private static readonly string[] ClosedStatuses = { "Entregue", "Cancelado" };

    private readonly RenovoWorkshopDbContext _context;
    private readonly IQuoteDocumentService _quoteDocumentService;
    private readonly IQuoteImageLoader _quoteImageLoader;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IServiceOrderStatusService _statusService;

    public QuotesController(RenovoWorkshopDbContext context, IQuoteDocumentService quoteDocumentService, IQuoteImageLoader quoteImageLoader, IWhatsAppService whatsAppService, IServiceOrderStatusService statusService)
    {
        _context = context;
        _quoteDocumentService = quoteDocumentService;
        _quoteImageLoader = quoteImageLoader;
        _whatsAppService = whatsAppService;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeClosed = false)
    {
        var query = _context.ServiceOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.Items)
            .Where(o => o.ServiceType == ServiceOrderTypes.Oficina);

        if (!includeClosed)
            query = query.Where(o => !ClosedStatuses.Contains(o.Status));

        var orders = await query.OrderByDescending(o => o.EntryDate).ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            orders = orders.Where(o =>
                SearchNormalizer.Contains(o.Number, term) ||
                SearchNormalizer.Contains(o.Customer.Name, term) ||
                SearchNormalizer.Contains(o.Vehicle.Plate, term) ||
                SearchNormalizer.Contains(o.Vehicle.Model, term)).ToList();
        }

        return Ok(orders.Select(o => FillSummary(new QuoteSummaryDto(), o)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null) return NotFound();
        return Ok(await BuildDetailAsync(order));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Save(Guid id, [FromBody] SaveQuoteDto request)
    {
        var order = await LoadOrderAsync(id);
        if (order is null) return NotFound();

        if (request.LaborValue < 0)
            return BadRequest(new { message = "Valor da mão de obra não pode ser negativo." });
        if (request.Discount < 0)
            return BadRequest(new { message = "Desconto não pode ser negativo." });

        if (request.Items is not null)
        {
            if (order.StockDeducted)
                return BadRequest(new { message = "As peças desta OS já foram baixadas do estoque e não podem mais ser alteradas." });

            var error = await ApplyItemsAsync(order, request.Items);
            if (error is not null) return BadRequest(new { message = error });
        }

        var photos = await _context.ServiceOrderPhotos.Where(p => p.ServiceOrderId == id).ToListAsync();
        var photoIds = photos.Select(p => p.Id).ToHashSet();
        if (request.VehiclePhotoId.HasValue && !photoIds.Contains(request.VehiclePhotoId.Value))
            return BadRequest(new { message = "Foto do veículo não pertence a esta OS." });
        if (request.PartPhotoIds.Any(p => !photoIds.Contains(p)))
            return BadRequest(new { message = "Foto de peça não pertence a esta OS." });

        if (request.Diagnosis is not null) order.Diagnosis = request.Diagnosis.Trim();
        if (request.Services is not null) order.Services = request.Services.Trim();
        order.LaborValue = request.LaborValue;
        order.QuoteVehiclePhotoId = request.VehiclePhotoId;
        foreach (var photo in photos)
            photo.IncludeInQuote = request.PartPhotoIds.Contains(photo.Id) && photo.Id != request.VehiclePhotoId;

        var subtotal = order.LaborValue + order.PartsTotal;
        if (request.Discount > subtotal)
            return BadRequest(new { message = "Desconto não pode ser maior que o valor do orçamento." });
        order.Discount = request.Discount;
        order.RecalculateValue();

        await _context.SaveChangesAsync();

        return Ok(await BuildDetailAsync(order));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null) return NotFound();

        var images = await _quoteImageLoader.LoadAsync(order, HttpContext.RequestAborted);
        var pdfBytes = _quoteDocumentService.GenerateQuotePdf(order, order.Customer, images);
        return File(pdfBytes, "application/pdf", $"Orcamento-{order.Number}.pdf");
    }

    // Manda o PDF (com o resumo do orçamento como legenda) para o WhatsApp cadastrado
    // do cliente. Se a OS ainda não estava esperando aprovação, passa para "Aguardando
    // aprovação" — é nesse status que a resposta SIM/NÃO do cliente é reconhecida.
    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Send(Guid id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null) return NotFound();

        var phone = !string.IsNullOrWhiteSpace(order.Customer.WhatsApp) ? order.Customer.WhatsApp : order.Customer.Phone;
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Cliente sem WhatsApp/telefone cadastrado. Cadastre o número ou use \"Imprimir PDF\"." });

        if (order.Items.Count == 0 && order.LaborValue <= 0)
            return BadRequest(new { message = "Orçamento vazio: lance as peças ou a mão de obra antes de enviar." });

        var caption = _whatsAppService.BuildStatusMessage(order, order.Customer, order.Status, ServiceOrderStatuses.AguardandoAprovacao);
        var result = await _whatsAppService.SendQuoteDocumentAsync(order, order.Customer, HttpContext.RequestAborted, caption);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new SendQuoteResultDto
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(result.Error) ? result.Message : $"{result.Message} ({result.Error})",
                Phone = phone,
                Status = order.Status,
                QuoteSentAt = order.QuoteSentAt
            });
        }

        order.QuoteSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (IsBeforeApproval(order.Status))
        {
            var changedBy = User.Identity?.Name ?? "Sistema";
            await _statusService.ChangeStatusAsync(order.Id, ServiceOrderStatuses.AguardandoAprovacao,
                "Orçamento enviado ao cliente pelo WhatsApp", changedBy, HttpContext.RequestAborted, notifyCustomer: false);
        }

        var status = await _context.ServiceOrders.Where(o => o.Id == id).Select(o => o.Status).FirstAsync();
        return Ok(new SendQuoteResultDto
        {
            Success = true,
            Message = result.Message,
            Phone = phone,
            Status = status,
            QuoteSentAt = order.QuoteSentAt
        });
    }

    private static bool IsBeforeApproval(string status)
    {
        var statuses = ServiceOrderStatuses.Oficina.ToList();
        var index = statuses.IndexOf(status);
        return index >= 0 && index < statuses.IndexOf(ServiceOrderStatuses.AguardandoAprovacao);
    }

    private Task<ServiceOrder?> LoadOrderAsync(Guid id) =>
        _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id && o.ServiceType == ServiceOrderTypes.Oficina);

    // Sincroniza os itens da OS com a lista enviada: atualiza os existentes, cria os
    // novos e remove os que sumiram. Retorna mensagem de erro de validação, se houver.
    private async Task<string?> ApplyItemsAsync(ServiceOrder order, List<SaveQuoteItemDto> items)
    {
        foreach (var item in items)
        {
            if (item.Quantity <= 0) return "Quantidade deve ser maior que zero.";
            if (item.UnitValue < 0) return "Valor da peça não pode ser negativo.";
            if (item.InventoryItemId is null && string.IsNullOrWhiteSpace(item.Description))
                return "Informe a descrição da peça avulsa.";
            if (item.Id.HasValue && order.Items.All(i => i.Id != item.Id.Value))
                return "Item não pertence a esta OS.";
        }

        var inventoryIds = items.Where(i => i.InventoryItemId.HasValue).Select(i => i.InventoryItemId!.Value).Distinct().ToList();
        var inventory = await _context.InventoryItems.Where(i => inventoryIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        if (inventory.Count != inventoryIds.Count) return "Item de estoque não encontrado.";

        var keptIds = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var removed in order.Items.Where(i => !keptIds.Contains(i.Id)).ToList())
        {
            order.Items.Remove(removed);
            _context.ServiceOrderItems.Remove(removed);
        }

        foreach (var item in items)
        {
            var entity = item.Id.HasValue ? order.Items.First(i => i.Id == item.Id.Value) : null;
            if (entity is null)
            {
                entity = new ServiceOrderItem { Id = Guid.NewGuid(), ServiceOrderId = order.Id };
                order.Items.Add(entity);
                _context.ServiceOrderItems.Add(entity);
            }

            entity.InventoryItemId = item.InventoryItemId;
            entity.InventoryItem = item.InventoryItemId.HasValue ? inventory[item.InventoryItemId.Value] : null;
            entity.Description = item.InventoryItemId.HasValue ? string.Empty : item.Description!.Trim();
            entity.Quantity = item.Quantity;
            entity.UnitValue = item.UnitValue;
        }

        return null;
    }

    private static T FillSummary<T>(T dto, ServiceOrder order) where T : QuoteSummaryDto
    {
        dto.Id = order.Id;
        dto.Number = order.Number;
        dto.Status = order.Status;
        dto.EntryDate = order.EntryDate;
        dto.CustomerName = order.Customer.Name;
        dto.CustomerWhatsApp = !string.IsNullOrWhiteSpace(order.Customer.WhatsApp) ? order.Customer.WhatsApp : order.Customer.Phone;
        dto.VehiclePlate = order.Vehicle.Plate;
        dto.VehicleDescription = $"{order.Vehicle.Brand} {order.Vehicle.Model}".Trim();
        dto.ItemsCount = order.Items.Count;
        dto.PartsTotal = order.PartsTotal;
        dto.LaborValue = order.LaborValue;
        dto.Discount = order.Discount;
        dto.Total = order.Value;
        dto.QuoteSentAt = order.QuoteSentAt;
        return dto;
    }

    private async Task<QuoteDetailDto> BuildDetailAsync(ServiceOrder order)
    {
        var photos = await _context.ServiceOrderPhotos
            .AsNoTracking()
            .Where(p => p.ServiceOrderId == order.Id)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        var dto = FillSummary(new QuoteDetailDto(), order);
        dto.ProblemReported = order.ProblemReported;
        dto.Diagnosis = order.Diagnosis;
        dto.Services = order.Services;
        dto.CustomerDocument = order.Customer.Document;
        dto.VehicleYear = order.Vehicle.Year;
        dto.VehicleMileage = order.Vehicle.Mileage;
        dto.StockDeducted = order.StockDeducted;
        dto.VehiclePhotoId = order.QuoteVehiclePhotoId;
        dto.Items = order.Items.Select(i => new QuoteItemDto
        {
            Id = i.Id,
            InventoryItemId = i.InventoryItemId,
            Code = i.InventoryItem?.Code ?? string.Empty,
            Description = i.DisplayDescription,
            Quantity = i.Quantity,
            UnitValue = i.UnitValue,
            Total = i.Quantity * i.UnitValue
        }).ToList();
        dto.Photos = photos.Select(p => new QuotePhotoDto
        {
            Id = p.Id,
            Url = p.Url,
            UploadedAt = p.UploadedAt,
            IncludeInQuote = p.IncludeInQuote
        }).ToList();
        return dto;
    }
}
