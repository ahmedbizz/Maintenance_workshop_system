using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class SparePartItem
    {

        public int Id { get; set; }
        public int RequestId { get; set; }
        [ForeignKey("RequestId")]
        public SparePartRequest Request { get; set; }

        public int? StoreId { get; set; } // المستودع الذي ستُصرف منه القطعة
        [ForeignKey("StoreId")]
        public Store? Store { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public int Quantity { get; set; }
    }
}
