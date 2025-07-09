using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class PartsDeviceDetailsViewModel
    {


        public int DeviceId { get; set; }
        public string? ProductName { get; set; }
        public string? SerialNumber { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime FaultDate { get; set; }
        public string TechnicianName { get; set; }
        public string? TechnicianReport { get; set; } = "";
        public bool RequestSpareParts { get; set; }
        public bool IsRepaired { get; set; }
        public string DeviceStatus { get; set; } = "";

        public ICollection<SparePartRequest> SparePartRequests { get; set; }
        public SparePartRequestViewModel SparePartRequest { get; set; } = new();
        public List<SelectListItem> Products { get; set; } = new();

    }
}
