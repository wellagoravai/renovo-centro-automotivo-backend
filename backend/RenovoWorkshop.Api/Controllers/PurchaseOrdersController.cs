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
public class PurchaseOrdersController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public PurchaseOrdersController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] Guid? supplierId = null)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        var orders = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        var orderDtos = _mapper.Map<List<PurchaseOrderDto>>(orders);
        return Ok(orderDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order is null) return NotFound();

        var orderDto = _mapper.Map<PurchaseOrderDto>(order);
        return Ok(orderDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto createOrderDto)
    {
        var order = _mapper.Map<PurchaseOrder>(createOrderDto);
        order.Id = Guid.NewGuid();
        order.Number = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Pendente";

        if (order.Items is not null)
        {
            foreach (var item in order.Items)
            {
                item.Id = Guid.NewGuid();
            }
        }

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        var orderDto = _mapper.Map<PurchaseOrderDto>(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePurchaseOrderStatusDto updateStatusDto)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null) return NotFound();

        order.Status = updateStatusDto.Status;
        order.Notes = updateStatusDto.Notes ?? order.Notes;

        await _context.SaveChangesAsync();

        var orderDto = _mapper.Map<PurchaseOrderDto>(order);
        return Ok(orderDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null) return NotFound();

        _context.PurchaseOrders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}