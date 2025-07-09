using WorkShop.Models;
using WorkShop.Repository;
using WorkShop.Repository.Base;

namespace WorkShop.Services.MainService
{
    public class NotificationService : INotificationService
    {
        
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationService( IServiceScopeFactory scopeFactory)
        {
       
            _scopeFactory = scopeFactory;
        }



        public async Task NotifyUsersAsync(List<User> receivers, string title, string message, int deviceId)
        {

            using var scope = _scopeFactory.CreateScope();
            var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
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
                await _unitOfWork.CompleteAsync();
            }

         
        }

        public async Task NotifyUsersAsync(string receivers, string title, string message, int deviceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    ReceiverId = receivers,
                    DeviceId = deviceId,
                    CreatedAt = DateTime.Now
                };
                _unitOfWork.notifications.Insert(notification);
                await _unitOfWork.CompleteAsync();

        }
    }
  
}
