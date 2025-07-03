namespace WorkShop.ViewModel
{
    public class AddUserToGroup
    {
        public string UserId { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<string> UserNames { get; set; } = new List<string>();
    }
}
