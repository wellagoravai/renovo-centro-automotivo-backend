using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/service-orders")]
[Authorize]
public class ServiceOrdersController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWhatsAppService _whatsAppService;

    public ServiceOrdersController(RenovoWorkshopDbContext context, IMapper mapper, IWhatsAppService whatsAppService)
    {
        _context = context;
        _mapper = mapper;
        _whatsAppService = whatsAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var query = _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.History)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.Number.Contains(term) || o.Customer.Name.Contains(term) || o.Vehicle.Plate.Contains(term));
        }

        var orders = await query.OrderByDescending(o => o.EntryDate).ToListAsync();
        var orderDtos = _mapper.Map<List<ServiceOrderDto>>(orders);
        return Ok(orderDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var orderDto = _mapper.Map<ServiceOrderDto>(order);
        return Ok(orderDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Create([FromBody] CreateServiceOrderDto createOrderDto)
    {
        var customer = await _context.Customers.FindAsync(createOrderDto.CustomerId);
        var vehicle = await _context.Vehicles.FindAsync(createOrderDto.VehicleId);

        if (customer is null || vehicle is null) 
            return BadRequest(new { message = "Cliente ou veículo inválido." });

        var order = _mapper.Map<ServiceOrder>(createOrderDto);
        order.Id = Guid.NewGuid();
        order.Number = $"OS-{DateTime.UtcNow:yyyyMMddHHmmss}";
        order.EntryDate = DateTime.UtcNow;

        _context.ServiceOrders.Add(order);

        _context.ServiceOrderHistories.Add(new ServiceOrderHistory
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            Status = order.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = order.ResponsibleUser,
            Notes = "Ordem criada"
        });

        await _context.SaveChangesAsync();

        var orderDto = _mapper.Map<ServiceOrderDto>(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPost("with-customer-vehicle")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> CreateWithCustomerVehicle([FromBody] CreateServiceOrderWithCustomerVehicleDto request)
    {
        // Check if customer exists by document
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Document == request.Customer.Document);

        Customer customer;
        if (existingCustomer is null)
        {
            // Create new customer
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = request.Customer.Name,
                Document = request.Customer.Document,
                WhatsApp = request.Customer.WhatsApp,
                Phone = request.Customer.Phone,
                Email = request.Customer.Email,
                Address = request.Customer.Address,
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer = existingCustomer;
        }

        // Check if vehicle exists by plate
        var existingVehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Plate == request.Vehicle.Plate);

        Vehicle vehicle;
        if (existingVehicle is null)
        {
            // Create new vehicle
            vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Plate = request.Vehicle.Plate,
                Brand = request.Vehicle.Brand,
                Model = request.Vehicle.Model,
                Year = request.Vehicle.Year,
                Color = request.Vehicle.Color,
                Mileage = request.Vehicle.Mileage,
                Fuel = request.Vehicle.Fuel,
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Vehicles.Add(vehicle);
        }
        else
        {
            vehicle = existingVehicle;
        }

        // Create service order
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = $"OS-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ProblemReported = request.ProblemReported,
            Notes = request.Notes,
            EstimatedDate = request.EstimatedDate,
            Status = request.Status,
            ResponsibleUser = request.ResponsibleUser,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            EntryDate = DateTime.UtcNow
        };

        _context.ServiceOrders.Add(order);

        _context.ServiceOrderHistories.Add(new ServiceOrderHistory
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            Status = order.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = request.ResponsibleUser,
            Notes = "Ordem criada"
        });

        await _context.SaveChangesAsync();

        var orderDto = _mapper.Map<ServiceOrderDto>(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceOrderStatusDto request)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order is null) return NotFound();

        var previousStatus = order.Status;
        order.Status = request.Status;
        order.Notes = request.Notes ?? order.Notes;
        order.FinalDate = request.Status == "Entregue" ? DateTime.UtcNow : order.FinalDate;

        _context.ServiceOrderHistories.Add(new ServiceOrderHistory
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            Status = request.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = request.ChangedBy,
            Notes = request.Notes ?? $"Status alterado de {previousStatus} para {request.Status}"
        });

        await _context.SaveChangesAsync();

        var customer = await _context.Customers.FindAsync(order.CustomerId);
        if (customer is not null)
        {
            await _whatsAppService.SendStatusMessageAsync(order, customer, previousStatus, request.Status, request.Notes);
        }

        var orderDto = _mapper.Map<ServiceOrderDto>(order);
        return Ok(new { message = "Status atualizado com sucesso.", order = orderDto });
    }

    [HttpPatch("{id:guid}/checklist")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> AttachChecklist(Guid id, [FromBody] AttachChecklistRequest request)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order is null) return NotFound();

        order.HasChecklist = true;
        order.ChecklistId = request.ChecklistId;
        await _context.SaveChangesAsync();

        var orderDto = _mapper.Map<ServiceOrderDto>(order);
        return Ok(orderDto);
    }

    public record AttachChecklistRequest(Guid ChecklistId);
}