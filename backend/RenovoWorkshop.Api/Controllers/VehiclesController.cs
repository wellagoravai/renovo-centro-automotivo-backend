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
public class VehiclesController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public VehiclesController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] Guid? customerId = null)
    {
        var query = _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceOrders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(v => v.Plate.Contains(term) || v.Brand.Contains(term) || v.Model.Contains(term) || v.Chassis.Contains(term));
        }

        if (customerId.HasValue)
        {
            query = query.Where(v => v.CustomerId == customerId.Value);
        }

        var vehicles = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
        var vehicleDtos = _mapper.Map<List<VehicleDto>>(vehicles);
        return Ok(vehicleDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceOrders)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null) return NotFound();

        var vehicleDto = _mapper.Map<VehicleDto>(vehicle);
        return Ok(vehicleDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageVehicles")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto createVehicleDto)
    {
        if (await _context.Vehicles.AnyAsync(v => v.Plate == createVehicleDto.Plate))
            return Conflict(new { message = "Veículo já cadastrado com esta placa." });

        var vehicle = _mapper.Map<Vehicle>(createVehicleDto);
        vehicle.Id = Guid.NewGuid();
        vehicle.CreatedAt = DateTime.UtcNow;

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var vehicleDto = _mapper.Map<VehicleDto>(vehicle);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicleDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageVehicles")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto updateVehicleDto)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        _mapper.Map(updateVehicleDto, vehicle);
        await _context.SaveChangesAsync();

        var vehicleDto = _mapper.Map<VehicleDto>(vehicle);
        return Ok(vehicleDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageVehicles")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        var vehicles = await _context.Vehicles
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        var vehicleDtos = _mapper.Map<List<VehicleDto>>(vehicles);
        return Ok(vehicleDtos);
    }
}