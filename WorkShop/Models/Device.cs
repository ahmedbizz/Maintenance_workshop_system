using System.ComponentModel.DataAnnotations;
using WorkShop.Models;

public class Device
{
    public int Id { get; set; }

    [Required]
    public string SerialNumber { get; set; }

    [Required]
    public string FromLocation { get; set; }

    public DateTime FaultDate { get; set; }

    [Required]
    public string TechnicianId { get; set; }
    public User? Technician { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    // العلاقة مع المنتج
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string Status { get; set; } // New, UnderRepair, WaitingParts, SentBack, etc.

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // علاقة One-to-One مع كرت الصيانة الحالي
    public MaintenanceCard MaintenanceCard { get; set; }

    // سجل الأحداث
    public ICollection<DeviceLogs> DeviceLogs { get; set; }
    public ICollection<SparePartRequest> SparePartRequests { get; set; }
}
