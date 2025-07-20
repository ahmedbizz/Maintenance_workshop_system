using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkShop.ViewModel
{
    public class DevicesViewModel
    {
        public List<Device> devices { get; set; }
        public string searchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public List<SelectListItem> Technicians { get; set; }
    }
}
