using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public InventoryController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] bool? lowStock = null)
    {
        var query = _context.InventoryItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Code.Contains(term) || i.Description.Contains(term) || i.Brand.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(i => i.Category == category);
        }

        if (lowStock.HasValue && lowStock.Value)
        {
            query = query.Where(i => i.Quantity <= i.MinimumQuantity);
        }

        var items = await query.OrderBy(i => i.Code).ToListAsync();
        var itemDtos = _mapper.Map<List<InventoryItemDto>>(items);
        return Ok(itemDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();

        var itemDto = _mapper.Map<InventoryItemDto>(item);
        return Ok(itemDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Create([FromBody] CreateInventoryItemDto createItemDto)
    {
        if (await _context.InventoryItems.AnyAsync(i => i.Code == createItemDto.Code))
            return Conflict(new { message = "Item já cadastrado com este código." });

        var item = _mapper.Map<InventoryItem>(createItemDto);
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.AverageValue = item.PurchaseValue;

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        var itemDto = _mapper.Map<InventoryItemDto>(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, itemDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInventoryItemDto updateItemDto)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();

        _mapper.Map(updateItemDto, item);
        await _context.SaveChangesAsync();

        var itemDto = _mapper.Map<InventoryItemDto>(item);
        return Ok(itemDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.InventoryItems
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();

        item.Quantity = request.NewQuantity;
        await _context.SaveChangesAsync();

        var itemDto = _mapper.Map<InventoryItemDto>(item);
        return Ok(itemDto);
    }

    public record UpdateStockRequest(int NewQuantity);
}