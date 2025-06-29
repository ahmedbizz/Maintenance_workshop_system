using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WorkShop.ViewModel
{
    public class SparePartRequestViewModel
    {
        [Required]
        public int DeviceId { get; set; }

        public string? DeviceSerialNumber { get; set; } // لعرض السيريال في الفورم فقط
        public string Status { get; set; }
        public List<SelectListItem> Products { get; set; } = new List<SelectListItem>();

        public List<SparePartItemViewModel> Items { get; set; } = new List<SparePartItemViewModel>();

        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}
