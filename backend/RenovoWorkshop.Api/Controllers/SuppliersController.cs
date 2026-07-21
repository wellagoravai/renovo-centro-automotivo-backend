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
public class SuppliersController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public SuppliersController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Document.Contains(term) || s.Email.Contains(term));
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
        var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
        return Ok(supplierDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return NotFound();

        var supplierDto = _mapper.Map<SupplierDto>(supplier);
        return Ok(supplierDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto createSupplierDto)
    {
        var supplier = _mapper.Map<Supplier>(createSupplierDto);
        supplier.Id = Guid.NewGuid();
        supplier.CreatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var supplierDto = _mapper.Map<SupplierDto>(supplier);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplierDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto updateSupplierDto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();

        _mapper.Map(updateSupplierDto, supplier);
        await _context.SaveChangesAsync();

        var supplierDto = _mapper.Map<SupplierDto>(supplier);
        return Ok(supplierDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}