namespace GlowReserve.Domain.Entities;

public class Appointment : BaseEntity
{
    public int SalonId { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? StaffId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled, NoShow
    public decimal TotalPrice { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    
    public Salon Salon { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Staff? Staff { get; set; }
}