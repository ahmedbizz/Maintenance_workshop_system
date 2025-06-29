
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class ProductStock
    {

        [Required(ErrorMessage = "Please select a Product.")]
        public int productId { get; set; }
        [ForeignKey("productId")]
        public Product? product { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "الكمية لا يمكن أن تكون سالبة")]
        public int quantity { get; set; }

        [Required(ErrorMessage = "Please select a Store.")]
        public int storeId { get; set; }
        [ForeignKey("storeId")]
        public Store? store { get; set; }
   
    }
}
