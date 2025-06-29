using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int OrederNumber { get; set; }

        public string Status { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department department { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User user {  get; set; }
        [ForeignKey("product")]
        public int productId { get; set; }
        public Product product { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime UpdateAt { get; set; }

    }
}
