using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkShop.Models;

namespace WorkShop.ViewModel
{
    public class SparePartItemViewModel
    {
        public int Id { get; set; }
        [Required]

        public int? StoreId { get; set; } // المستودع الذي ستُصرف منه القطعة
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "يجب إدخال كمية صحيحة.")]
        public int Quantity { get; set; }

        public List<SelectListItem>? AvailableStores { get; set; }
    }
}
