using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Services.MainService
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }



        public async Task NotifyUsersAsync(List<User> receivers, string title, string message, int deviceId)
        {
            foreach (var user in receivers)
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    ReceiverId = user.Id,
                    DeviceId = deviceId,
                    CreatedAt = DateTime.Now
                };
                _unitOfWork.notifications.Insert(notification);
            }

         
        }

        public async Task NotifyUsersAsync(string receivers, string title, string message, int deviceId)
        {
         
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    ReceiverId = receivers,
                    DeviceId = deviceId,
                    CreatedAt = DateTime.Now
                };
                _unitOfWork.notifications.Insert(notification);
            
        }
    }
  
}
