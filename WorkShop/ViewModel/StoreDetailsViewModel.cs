using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class StoreDetailsViewModel
    {
        public string store { get; set; }
        public List<ProductStock> products { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
