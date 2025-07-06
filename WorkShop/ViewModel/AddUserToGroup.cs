namespace WorkShop.ViewModel
{
    public class AddUserToGroup
    {
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string UserId { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<string> UserNames { get; set; } = new List<string>();
    }
}
