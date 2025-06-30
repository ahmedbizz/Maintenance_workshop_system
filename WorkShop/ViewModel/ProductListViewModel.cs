using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
