
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Services.MainService
{
    public class LogService : ILogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(int deviceId, string action, string description, string status, string notes, string role, string userId)
        {
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

        }
    }
}
