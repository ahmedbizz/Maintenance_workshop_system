using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WorkShop.Enums;

namespace WorkShop.ViewModel
{
    public class DeviceInputViewModel
    {
        [Required(ErrorMessage = "الرقم التسلسلي مطلوب")]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "مكان قدوم الجهاز مطلوب")]
        public string FromLocation { get; set; }

        [Required(ErrorMessage = "تاريخ العطل مطلوب")]
        [DataType(DataType.Date)]
        public DateTime FaultDate { get; set; } = DateTime.Now;

      
        [Required(ErrorMessage = "اختيار المهندس مطلوب")]
        public string EngineerId { get; set; }
        public IEnumerable<SelectListItem>? Engineers { get; set; }

        [Required(ErrorMessage = "اختيار القسم القادم منه الجهاز مطلوب")]
        public int ComingFromDepartmentId { get; set; }
        public IEnumerable<SelectListItem>? ComingFromDepartments { get; set; }

        [Required(ErrorMessage = "اختيار القسم المختص مطلوب")]
        public int DepartmentId { get; set; }
        public IEnumerable<SelectListItem> ?Departments { get; set; }

        [Required(ErrorMessage = "اختيار الجهاز مطلوب")]
        public int ProductId { get; set; }
        public IEnumerable<SelectListItem>? Products { get; set; }

        // حالة الجهاز - يمكن تعيينها تلقائياً أو يدوياً حسب الحاجة
        public string Status { get; set; } = MaintenanceStatus.New.ToString();
        // جديد: قائمة الأعطال السابقة
        public IEnumerable<SelectListItem>? ErrorKeywords { get; set; }
        public string FaultDescription { get; set; }
        public string? ErrorKeyword { get; set; }
    }
}
