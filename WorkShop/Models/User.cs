using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class User : IdentityUser

    {

        [Required(ErrorMessage = "اسم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم يجب أن يكون بين 2 و100 حرف")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "اسم مطلوب")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "رقم الهاتف يجب أن يكون 10 أرقام")]
        public string PhoneNumber { get; set; }
        public DateTime birthDay { get; set; }
        public int EmployeeNumber { get; set; }
        public string? imagePath { get; set; }
        [NotMapped]
        public IFormFile? clientFile { get; set; }   

        public DateTime CreateAt { get; set; }

        public DateTime UpdateAt { get; set; }

        [Required(ErrorMessage = "Please select a department.")]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? department { get; set; }

        public ICollection<Order>? orders { get; set; }

    }
}
