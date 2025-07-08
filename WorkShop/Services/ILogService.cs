namespace WorkShop.Services
{
    public interface ILogService
    {
        Task LogAsync(
            int deviceId,
            string action,
            string description,
            string status,
            string notes,
            string role,
            string userId);


    }
}
