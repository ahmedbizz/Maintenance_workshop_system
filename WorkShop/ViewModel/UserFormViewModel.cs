using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkShop.ViewModel
{
    public class UserFormViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        public List<int> SelectedDepartmentIds { get; set; }

        public List<SelectListItem> AllDepartments { get; set; }
    }
}
