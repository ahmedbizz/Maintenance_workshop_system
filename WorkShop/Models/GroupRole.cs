using Microsoft.AspNetCore.Identity;

namespace WorkShop.Models
{
    public class GroupRole
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }

        public string RoleId { get; set; } // e.g., "Admin", "Member", etc.
        
        public IdentityRole Role { get; set; } // Navigation property to IdentityRole
    }
}
