using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [RegularExpression(@"^[\u0621-\u064Aa-zA-Z\s]+$", ErrorMessage = "يسمح فقط بالحروف العربية والإنجليزية")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "الاسم يجب أن يكون بين 2 و100 حرف")]
        public string Name { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public DateTime UpdateAt { get; set; } = DateTime.Now;

        public ICollection<User> users { get; set; } = new List<User>();

        public ICollection<Order> orders { get; set; } = new List<Order>();

        public ICollection<Product> products { get; set; } = new List<Product>();

        public ICollection<Store> stors { get; set; } = new List<Store>();


    }
}
