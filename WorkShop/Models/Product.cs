using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
        public string Desc { get; set; }
        [Required(ErrorMessage = "الرقم التسلسي  مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الرقم التسلسي  يجب أن يكون بين 3 و 100 حرف")]
        public string PartNumber { get; set; }

        public string? imagePath { get; set;}
        [NotMapped]
        public IFormFile clientFile { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public DateTime UpdateAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Please select a department.")]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? department { get; set; }

        public ICollection<Order>? orders { get; set; }

        public ICollection<ProductStock>? productStocks { get; set; }

        public ICollection<DeviceLogs>? deviceLogs { get; set; }

    }
}
