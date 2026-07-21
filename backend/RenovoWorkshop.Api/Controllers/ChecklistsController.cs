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
public class ChecklistsController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public ChecklistsController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? serviceOrderId = null)
    {
        var query = _context.VehicleCheckLists
            .Include(c => c.Vehicle)
            .Include(c => c.ServiceOrder)
            .AsQueryable();

        if (serviceOrderId.HasValue)
        {
            query = query.Where(c => c.ServiceOrderId == serviceOrderId.Value);
        }

        var checklists = await query.OrderByDescending(c => c.CheckedAt).ToListAsync();
        var checklistDtos = _mapper.Map<List<VehicleCheckListDto>>(checklists);
        return Ok(checklistDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var checklist = await _context.VehicleCheckLists
            .Include(c => c.Vehicle)
            .Include(c => c.ServiceOrder)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (checklist is null) return NotFound();

        var checklistDto = _mapper.Map<VehicleCheckListDto>(checklist);
        return Ok(checklistDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Create([FromBody] CreateCheckListDto createCheckListDto)
    {
        var checklist = _mapper.Map<VehicleCheckList>(createCheckListDto);
        checklist.Id = Guid.NewGuid();
        checklist.CheckedAt = DateTime.UtcNow;

        _context.VehicleCheckLists.Add(checklist);

        var serviceOrder = await _context.ServiceOrders.FindAsync(checklist.ServiceOrderId);
        if (serviceOrder is not null)
        {
            serviceOrder.HasChecklist = true;
            serviceOrder.ChecklistId = checklist.Id;
        }

        await _context.SaveChangesAsync();

        var checklistDto = _mapper.Map<VehicleCheckListDto>(checklist);
        return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklistDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCheckListDto updateCheckListDto)
    {
        var checklist = await _context.VehicleCheckLists.FindAsync(id);
        if (checklist is null) return NotFound();

        _mapper.Map(updateCheckListDto, checklist);
        await _context.SaveChangesAsync();

        var checklistDto = _mapper.Map<VehicleCheckListDto>(checklist);
        return Ok(checklistDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageOrders")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var checklist = await _context.VehicleCheckLists.FindAsync(id);
        if (checklist is null) return NotFound();

        var serviceOrder = await _context.ServiceOrders.FindAsync(checklist.ServiceOrderId);
        if (serviceOrder is not null)
        {
            serviceOrder.HasChecklist = false;
            serviceOrder.ChecklistId = null;
        }

        _context.VehicleCheckLists.Remove(checklist);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}