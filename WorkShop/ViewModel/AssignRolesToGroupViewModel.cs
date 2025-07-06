namespace WorkShop.ViewModel
{
    public class AssignRolesToGroupViewModel
    {
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int GroupId { get; set; } // The ID of the group to which roles are being assigned
        public string? GroupName { get; set; }
        public List<string> RoleNames { get; set; } = new List<string>(); // List of selected role IDs

    }
}
