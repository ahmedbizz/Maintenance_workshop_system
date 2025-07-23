using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class MaintenanceCard
    {
        public int Id { get; set; }

        public int DeviceId { get; set; }
        [ForeignKey("DeviceId")]
        public Device Device { get; set; }
        public string? EngineerId { get; set; }
        [ForeignKey("EngineerId")]
        public User? Engineer { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; } // AwaitingTechnician, WaitingSparePart, InReview, Done
        public string? TechnicianReport { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime ReceivedAt { get; set; } = DateTime.Now;            // وقت الاستلام
        public DateTime? AssignedToTechnicianAt { get; set; } = DateTime.Now; // وقت التحويل للفني
        public DateTime? SparePartsRequestedAt { get; set; } = DateTime.Now; // وقت طلب قطع الغيار
        public DateTime? ApprovedByEngineerAt { get; set; } = DateTime.Now;  // وقت الموافقة من المهندس
        public DateTime? RepairedAt { get; set; } = DateTime.Now;           // وقت انتهاء الفني من الإصلاح
        public DateTime? ClosedAt { get; set; } = DateTime.Now;           // وقت الإغلاق النهائي


    }
}
