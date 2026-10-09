using AutoMapper;
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
    private readonly INotificationService _notificationService;
    private readonly IQuoteImageLoader _quoteImageLoader;

    public ServiceOrdersController(RenovoWorkshopDbContext context, IMapper mapper, IServiceOrderStatusService statusService, IPhotoStorageService photoStorageService, IQuoteDocumentService quoteDocumentService, INotificationService notificationService, IQuoteImageLoader quoteImageLoader)
    {
        _quoteImageLoader = quoteImageLoader;
        _context = context;
        _mapper = mapper;
        _statusService = statusService;
        _photoStorageService = photoStorageService;
        _quoteDocumentService = quoteDocumentService;
        _notificationService = notificationService;
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

        var images = await _quoteImageLoader.LoadAsync(order, HttpContext.RequestAborted);
        var pdfBytes = _quoteDocumentService.GenerateQuotePdf(order, order.Customer, images);
        return File(pdfBytes, "application/pdf", $"Orcamento-{order.Number}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] string? search = null, [FromQuery] Guid? assignedUserId = null, [FromQuery] string? serviceType = null, [FromQuery] string? responsibleUser = null)
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

        var orders = await query.OrderByDescending(o => o.EntryDate).ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            orders = orders.Where(o =>
                SearchNormalizer.Contains(o.Number, term) ||
                SearchNormalizer.Contains(o.Customer.Name, term) ||
                SearchNormalizer.Contains(o.Vehicle.Plate, term)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(responsibleUser))
        {
            var term = responsibleUser.Trim();
            orders = orders.Where(o => SearchNormalizer.Contains(o.ResponsibleUser, term)).ToList();
        }

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

        // Campos de check-in: só sobrescreve quando o cliente enviou o campo — evita que
        // um PUT parcial (ex.: só diagnóstico) apague responsável, problema relatado ou
        // dados do cliente/veículo por omissão. Veículo é uma entidade compartilhada por
        // outras OS, então a edição aqui reflete nelas também.
        if (updateDto.ResponsibleUser is not null) order.ResponsibleUser = updateDto.ResponsibleUser;
        if (updateDto.ProblemReported is not null) order.ProblemReported = updateDto.ProblemReported;
        if (updateDto.ClearEstimatedDate == true) order.EstimatedDate = null;
        else if (updateDto.EstimatedDate.HasValue) order.EstimatedDate = DateTime.SpecifyKind(updateDto.EstimatedDate.Value, DateTimeKind.Utc);

        // Troca de cliente: reaponta a OS para outro Customer já cadastrado (por Id), em vez
        // de renomear o Customer atual — renomear mutaria um registro compartilhado por
        // outras OS (foi exatamente isso que causou o incidente em produção de 2026-09-16,
        // onde abrir uma OS com CPF já cadastrado sobrescreveu o nome do cliente em todas as
        // OS que apontavam pra ele).
        if (updateDto.CustomerId.HasValue && updateDto.CustomerId.Value != order.CustomerId)
        {
            var newCustomer = await _context.Customers.FindAsync(updateDto.CustomerId.Value);
            if (newCustomer is null)
                return BadRequest(new { message = "Cliente selecionado não encontrado." });

            order.CustomerId = newCustomer.Id;
            order.Customer = newCustomer;
        }

        if (updateDto.VehiclePlate is not null) order.Vehicle.Plate = updateDto.VehiclePlate;
        if (updateDto.VehicleBrand is not null) order.Vehicle.Brand = updateDto.VehicleBrand;
        if (updateDto.VehicleModel is not null) order.Vehicle.Model = updateDto.VehicleModel;
        if (updateDto.VehicleColor is not null) order.Vehicle.Color = updateDto.VehicleColor;
        if (updateDto.VehicleYear.HasValue) order.Vehicle.Year = updateDto.VehicleYear.Value;
        if (updateDto.VehicleMileage.HasValue) order.Vehicle.Mileage = updateDto.VehicleMileage.Value;

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

    private static void RecalculateValue(ServiceOrder order) => order.RecalculateValue();

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
        var (prepared, error) = await PrepareOrderAsync(request, "Ordem criada");
        if (error is not null) return error;
        return await CompleteOrderAsync(prepared!);
    }

    // Pedido (rascunho de OS de oficina) aprovado: abre a OS com os dados do pedido, pelas mesmas
    // regras de cliente/veículo do check-in, e leva as fotos do pedido para a OS. Tudo
    // num único SaveChanges — ou a OS nasce com as fotos e o pedido marcado, ou nada muda.
    [HttpPost("from-request/{requestId:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> CreateFromRequest(Guid requestId, [FromBody] CreateOrderFromRequestRequest body)
    {
        var serviceRequest = await _context.ServiceRequests
            .Include(r => r.Photos)
            .Include(r => r.ServiceOrder)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (serviceRequest is null) return NotFound();

        if (serviceRequest.Status == ServiceRequestStatuses.OsCriada)
            return Conflict(new { message = $"Este pedido já virou a OS {serviceRequest.ServiceOrder?.Number}.", serviceOrderId = serviceRequest.ServiceOrderId });
        if (serviceRequest.Status != ServiceRequestStatuses.Aprovado)
            return BadRequest(new { message = "Marque o pedido como aprovado antes de criar a OS." });

        // Pedido é sempre de oficina (guincho tem o próprio fluxo de chamado/orçamento).
        var orderRequest = new CreateServiceOrderWithCustomerVehicleDto
        {
            ServiceType = ServiceOrderTypes.Oficina,
            ProblemReported = serviceRequest.ProblemReported,
            Notes = serviceRequest.Notes,
            ResponsibleUser = string.IsNullOrWhiteSpace(body.ResponsibleUser) ? User.Identity?.Name ?? string.Empty : body.ResponsibleUser,
            AssignedUserId = body.AssignedUserId,
            Customer = new CustomerInfoDto
            {
                Name = serviceRequest.CustomerName,
                Document = serviceRequest.CustomerDocument,
                Phone = serviceRequest.CustomerPhone,
            },
            Vehicle = new VehicleInfoDto
            {
                Plate = serviceRequest.VehiclePlate,
                Brand = serviceRequest.VehicleBrand,
                Model = serviceRequest.VehicleModel,
                Year = serviceRequest.VehicleYear ?? 0,
                Color = serviceRequest.VehicleColor,
                Mileage = serviceRequest.VehicleMileage ?? 0,
            },
        };

        var (prepared, error) = await PrepareOrderAsync(orderRequest, $"Ordem criada a partir do pedido {serviceRequest.Number}");
        if (error is not null) return error;
        var order = prepared!.Order;

        // Move (não copia) as fotos: o arquivo passa a ser da OS, e excluir a foto na OS
        // não deixa um link quebrado para trás no pedido.
        foreach (var photo in serviceRequest.Photos)
        {
            _context.ServiceOrderPhotos.Add(new ServiceOrderPhoto
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = order.Id,
                Url = photo.Url,
                UploadedAt = photo.UploadedAt,
                UploadedBy = photo.UploadedBy,
            });
        }
        _context.ServiceRequestPhotos.RemoveRange(serviceRequest.Photos);

        serviceRequest.Status = ServiceRequestStatuses.OsCriada;
        serviceRequest.ServiceOrderId = order.Id;
        serviceRequest.UpdatedAt = DateTime.UtcNow;

        return await CompleteOrderAsync(prepared);
    }

    private sealed record PreparedOrder(ServiceOrder Order, Customer Customer, bool CustomerWasCreated);

    // Monta cliente/veículo/OS no contexto sem salvar — quem chama pode juntar mais
    // alterações na mesma transação antes de CompleteOrderAsync.
    private async Task<(PreparedOrder? Prepared, IActionResult? Error)> PrepareOrderAsync(CreateServiceOrderWithCustomerVehicleDto request, string historyNote)
    {
        if (string.IsNullOrWhiteSpace(request.Customer.Name))
            return (null, BadRequest(new { message = "O nome do cliente é obrigatório." }));

        var document = DocumentValidator.Normalize(request.Customer.Document);
        if (!string.IsNullOrWhiteSpace(document) && !DocumentValidator.IsValidCpfOrCnpj(document))
            return (null, BadRequest(new { message = "CPF ou CNPJ do cliente inválido." }));

        request.Customer.Document = document;
        var isTow = request.ServiceType == ServiceOrderTypes.Guincho;

        var existingCustomer = string.IsNullOrWhiteSpace(document)
            ? null
            : await _context.Customers.FirstOrDefaultAsync(c => c.Document == document);

        Customer customer;
        var customerWasCreated = false;
        if (existingCustomer is null)
        {
            // CPF/CNPJ informado mas não cadastrado: não cria o cliente aqui — o
            // check-in fica pendente até o cliente ser cadastrado de verdade pela tela
            // de Clientes (evita registros incompletos/duplicados criados às pressas
            // durante a abertura da OS). O front reenvia esta mesma chamada depois que
            // o cliente existir.
            // Exceção: guincho é atendimento de urgência (o chamado chega com o CPF do
            // associado e ninguém vai parar a remoção para cadastrar cliente). Cria o
            // registro com o documento e gera o mesmo aviso "Cadastrar Cliente" abaixo.
            if (!string.IsNullOrWhiteSpace(document) && !isTow)
                return (null, NotFound(new { message = "Cliente não cadastrado com este CPF/CNPJ. Cadastre o cliente antes de continuar.", document }));

            // Sem documento (atendimento avulso/urgência, ex.: guincho): cria um registro
            // simples aqui mesmo. Documento vazio nunca é usado em busca (ver acima), então
            // esse registro nunca é encontrado/reaproveitado por outro check-in depois —
            // sem risco do merge que já causou incidente em produção.
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
            customerWasCreated = true;
        }
        else
        {
            customer = existingCustomer;
            var incomingName = request.Customer.Name.Trim();
            if (!string.Equals(customer.Name, incomingName, StringComparison.OrdinalIgnoreCase))
                return (null, Conflict(new { message = $"O CPF/CNPJ informado já está cadastrado para o cliente \"{customer.Name}\". Confirme os dados antes de continuar." }));

            customer.WhatsApp = request.Customer.WhatsApp;
            customer.Phone = request.Customer.Phone;
            customer.Email = request.Customer.Email;
            customer.Address = request.Customer.Address;
        }

        // Check if vehicle exists by plate
        var normalizedPlate = (request.Vehicle.Plate ?? string.Empty).Trim().ToUpperInvariant();

        // Na OS de guincho os dados do veículo só são conhecidos no local da remoção:
        // sem placa, cria um veículo "a definir" só desta OS (placa vazia nunca é usada
        // em busca, então não é reaproveitado). Na oficina a placa continua obrigatória.
        if (string.IsNullOrWhiteSpace(normalizedPlate) && !isTow)
            return (null, BadRequest(new { message = "Informe a placa do veículo." }));

        var existingVehicle = string.IsNullOrWhiteSpace(normalizedPlate)
            ? null
            : await _context.Vehicles.FirstOrDefaultAsync(v => v.Plate == normalizedPlate);

        Vehicle vehicle;
        string? previousOwnerName = null;
        if (existingVehicle is null)
        {
            // Create new vehicle
            vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Plate = normalizedPlate,
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
            // Carro que já passou pela oficina com outro dono (vendido, ou trazido por
            // outra pessoa): o veículo passa a ser do cliente desta OS. As OS antigas
            // não mudam — cada OS guarda o próprio CustomerId, então continuam no nome
            // de quem as abriu.
            if (vehicle.CustomerId != customer.Id)
            {
                previousOwnerName = await _context.Customers
                    .Where(c => c.Id == vehicle.CustomerId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
                vehicle.CustomerId = customer.Id;
            }
        }

        // Status inicial precisa ser do fluxo do tipo: o DTO tem "Recebido" (oficina)
        // como padrão, o que deixava a OS de guincho fora do fluxo e do painel.
        var initialStatus = ServiceOrderStatuses.ForServiceType(request.ServiceType).Contains(request.Status)
            ? request.Status
            : ServiceOrderStatuses.ForServiceType(request.ServiceType)[0];

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
            Status = initialStatus,
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
            Notes = previousOwnerName is null
                ? historyNote
                : $"{historyNote}. Veículo transferido do cliente \"{previousOwnerName}\" para \"{customer.Name}\"."
        });

        if (order.ServiceType == ServiceOrderTypes.Guincho && request.TowDetails is not null)
            await UpsertTowDetailsAsync(order.Id, request.TowDetails);

        return (new PreparedOrder(order, customer, customerWasCreated), null);
    }

    private async Task<IActionResult> CompleteOrderAsync(PreparedOrder prepared)
    {
        var (order, customer, customerWasCreated) = prepared;
        await _context.SaveChangesAsync();

        if (customerWasCreated)
        {
            _context.DashboardNotifications.Add(new DashboardNotification
            {
                Id = Guid.NewGuid(),
                Type = "customer-registration",
                Title = "Cadastrar Cliente",
                Message = $"Conclua o cadastro de {customer.Name}.",
                CustomerId = customer.Id,
                ServiceOrderId = order.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await _notificationService.NotifyCustomerRegistrationAsync(customer.Id, customer.Name, order.Id, order.Number, HttpContext.RequestAborted);
        }

        await _context.Entry(order).Reference(o => o.Customer).LoadAsync();
        await _context.Entry(order).Reference(o => o.Vehicle).LoadAsync();

        var orderDto = await MapToDtoAsync(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, orderDto);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceOrderStatusDto request)
    {
        var serviceType = await _context.ServiceOrders
            .Where(o => o.Id == id)
            .Select(o => o.ServiceType)
            .FirstOrDefaultAsync();
        if (serviceType is null) return NotFound();
        if (!ServiceOrderStatuses.IsValidFor(serviceType, request.Status))
            return BadRequest(new { message = $"Status \"{request.Status}\" não é válido para OS de {serviceType}." });

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

    // Remove foto enviada por engano. Se for a foto do painel (KM inicial/final do
    // caminhão), também desvincula dos dados do guincho para não ficar URL quebrada.
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId)
    {
        var photo = await _context.ServiceOrderPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.ServiceOrderId == id);
        if (photo is null) return NotFound();

        var tow = await _context.TowServiceDetails.FirstOrDefaultAsync(t => t.ServiceOrderId == id);
        if (tow is not null)
        {
            if (tow.TruckStartKmPhotoUrl == photo.Url) tow.TruckStartKmPhotoUrl = null;
            if (tow.TruckEndKmPhotoUrl == photo.Url) tow.TruckEndKmPhotoUrl = null;
        }

        _context.ServiceOrderPhotos.Remove(photo);
        await _context.SaveChangesAsync();
        await _photoStorageService.DeleteAsync(photo.Url, HttpContext.RequestAborted);

        return NoContent();
    }
}