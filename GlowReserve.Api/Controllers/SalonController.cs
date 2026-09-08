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
public class SalonController : ControllerBase
{
    private readonly GlowReserveDbContext _context;

    public SalonController(GlowReserveDbContext context)
    {
        _context = context;
    }

    // GET: /api/salon
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        //  Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Salons
            .Include(s => s.Services)
            .Include(s => s.StaffMembers)
            .Where(s => s.IsActive && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.Name.Contains(search) || 
                s.Description.Contains(search) ||
                s.Address.Contains(search));
        }

        var totalCount = await query.CountAsync();
        
        // Add OrderBy for deterministic pagination
        var salons = await query
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SalonResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Address = s.Address,
                Phone = s.Phone,
                Email = s.Email,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                IsActive = s.IsActive,
                Services = s.Services.Select(service => new ServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Price = service.Price,
                    DurationMinutes = service.DurationMinutes
                }).ToList(),
                StaffMembers = s.StaffMembers.Select(staff => new StaffDto
                {
                    Id = staff.Id,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            items = salons,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    // GET: /api/salon/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var salon = await _context.Salons
            .Include(s => s.Services)
            .Include(s => s.StaffMembers)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted); // ✅ Removed unused Appointments Include

        if (salon == null)
            return NotFound();

        //  Return DTO, not entity
        return Ok(new SalonDetailResponseDto
        {
            Id = salon.Id,
            Name = salon.Name,
            Description = salon.Description,
            Address = salon.Address,
            Phone = salon.Phone,
            Email = salon.Email,
            OpeningTime = salon.OpeningTime,
            ClosingTime = salon.ClosingTime,
            IsActive = salon.IsActive,
            Services = salon.Services.Select(service => new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                Category = service.Category
            }).ToList(),
            StaffMembers = salon.StaffMembers.Select(staff => new StaffDto
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Bio = staff.Bio,
                ProfileImageUrl = staff.ProfileImageUrl
            }).ToList()
        });
    }

    // POST: /api/salon
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSalonRequest request)
    {
        var salon = new Salon
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            OwnerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "",
            IsActive = true
        };

        _context.Salons.Add(salon);
        await _context.SaveChangesAsync();

        //  Return DTO, not entity
        var response = new SalonResponseDto
        {
            Id = salon.Id,
            Name = salon.Name,
            Description = salon.Description,
            Address = salon.Address,
            Phone = salon.Phone,
            Email = salon.Email,
            OpeningTime = salon.OpeningTime,
            ClosingTime = salon.ClosingTime,
            IsActive = salon.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = salon.Id }, response);
    }

    // PUT: /api/salon/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSalonRequest request)
    {
        var salon = await _context.Salons.FindAsync(id);
        if (salon == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (salon.OwnerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        salon.Name = request.Name;
        salon.Description = request.Description;
        salon.Address = request.Address;
        salon.Phone = request.Phone;
        salon.Email = request.Email;
        salon.OpeningTime = request.OpeningTime;
        salon.ClosingTime = request.ClosingTime;
        salon.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: /api/salon/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var salon = await _context.Salons.FindAsync(id);
        if (salon == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (salon.OwnerId != userId && !User.IsInRole("Admin"))
            return Forbid();

        // Soft delete
        salon.IsDeleted = true;
        salon.IsActive = false;
        salon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}