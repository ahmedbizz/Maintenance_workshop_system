using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class UserViewModel
    {
        public List<User> users { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
