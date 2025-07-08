using WorkShop.Models;

namespace WorkShop.Services
{
    public interface INotificationService
    {
        Task NotifyUsersAsync(List<User> receivers, string title, string message, int deviceId);
        Task NotifyUsersAsync(string receivers, string title, string message, int deviceId);
    }

}
