using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class SparePartRequest
    {
        public int Id { get; set; }


        public int DeviceId { get; set; }  // المفتاح الأجنبي للجهاز
        [ForeignKey("DeviceId")]
        public Device Device { get; set; }

        public DateTime RequestDate { get; set; }

        public string RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public User? RequestedBy { get; set; }

        public string Status { get; set; } // "قيد الانتظار", "موافق عليه", "مرفوض"
        public bool IsFinalized { get; set; }
        public string? ManagerId { get; set; } // المدير الذي يوافق
        [ForeignKey("ManagerId")]
        public User? Manager { get; set; }

        public ICollection<SparePartItem> Items { get; set; }
    }
}
