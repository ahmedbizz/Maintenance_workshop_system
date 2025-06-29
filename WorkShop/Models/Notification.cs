using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string? Title { get; set; }
        public int DeviceId { get; set; }
        public string? Message { get; set; }

        public string? ReceiverId { get; set; }  // المستخدم المستلم (مثل المدير)

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // علاقة مع المستخدم
        public User? Receiver { get; set; }
    }
}
