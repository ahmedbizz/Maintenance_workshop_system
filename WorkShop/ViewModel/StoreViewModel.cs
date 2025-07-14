using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class StoreViewModel
    {
        public List<Store> stors { get; set; }
        public string searchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
