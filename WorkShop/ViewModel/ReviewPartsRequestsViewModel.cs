using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class ReviewPartsRequestsViewModel
    {
        public List<Device> devices { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;


    }
}
