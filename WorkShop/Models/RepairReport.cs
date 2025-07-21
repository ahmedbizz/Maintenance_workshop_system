namespace WorkShop.Models
{
    public class RepairReport
    {
        public int Id { get; set; }

        // ربط الجهاز
        public int DeviceId { get; set; }
        public Device Device { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // الكلمة المفتاحية أو وصف العطل
        public string ErrorKeyword { get; set; }

        // وصف مفصل للعطل (اختياري)
        public string? ErrorDescription { get; set; }

        // الإصلاح المقترح أو الذي تم تنفيذه
        public string SuggestedFix { get; set; }

        // القطع التي تم استخدامها (يمكن أن تفصل بفاصلة أو جدول منفصل)
        public string? UsedParts { get; set; }

        // الفني الذي أجرى الإصلاح
        public string? TechnicianName { get; set; }

        // تاريخ الإصلاح
        public DateTime RepairedAt { get; set; }

        // هل تم حل المشكلة بنجاح؟
        public bool IsSuccessful { get; set; }
    }

}
