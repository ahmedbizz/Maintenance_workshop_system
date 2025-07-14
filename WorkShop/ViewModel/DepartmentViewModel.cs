using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class DepartmentViewModel
    {
        public List<Department> departments { get; set; }
        public string searchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
