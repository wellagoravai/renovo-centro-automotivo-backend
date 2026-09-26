using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

// Custos de viagem do guincho (tela "Custos Viagem" do app da equipe).
[ApiController]
[Route("api/trip-expenses")]
[Authorize]
public class TripExpensesController : ControllerBase
{
    private const long MaxPhotoSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic"
    };

    private readonly RenovoWorkshopDbContext _context;
    private readonly IPhotoStorageService _photoStorageService;

    public TripExpensesController(RenovoWorkshopDbContext context, IPhotoStorageService photoStorageService)
    {
        _context = context;
        _photoStorageService = photoStorageService;
    }

    [HttpGet("categories")]
    public IActionResult GetCategories() => Ok(TripExpenseCategories.All);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? serviceOrderId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _context.TripExpenses
            .Include(e => e.ServiceOrder).ThenInclude(o => o!.Vehicle)
            .AsQueryable();

        if (serviceOrderId.HasValue) query = query.Where(e => e.ServiceOrderId == serviceOrderId);
        if (from.HasValue) query = query.Where(e => e.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(e => e.CreatedAt < to.Value);

        var expenses = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(500)
            .ToListAsync();

        return Ok(expenses.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageOrders")]
    [RequestSizeLimit(MaxPhotoSizeBytes + 64 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateTripExpenseRequest request)
    {
        if (!TripExpenseCategories.IsValid(request.Category))
            return BadRequest(new { message = "Categoria de custo inválida." });

        var normalizedAmount = (request.Amount ?? string.Empty).Trim().Replace(',', '.');
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return BadRequest(new { message = "Informe um valor maior que zero." });

        ServiceOrder? order = null;
        if (request.ServiceOrderId.HasValue)
        {
            order = await _context.ServiceOrders.Include(o => o.Vehicle).FirstOrDefaultAsync(o => o.Id == request.ServiceOrderId.Value);
            if (order is null)
                return BadRequest(new { message = "OS vinculada não encontrada." });
        }

        string? photoUrl = null;
        if (request.Photo is not null && request.Photo.Length > 0)
        {
            if (request.Photo.Length > MaxPhotoSizeBytes)
                return BadRequest(new { message = "Arquivo excede o limite de 10MB." });
            if (!AllowedPhotoContentTypes.Contains(request.Photo.ContentType))
                return BadRequest(new { message = "Formato de imagem não suportado." });

            try
            {
                await using var stream = request.Photo.OpenReadStream();
                photoUrl = await _photoStorageService.UploadAsync(stream, request.Photo.FileName, HttpContext.RequestAborted);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }

        var expense = new TripExpense
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            Amount = Math.Round(amount, 2),
            Description = request.Description?.Trim() ?? string.Empty,
            PhotoUrl = photoUrl,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "Desconhecido",
            ServiceOrderId = order?.Id,
            ServiceOrder = order,
        };

        _context.TripExpenses.Add(expense);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { serviceOrderId = expense.ServiceOrderId }, ToDto(expense));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await _context.TripExpenses.FindAsync(id);
        if (expense is null) return NotFound();

        _context.TripExpenses.Remove(expense);
        await _context.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(expense.PhotoUrl))
            await _photoStorageService.DeleteAsync(expense.PhotoUrl, HttpContext.RequestAborted);
        return NoContent();
    }

    private static TripExpenseDto ToDto(TripExpense expense) => new()
    {
        Id = expense.Id,
        Category = expense.Category,
        Amount = expense.Amount,
        Description = expense.Description,
        PhotoUrl = expense.PhotoUrl,
        CreatedAt = expense.CreatedAt,
        CreatedBy = expense.CreatedBy,
        ServiceOrderId = expense.ServiceOrderId,
        ServiceOrderNumber = expense.ServiceOrder?.Number,
        VehiclePlate = expense.ServiceOrder?.Vehicle?.Plate,
    };
}
