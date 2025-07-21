namespace WorkShop.Models
{
    public class FixSuggestion
    {
        public int Id { get; set; }

        public int DeviceId { get; set; } // FK
        public string ErrorKeyword { get; set; }
        public string SuggestedFix { get; set; }
        public string SuggestedSpareParts { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        public Device Device { get; set; }
    }

}
