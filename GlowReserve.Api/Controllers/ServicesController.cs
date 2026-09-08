using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlowReserve.Application.DTOs;
using GlowReserve.Domain.Entities;
using GlowReserve.Infrastructure.Persistence;

namespace GlowReserve.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly GlowReserveDbContext _context;

    public ServicesController(GlowReserveDbContext context)
    {
        _context = context;
    }

    // GET: /api/services
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? salonId = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Services
            .Include(s => s.Salon)
            .Where(s => s.IsActive && !s.IsDeleted);

        if (salonId.HasValue)
            query = query.Where(s => s.SalonId == salonId.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        var totalCount = await query.CountAsync();

        var services = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                Category = s.Category
            })
            .ToListAsync();

        return Ok(new
        {
            items = services,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    // POST: /api/services
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var salon = await _context.Salons.FindAsync(request.SalonId);
        if (salon == null)
            return NotFound("Salon not found");

        if (salon.OwnerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var service = new Service
        {
            SalonId = request.SalonId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            Category = request.Category,
            IsActive = true
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            Category = service.Category
        });
    }

    // GET: /api/services/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var service = await _context.Services
            .Include(s => s.Salon)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (service == null)
            return NotFound();

        return Ok(new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            Category = service.Category
        });
    }
}