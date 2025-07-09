
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Services.MainService
{
    public class LogService : ILogService
    {

        private readonly IServiceScopeFactory _scopeFactory;

        public LogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task LogAsync(int deviceId, string action, string description, string status, string notes, string role, string userId)
        {

           using var scope = _scopeFactory.CreateScope();
            var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var log = new DeviceLogs
            {
                DeviceId = deviceId,
                Action = action,
                description = description,
                status = status,
                Notes = notes,
                Role = role,
                userId = userId,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.deviceLogs.AddAsync(log);
            await _unitOfWork.CompleteAsync();

        }
    }
}
