using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class Store
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "اسم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم يجب أن يكون بين 2 و100 حرف")]
        public string Name { get; set; }
        [Required(ErrorMessage = "اسم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم يجب أن يكون بين 2 و100 حرف")]
        public string Location { get; set; }
        [Required(ErrorMessage = "Please select a department.")]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? department { get; set; }
        public ICollection<ProductStock>? productStocks { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime UpdateAt { get; set; }

    }
}
