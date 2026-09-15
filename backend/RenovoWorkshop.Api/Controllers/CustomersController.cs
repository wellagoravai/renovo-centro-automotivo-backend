using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Api.Helpers;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public CustomersController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _context.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.ServiceOrders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Document.Contains(term) || c.Phone.Contains(term));
        }

        var customers = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        var customerDtos = _mapper.Map<List<CustomerDto>>(customers);
        return Ok(customerDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _context.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.ServiceOrders)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return NotFound();

        var customerDto = _mapper.Map<CustomerDto>(customer);
        return Ok(customerDto);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string? document = null, [FromQuery] string? plate = null)
    {
        var normalizedDocument = string.IsNullOrWhiteSpace(document) ? null : DocumentValidator.Normalize(document);
        var normalizedPlate = string.IsNullOrWhiteSpace(plate) ? null : plate.Trim().ToUpperInvariant();

        if (normalizedDocument is null && normalizedPlate is null)
            return BadRequest(new { message = "Informe CPF/CNPJ ou placa para a consulta." });

        Customer? customer = normalizedDocument is not null
            ? await _context.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Document == normalizedDocument)
            : null;
        Vehicle? vehicle = null;

        if (normalizedPlate is not null)
        {
            vehicle = await _context.Vehicles.Include(v => v.Customer)
                .FirstOrDefaultAsync(v => v.Plate == normalizedPlate);
            customer ??= vehicle?.Customer;
        }

        if (customer is null && vehicle is null)
            return NotFound();

        return Ok(new
        {
            customer = customer is null ? null : _mapper.Map<CustomerDto>(customer),
            vehicle = vehicle is null ? null : _mapper.Map<VehicleDto>(vehicle)
        });
    }

    [HttpPost]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto createCustomerDto)
    {
        var document = DocumentValidator.Normalize(createCustomerDto.Document);
        if (!DocumentValidator.IsValidCpfOrCnpj(document))
            return BadRequest(new { message = "Informe um CPF ou CNPJ válido." });

        createCustomerDto.Document = document;

        if (await _context.Customers.AnyAsync(c => c.Document == createCustomerDto.Document))
            return Conflict(new { message = "Cliente já cadastrado com este CPF/CNPJ." });

        var customer = _mapper.Map<Customer>(createCustomerDto);
        customer.Id = Guid.NewGuid();
        customer.CreatedAt = DateTime.UtcNow;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var customerDto = _mapper.Map<CustomerDto>(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customerDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto updateCustomerDto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var document = DocumentValidator.Normalize(updateCustomerDto.Document);
        if (!DocumentValidator.IsValidCpfOrCnpj(document))
            return BadRequest(new { message = "Informe um CPF ou CNPJ válido." });

        updateCustomerDto.Document = document;

        _mapper.Map(updateCustomerDto, customer);
        await _context.SaveChangesAsync();

        var customerDto = _mapper.Map<CustomerDto>(customer);
        return Ok(customerDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}