using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WorkShop.Models;

public class Device
{
    public int Id { get; set; }

    [Required]
    public string SerialNumber { get; set; }

    [Required]
    public string FromLocation { get; set; }
    [Required]
    public DateTime FaultDate { get; set; } = DateTime.Now;


    public string? TechnicianId { get; set; }
    public User? Technician { get; set; }
    [Required]
    public string EngineerId { get; set; }
    public User? Engineer { get; set; }


    [Required]
    public int ComingFromDepartmentId { get; set; }

    [Required]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    // العلاقة مع المنتج
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string FaultDescription { get; set; }
    public string Status { get; set; } // New, UnderRepair, WaitingParts, SentBack, etc.

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // علاقة One-to-One مع كرت الصيانة الحالي
    public MaintenanceCard MaintenanceCard { get; set; }

    // سجل الأحداث
    public ICollection<DeviceLogs> DeviceLogs { get; set; }
    public ICollection<SparePartRequest> SparePartRequests { get; set; }
}
