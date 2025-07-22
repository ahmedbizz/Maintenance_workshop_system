using System.ComponentModel.DataAnnotations.Schema;

namespace WorkShop.Models
{
    public class UserDepartment
    {
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }
    }

}
