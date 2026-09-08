namespace GlowReserve.Domain.Entities;

public class Salon : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Relationships
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}