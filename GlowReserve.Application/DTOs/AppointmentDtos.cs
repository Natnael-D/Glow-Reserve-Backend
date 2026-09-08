using System.ComponentModel.DataAnnotations;

namespace GlowReserve.Application.DTOs;

public class CreateAppointmentRequest
{
    [Required]
    public int ServiceId { get; set; }

    public int? StaffId { get; set; }

    [Required]
    public DateTime AppointmentDateTime { get; set; }

    public string? Notes { get; set; }
}

public class UpdateStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // Pending, Confirmed, Completed, Cancelled, NoShow
}

public class AppointmentResponseDto
{
    public int Id { get; set; }
    public int SalonId { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? StaffId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ServiceDto? Service { get; set; }
    public StaffDto? Staff { get; set; }
    public CustomerDto? Customer { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}