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
public class AppointmentController : ControllerBase
{
    private readonly GlowReserveDbContext _context;

    public AppointmentController(GlowReserveDbContext context)
    {
        _context = context;
    }

    // GET: /api/appointment
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.Staff)
            .Where(a => !a.IsDeleted);

        if (!User.IsInRole("Admin"))
        {
            var salons = await _context.Salons
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            query = query.Where(a => salons.Contains(a.SalonId));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var totalCount = await query.CountAsync();

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                SalonId = a.SalonId,
                CustomerId = a.CustomerId,
                ServiceId = a.ServiceId,
                StaffId = a.StaffId,
                AppointmentDateTime = a.AppointmentDateTime,
                Status = a.Status,
                TotalPrice = a.TotalPrice,
                DurationMinutes = a.DurationMinutes,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt,
                Service = a.Service != null ? new ServiceDto
                {
                    Id = a.Service.Id,
                    Name = a.Service.Name,
                    Price = a.Service.Price,
                    DurationMinutes = a.Service.DurationMinutes
                } : null,
                Staff = a.Staff != null ? new StaffDto
                {
                    Id = a.Staff.Id,
                    FirstName = a.Staff.FirstName,
                    LastName = a.Staff.LastName
                } : null,
                Customer = a.Customer != null ? new CustomerDto
                {
                    Id = a.Customer.Id,
                    FirstName = a.Customer.FirstName,
                    LastName = a.Customer.LastName,
                    Email = a.Customer.Email,
                    Phone = a.Customer.Phone
                } : null
            })
            .ToListAsync();

        return Ok(new
        {
            items = appointments,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    // POST: /api/appointment
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
            return BadRequest("Customer profile not found");

        var service = await _context.Services
            .Include(s => s.Salon)
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId);

        if (service == null)
            return BadRequest("Service not found");

        //  Validate staff belongs to the salon
        if (request.StaffId.HasValue)
        {
            var staff = await _context.StaffMembers
                .FirstOrDefaultAsync(s => s.Id == request.StaffId.Value && s.SalonId == service.SalonId);
            
            if (staff == null)
                return BadRequest("Invalid staff member for this salon");
        }

        var appointment = new Appointment
        {
            SalonId = service.SalonId,
            CustomerId = customer.Id,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            AppointmentDateTime = request.AppointmentDateTime,
            Status = "Pending",
            TotalPrice = service.Price,
            DurationMinutes = service.DurationMinutes,
            Notes = request.Notes
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        //  Return DTO
        var response = new AppointmentResponseDto
        {
            Id = appointment.Id,
            SalonId = appointment.SalonId,
            CustomerId = appointment.CustomerId,
            ServiceId = appointment.ServiceId,
            StaffId = appointment.StaffId,
            AppointmentDateTime = appointment.AppointmentDateTime,
            Status = appointment.Status,
            TotalPrice = appointment.TotalPrice,
            DurationMinutes = appointment.DurationMinutes,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, response);
    }

    // GET: /api/appointment/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Staff)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        //  Check ownership: user can only view their own appointments
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null || appointment.CustomerId != customer.Id)
            return Forbid();

        //  Return DTO, not entity (avoid circular references)
        return Ok(new AppointmentResponseDto
        {
            Id = appointment.Id,
            SalonId = appointment.SalonId,
            CustomerId = appointment.CustomerId,
            ServiceId = appointment.ServiceId,
            StaffId = appointment.StaffId,
            AppointmentDateTime = appointment.AppointmentDateTime,
            Status = appointment.Status,
            TotalPrice = appointment.TotalPrice,
            DurationMinutes = appointment.DurationMinutes,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt,
            Service = appointment.Service != null ? new ServiceDto
            {
                Id = appointment.Service.Id,
                Name = appointment.Service.Name,
                Price = appointment.Service.Price,
                DurationMinutes = appointment.Service.DurationMinutes
            } : null,
            Staff = appointment.Staff != null ? new StaffDto
            {
                Id = appointment.Staff.Id,
                FirstName = appointment.Staff.FirstName,
                LastName = appointment.Staff.LastName
            } : null,
            Customer = appointment.Customer != null ? new CustomerDto
            {
                Id = appointment.Customer.Id,
                FirstName = appointment.Customer.FirstName,
                LastName = appointment.Customer.LastName,
                Email = appointment.Customer.Email,
                Phone = appointment.Customer.Phone
            } : null
        });
    }

    // PUT: /api/appointment/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        // Only salon owner or admin can update status
        var salon = await _context.Salons
            .FirstOrDefaultAsync(s => s.Id == appointment.SalonId);

        if (salon == null)
            return NotFound();

        var isOwner = salon.OwnerId == userId;
        var isAdmin = User.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Forbid();

        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated successfully", status = appointment.Status });
    }

    // DELETE: /api/appointment/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        // Only customer who owns the appointment can cancel
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null || appointment.CustomerId != customer.Id)
        {
            // Or allow salon owner/admin to cancel
            var salon = await _context.Salons
                .FirstOrDefaultAsync(s => s.Id == appointment.SalonId);

            if (salon == null || salon.OwnerId != userId)
            {
                if (!User.IsInRole("Admin"))
                    return Forbid();
            }
        }

        appointment.Status = "Cancelled";
        appointment.CancellationReason = "User requested cancellation";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: /api/appointment/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
            return Ok(new List<AppointmentResponseDto>());

        var appointments = await _context.Appointments
            .Where(a => a.CustomerId == customer.Id && !a.IsDeleted)
            .OrderByDescending(a => a.AppointmentDateTime)
            .Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                SalonId = a.SalonId,
                CustomerId = a.CustomerId,
                ServiceId = a.ServiceId,
                StaffId = a.StaffId,
                AppointmentDateTime = a.AppointmentDateTime,
                Status = a.Status,
                TotalPrice = a.TotalPrice,
                DurationMinutes = a.DurationMinutes,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(appointments);
    }
}