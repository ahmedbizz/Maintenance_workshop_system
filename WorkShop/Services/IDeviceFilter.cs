namespace WorkShop.Services
{
    public interface IDeviceFilter
    {
        IQueryable<Device> Apply(IQueryable<Device> devices);
    }
}
