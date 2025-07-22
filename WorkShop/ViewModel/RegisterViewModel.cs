using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WorkShop.ViewModel
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "اسم مطلوب")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "رقم الهاتف يجب أن يكون 10 أرقام")]
        public string PhoneNumber { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        public DateTime birthDay { get; set; }

        // الأقسام المختارة في النموذج (ID فقط)
        public List<int> SelectedDepartmentIds { get; set; }

        // قائمة الأقسام لتعبئة الـ DropdownList أو Checkbox في الـ View
        public List<SelectListItem>? Departments { get; set; }

        public IFormFile? Image { get; set; }
        public string? imagePath { get; set; }
    }
}
