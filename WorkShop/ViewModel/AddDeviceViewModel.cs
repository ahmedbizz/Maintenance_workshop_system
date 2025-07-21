using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class AddDeviceViewModel
    {


        public string SerialNumber { get; set; }

        public string FromLocation { get; set; }

        public DateTime FaultDate { get; set; } = DateTime.Now;
        public string? FaultDescription { get; set; } = "null";

        public int productId { get; set; }
        public string TechnicianId { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        public IEnumerable<SelectListItem>? Products { get; set; }
        public IEnumerable<SelectListItem>? Departments { get; set; }

        public IEnumerable<SelectListItem>? Technicians { get; set; }

        // جديد: قائمة الأعطال السابقة
        public IEnumerable<SelectListItem> ErrorSuggestions { get; set; }

        // جديد: لو أردت تعبئة الحقول لاحقًا
        public string SelectedErrorKeyword { get; set; }
        public string SuggestedFix { get; set; }
    }
}
