
using Microsoft.Build.Framework;

namespace WorkShop.Services.MainService
{
    public class StatusFilter : IDeviceFilter
    {


        private readonly string _Status;

        public StatusFilter(string Status) {
            _Status = Status;
        }


        public IQueryable<Device> Apply(IQueryable<Device> devices)
        {
            return string.IsNullOrEmpty(_Status)
                ? devices
                : devices.Where(d => d.Status == _Status);
        }
    }
}
