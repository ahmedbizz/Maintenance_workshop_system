using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class DeviceLogs
    {

        public int Id { get; set; }

        public int DeviceId { get; set; }
        [ForeignKey("DeviceId")]
        public Device Device { get; set; }
        public string Action { get; set; }
        public string Notes { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Role { get; set; }
        public string userId { get; set; }
        [ForeignKey("userId")]
        public User? user { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
