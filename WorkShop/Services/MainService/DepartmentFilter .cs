
using System.Net.NetworkInformation;

namespace WorkShop.Services.MainService
{
    public class DepartmentFilter : IDeviceFilter
    {
        private readonly int? _DepartmentId;

        public DepartmentFilter(int? DepartmentId)
        {
            _DepartmentId =DepartmentId;
        }


        public IQueryable<Device> Apply(IQueryable<Device> devices)
        {
            return _DepartmentId.HasValue
                ? devices.Where(d => d.DepartmentId == _DepartmentId)
                : devices;
        }
    }
}
