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
public class SettingsController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public SettingsController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new WorkshopSettings { Id = Guid.NewGuid() };
            _context.WorkshopSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(_mapper.Map<WorkshopSettingsDto>(settings));
    }

    [HttpPut]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Update([FromBody] UpdateWorkshopSettingsDto request)
    {
        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new WorkshopSettings { Id = Guid.NewGuid() };
            _context.WorkshopSettings.Add(settings);
        }

        _mapper.Map(request, settings);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<WorkshopSettingsDto>(settings));
    }
}
