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

// Pedidos (rascunhos de OS de oficina) feitos pelo app no primeiro contato com o cliente.
// Aprovado, vira OS por POST /api/service-orders/from-request/{id}.
[ApiController]
[Route("api/service-requests")]
[Authorize(Policy = "CanReadOrders")]
public class ServiceRequestsController : ControllerBase
{
    private const long MaxPhotoSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic"
    };

    private readonly RenovoWorkshopDbContext _context;
    private readonly IPhotoStorageService _photoStorageService;

    public ServiceRequestsController(RenovoWorkshopDbContext context, IPhotoStorageService photoStorageService)
    {
        _context = context;
        _photoStorageService = photoStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search)
    {
        var query = _context.ServiceRequests.AsNoTracking()
            .Include(r => r.ServiceOrder)
            .Include(r => r.Photos)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);

        var requests = await query.OrderByDescending(r => r.CreatedAt).Take(500).ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            requests = requests.Where(r =>
                SearchNormalizer.Contains(r.Number, term) ||
                SearchNormalizer.Contains(r.CustomerName, term) ||
                SearchNormalizer.Contains(r.VehiclePlate, term)).ToList();
        }

        // Listagem sem a lista de fotos, só a contagem.
        return Ok(requests.Select(r => ToDto(r, includePhotos: false)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = await LoadAsync(id, tracking: false);
        if (request is null) return NotFound();
        return Ok(ToDto(request));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Create([FromBody] SaveServiceRequestRequest body)
    {
        var error = Validate(body);
        if (error is not null) return BadRequest(new { message = error });

        var now = DateTime.UtcNow;
        var request = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Number = $"PED-{now:yyyyMMddHHmmss}",
            Status = ServiceRequestStatuses.Pendente,
            CreatedAt = now,
            CreatedBy = User.Identity?.Name ?? "Desconhecido",
        };
        Apply(request, body, now);

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = request.Id }, ToDto(request));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveServiceRequestRequest body)
    {
        var request = await LoadAsync(id, tracking: true);
        if (request is null) return NotFound();
        if (request.Status == ServiceRequestStatuses.OsCriada)
            return Conflict(new { message = "Este pedido já virou OS. Edite os dados na própria OS." });

        var error = Validate(body);
        if (error is not null) return BadRequest(new { message = error });

        Apply(request, body, DateTime.UtcNow);
        await _context.SaveChangesAsync();
        return Ok(ToDto(request));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "CanManageOrders")]
    public Task<IActionResult> Approve(Guid id) => DecideAsync(id, ServiceRequestStatuses.Aprovado);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "CanManageOrders")]
    public Task<IActionResult> Reject(Guid id) => DecideAsync(id, ServiceRequestStatuses.Recusado);

    // Desfaz a decisão (aprovado/recusado por engano).
    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "CanManageOrders")]
    public Task<IActionResult> Reopen(Guid id) => DecideAsync(id, ServiceRequestStatuses.Pendente);

    private async Task<IActionResult> DecideAsync(Guid id, string status)
    {
        var request = await LoadAsync(id, tracking: true);
        if (request is null) return NotFound();
        if (request.Status == ServiceRequestStatuses.OsCriada)
            return Conflict(new { message = "Este pedido já virou OS." });

        if (request.Status != status)
        {
            request.Status = status;
            var pending = status == ServiceRequestStatuses.Pendente;
            request.DecidedAt = pending ? null : DateTime.UtcNow;
            request.DecidedBy = pending ? null : User.Identity?.Name ?? "Desconhecido";
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(ToDto(request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = await LoadAsync(id, tracking: true);
        if (request is null) return NotFound();
        if (request.Status == ServiceRequestStatuses.OsCriada)
            return Conflict(new { message = "Pedido que já virou OS faz parte do histórico e não pode ser excluído." });

        var urls = request.Photos.Select(p => p.Url).ToList();
        _context.ServiceRequests.Remove(request);
        await _context.SaveChangesAsync();
        foreach (var url in urls)
            await _photoStorageService.DeleteAsync(url, HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize(Policy = "CanManageOrders")]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (request.Status == ServiceRequestStatuses.OsCriada)
            return Conflict(new { message = "Este pedido já virou OS. Envie as fotos na própria OS." });

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado." });
        if (file.Length > MaxPhotoSizeBytes)
            return BadRequest(new { message = "Arquivo excede o limite de 10MB." });
        if (!AllowedPhotoContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Formato de imagem não suportado." });

        string url;
        try
        {
            await using var stream = file.OpenReadStream();
            url = await _photoStorageService.UploadAsync(stream, file.FileName, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        var photo = new ServiceRequestPhoto
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = id,
            Url = url,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = User.Identity?.Name ?? "Desconhecido",
        };
        _context.ServiceRequestPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return Ok(ToPhotoDto(photo));
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId)
    {
        var photo = await _context.ServiceRequestPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.ServiceRequestId == id);
        if (photo is null) return NotFound();

        _context.ServiceRequestPhotos.Remove(photo);
        await _context.SaveChangesAsync();
        await _photoStorageService.DeleteAsync(photo.Url, HttpContext.RequestAborted);

        return NoContent();
    }

    private Task<ServiceRequest?> LoadAsync(Guid id, bool tracking)
    {
        var query = _context.ServiceRequests
            .Include(r => r.ServiceOrder)
            .Include(r => r.Photos)
            .AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(r => r.Id == id);
    }

    private static string? Validate(SaveServiceRequestRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.CustomerName) && string.IsNullOrWhiteSpace(body.VehiclePlate))
            return "Informe ao menos o nome do cliente ou a placa do veículo.";
        if (body.VehicleYear is < 0 || body.VehicleMileage is < 0)
            return "Ano ou quilometragem inválidos.";

        var document = DocumentValidator.Normalize(body.CustomerDocument);
        if (!string.IsNullOrWhiteSpace(document) && !DocumentValidator.IsValidCpfOrCnpj(document))
            return "CPF ou CNPJ do cliente inválido.";

        return null;
    }

    private static string Clip(string? value, int max)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static void Apply(ServiceRequest request, SaveServiceRequestRequest body, DateTime now)
    {
        request.CustomerName = Clip(body.CustomerName, 200);
        request.CustomerDocument = DocumentValidator.Normalize(body.CustomerDocument);
        request.CustomerPhone = Clip(body.CustomerPhone, 50);
        request.VehiclePlate = Clip(body.VehiclePlate, 20).ToUpperInvariant();
        request.VehicleBrand = Clip(body.VehicleBrand, 100);
        request.VehicleModel = Clip(body.VehicleModel, 150);
        request.VehicleYear = body.VehicleYear is > 0 ? body.VehicleYear : null;
        request.VehicleColor = Clip(body.VehicleColor, 50);
        request.VehicleMileage = body.VehicleMileage;
        request.ProblemReported = Clip(body.ProblemReported, 4000);
        request.Notes = Clip(body.Notes, 4000);
        request.UpdatedAt = now;
    }

    private static ServiceOrderPhotoDto ToPhotoDto(ServiceRequestPhoto photo) => new()
    {
        Id = photo.Id,
        Url = photo.Url,
        UploadedAt = photo.UploadedAt,
        UploadedBy = photo.UploadedBy,
    };

    internal static ServiceRequestDto ToDto(ServiceRequest r, bool includePhotos = true) => new()
    {
        Id = r.Id,
        Number = r.Number,
        Status = r.Status,
        CustomerName = r.CustomerName,
        CustomerDocument = r.CustomerDocument,
        CustomerPhone = r.CustomerPhone,
        VehiclePlate = r.VehiclePlate,
        VehicleBrand = r.VehicleBrand,
        VehicleModel = r.VehicleModel,
        VehicleYear = r.VehicleYear,
        VehicleColor = r.VehicleColor,
        VehicleMileage = r.VehicleMileage,
        ProblemReported = r.ProblemReported,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        CreatedBy = r.CreatedBy,
        UpdatedAt = r.UpdatedAt,
        DecidedAt = r.DecidedAt,
        DecidedBy = r.DecidedBy,
        ServiceOrderId = r.ServiceOrderId,
        ServiceOrderNumber = r.ServiceOrder?.Number,
        PhotoCount = r.Photos.Count,
        Photos = includePhotos
            ? r.Photos.OrderByDescending(p => p.UploadedAt).Select(ToPhotoDto).ToList()
            : new List<ServiceOrderPhotoDto>(),
    };
}
