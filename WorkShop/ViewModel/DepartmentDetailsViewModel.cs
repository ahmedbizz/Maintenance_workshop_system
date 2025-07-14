using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class DepartmentDetailsViewModel
    {
        public Department Department { get; set; }
        public IEnumerable<User> Users { get; set; }
        public int TotalUsers { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
    }
}
