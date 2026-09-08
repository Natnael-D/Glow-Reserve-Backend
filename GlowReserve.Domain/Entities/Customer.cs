namespace GlowReserve.Domain.Entities;

public class Customer : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public int? PreferredSalonId { get; set; }
    
    // Navigation properties
    public Salon? PreferredSalon { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}