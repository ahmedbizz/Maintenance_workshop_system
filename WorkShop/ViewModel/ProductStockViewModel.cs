using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class ProductStockViewModel
    {
        public List<ProductStock> ProductStocks { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

    }
}
