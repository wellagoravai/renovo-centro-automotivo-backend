using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
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
    private readonly IServiceOrderStatusService _statusService;
    private readonly IPhotoStorageService _photoStorageService;
    private readonly IQuoteDocumentService _quoteDocumentService;

    public ServiceOrdersController(RenovoWorkshopDbContext context, IMapper mapper, IServiceOrderStatusService statusService, IPhotoStorageService photoStorageService, IQuoteDocumentService quoteDocumentService)
    {
        _context = context;
        _mapper = mapper;
        _statusService = statusService;
        _photoStorageService = photoStorageService;
        _quoteDocumentService = quoteDocumentService;
    }

    // Mesmo PDF que sai automaticamente no WhatsApp quando a OS entra em "Aguardando
    // aprovação" — aqui pra gerar sob demanda (inclusive antes desse status, ou pra
    // reimprimir) e cobrir o caso do cliente sem WhatsApp, que recebe manualmente.
    [HttpGet("{id:guid}/quote-pdf")]
    public async Task<IActionResult> GetQuotePdf(Guid id)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var pdfBytes = _quoteDocumentService.GenerateQuotePdf(order, order.Customer);
        return File(pdfBytes, "application/pdf", $"Orcamento-{order.Number}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] string? search = null, [FromQuery] Guid? assignedUserId = null, [FromQuery] string? serviceType = null)
    {
        var query = _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(serviceType))
            query = query.Where(o => o.ServiceType == serviceType);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (assignedUserId.HasValue)
            query = query.Where(o => o.AssignedUserId == assignedUserId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.Number.Contains(term) || o.Customer.Name.Contains(term) || o.Vehicle.Plate.Contains(term));
        }

        var orders = await query.OrderByDescending(o => o.EntryDate).ToListAsync();
        var orderDtos = _mapper.Map<List<ServiceOrderDto>>(orders);
        await AttachTowDetailsAsync(orders, orderDtos);
        return Ok(orderDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var orderDto = await MapToDtoAsync(order);
        return Ok(orderDto);
    }

    // ServiceOrder não tem navegação pra TowServiceDetails (mesma convenção já usada
    // para VehicleCheckList) — por isso o AutoMapper não preenche TowDetails sozinho,
    // e cada leitura de OS busca isso à parte, só quando ServiceType == Guincho.
    private async Task<ServiceOrderDto> MapToDtoAsync(ServiceOrder order)
    {
        var dto = _mapper.Map<ServiceOrderDto>(order);
        if (order.ServiceType == ServiceOrderTypes.Guincho)
        {
            var towDetails = await _context.TowServiceDetails.FirstOrDefaultAsync(t => t.ServiceOrderId == order.Id);
            if (towDetails is not null)
                dto.TowDetails = _mapper.Map<TowServiceDetailsDto>(towDetails);
        }
        return dto;
    }

    private async Task AttachTowDetailsAsync(List<ServiceOrder> orders, List<ServiceOrderDto> dtos)
    {
        var towOrderIds = orders.Where(o => o.ServiceType == ServiceOrderTypes.Guincho).Select(o => o.Id).ToList();
        if (towOrderIds.Count == 0) return;

        var towDetailsByOrderId = await _context.TowServiceDetails
            .Where(t => towOrderIds.Contains(t.ServiceOrderId))
            .ToDictionaryAsync(t => t.ServiceOrderId);

        foreach (var dto in dtos)
        {
            if (towDetailsByOrderId.TryGetValue(dto.Id, out var towDetails))
                dto.TowDetails = _mapper.Map<TowServiceDetailsDto>(towDetails);
        }
    }

    private async Task UpsertTowDetailsAsync(Guid orderId, TowServiceDetailsDto towDetailsDto)
    {
        var existing = await _context.TowServiceDetails.FirstOrDefaultAsync(t => t.ServiceOrderId == orderId);
        if (existing is null)
        {
            var entity = _mapper.Map<TowServiceDetails>(towDetailsDto);
            entity.Id = Guid.NewGuid();
            entity.ServiceOrderId = orderId;
            _context.TowServiceDetails.Add(entity);
        }
        else
        {
            _mapper.Map(towDetailsDto, existing);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceOrderDto updateDto)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        _mapper.Map(updateDto, order);
        RecalculateValue(order);

        if (updateDto.TowDetails is not null)
            await UpsertTowDetailsAsync(order.Id, updateDto.TowDetails);

        await _context.SaveChangesAsync();

        // AssignedUserId pode ter mudado; a navegação já carregada acima não
        // se atualiza sozinha só porque o FK escalar mudou.
        await _context.Entry(order).Reference(o => o.AssignedUser).LoadAsync();

        var orderDto = await MapToDtoAsync(order);
        return Ok(orderDto);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] CreateServiceOrderItemDto request)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order is null) return NotFound();

        var inventoryItem = await _context.InventoryItems.FindAsync(request.InventoryItemId);
        if (inventoryItem is null) return BadRequest(new { message = "Item de estoque não encontrado." });

        if (request.Quantity <= 0) return BadRequest(new { message = "Quantidade deve ser maior que zero." });

        var item = new ServiceOrderItem
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            InventoryItemId = inventoryItem.Id,
            Quantity = request.Quantity,
            UnitValue = inventoryItem.SaleValue,
        };

        _context.ServiceOrderItems.Add(item);
        await _context.SaveChangesAsync();

        var updatedOrder = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstAsync(o => o.Id == id);

        RecalculateValue(updatedOrder);
        await _context.SaveChangesAsync();

        return Ok(await MapToDtoAsync(updatedOrder));
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        var item = await _context.ServiceOrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ServiceOrderId == id);
        if (item is null) return NotFound();

        _context.ServiceOrderItems.Remove(item);
        await _context.SaveChangesAsync();

        var order = await _context.ServiceOrders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == id);
        RecalculateValue(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Value nunca é digitado diretamente — é sempre mão de obra + soma das peças
    // lançadas, para o resumo de orçamento mandado ao cliente (e os relatórios que
    // leem Value) nunca ficarem incoerentes com os itens reais da OS.
    private static void RecalculateValue(ServiceOrder order)
    {
        order.Value = order.LaborValue + order.Items.Sum(i => i.Quantity * i.UnitValue);
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
        RecalculateValue(order);

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

        if (order.ServiceType == ServiceOrderTypes.Guincho && createOrderDto.TowDetails is not null)
            await UpsertTowDetailsAsync(order.Id, createOrderDto.TowDetails);

        await _context.SaveChangesAsync();

        var orderDto = await MapToDtoAsync(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPost("with-customer-vehicle")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> CreateWithCustomerVehicle([FromBody] CreateServiceOrderWithCustomerVehicleDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Customer.Name))
            return BadRequest(new { message = "O nome do cliente é obrigatório." });

        var document = request.Customer.Document.Trim();
        var existingCustomer = string.IsNullOrWhiteSpace(document)
            ? null
            : await _context.Customers.FirstOrDefaultAsync(c => c.Document == document);

        Customer customer;
        if (existingCustomer is null)
        {
            // Create new customer
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = request.Customer.Name.Trim(),
                Document = document,
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
            customer.Name = request.Customer.Name.Trim();
            customer.WhatsApp = request.Customer.WhatsApp;
            customer.Phone = request.Customer.Phone;
            customer.Email = request.Customer.Email;
            customer.Address = request.Customer.Address;
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
            ServiceType = request.ServiceType,
            ProblemReported = request.ProblemReported,
            Services = request.Services,
            Notes = request.Notes,
            EstimatedDate = request.EstimatedDate,
            Status = request.Status,
            ResponsibleUser = request.ResponsibleUser,
            AssignedUserId = request.AssignedUserId,
            Photos = request.Photos,
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

        if (order.ServiceType == ServiceOrderTypes.Guincho && request.TowDetails is not null)
            await UpsertTowDetailsAsync(order.Id, request.TowDetails);

        await _context.SaveChangesAsync();

        await _context.Entry(order).Reference(o => o.Customer).LoadAsync();
        await _context.Entry(order).Reference(o => o.Vehicle).LoadAsync();

        var orderDto = await MapToDtoAsync(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceOrderStatusDto request)
    {
        var result = await _statusService.ChangeStatusAsync(id, request.Status, request.Notes, request.ChangedBy, HttpContext.RequestAborted);
        if (result.Order is null) return NotFound();

        var orderDto = await MapToDtoAsync(result.Order);
        return Ok(new { message = result.Message, order = orderDto });
    }

    // O guincho traz o carro e o cliente decide deixar pra reparo: em vez de abrir
    // uma OS nova (recadastrando cliente/veículo), a mesma OS muda de tipo e passa a
    // valer o fluxo de status/diagnóstico/peças de oficina. O TowServiceDetails
    // já registrado fica como está, como histórico de que essa OS começou como guincho.
    [HttpPatch("{id:guid}/convert-to-oficina")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> ConvertToOficina(Guid id, [FromBody] ConvertToOficinaRequest request)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.Vehicle)
            .Include(o => o.AssignedUser)
            .Include(o => o.History)
            .Include(o => o.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        if (order.ServiceType != ServiceOrderTypes.Guincho)
            return BadRequest(new { message = "Só é possível converter uma OS que hoje é do tipo Guincho." });

        order.ServiceType = ServiceOrderTypes.Oficina;
        order.Status = ServiceOrderStatuses.Oficina[0];

        _context.ServiceOrderHistories.Add(new ServiceOrderHistory
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            Status = order.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = request.ChangedBy,
            Notes = "Convertida de Guincho para Oficina (reparo)"
        });

        await _context.SaveChangesAsync();

        var orderDto = await MapToDtoAsync(order);
        return Ok(orderDto);
    }

    public record ConvertToOficinaRequest(string ChangedBy);

    [HttpPatch("{id:guid}/checklist")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> AttachChecklist(Guid id, [FromBody] AttachChecklistRequest request)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order is null) return NotFound();

        order.HasChecklist = true;
        order.ChecklistId = request.ChecklistId;
        await _context.SaveChangesAsync();

        var orderDto = await MapToDtoAsync(order);
        return Ok(orderDto);
    }

    public record AttachChecklistRequest(Guid ChecklistId);

    private const long MaxPhotoSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic"
    };

    [HttpGet("{id:guid}/photos")]
    public async Task<IActionResult> GetPhotos(Guid id)
    {
        var exists = await _context.ServiceOrders.AnyAsync(o => o.Id == id);
        if (!exists) return NotFound();

        var photos = await _context.ServiceOrderPhotos
            .Where(p => p.ServiceOrderId == id)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return Ok(_mapper.Map<List<ServiceOrderPhotoDto>>(photos));
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize(Policy = "CanManageOrders")]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order is null) return NotFound();

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

        var photo = new ServiceOrderPhoto
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = id,
            Url = url,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = User.Identity?.Name ?? "Desconhecido"
        };

        _context.ServiceOrderPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPhotos), new { id }, _mapper.Map<ServiceOrderPhotoDto>(photo));
    }
}