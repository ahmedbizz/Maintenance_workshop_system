using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WorkShop.ViewModel
{
    public class AddDeviceViewModel
    {


        public string SerialNumber { get; set; }

        public string FromLocation { get; set; }

        public DateTime FaultDate { get; set; }

        public int productId { get; set; }
        public string TechnicianId { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        public IEnumerable<SelectListItem>? Products { get; set; }
        public IEnumerable<SelectListItem>? Departments { get; set; }

        public IEnumerable<SelectListItem>? Technicians { get; set; }
    }
}
