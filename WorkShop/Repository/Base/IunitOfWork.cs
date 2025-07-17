using Microsoft.CodeAnalysis.Differencing;
using WorkShop.Models;

namespace WorkShop.Repository.Base
{
    public interface IUnitOfWork : IDisposable
    {

        IRepository<User> users{get;}
        IRepository<Product> products{get;}
        IRepository<Device> devices { get; }
        IRepository<DeviceLogs> deviceLogs { get; }
        IRepository<MaintenanceCard> maintenanceCards { get; }
        IRepository<SparePartRequest> sparePartRequests { get; }
        IRepository<SparePartItem> sparePartItems { get; }
        IRepository<Order> orders{get;}
        IRepository<Department> departments{get;}
        IRepository<Store> stores{get;}
        IRepository<ProductStock> productStoks{get;}
        IRepository<Notification> notifications { get; }
        IRepository<UserDepartment> UserDepartments { get; }
        IRepository<Group> groups { get; }
        IRepository<UserGroup> userGroups { get; }
        IRepository<GroupRole> groupRoles { get; }

        int CommitChanges();

        Task CompleteAsync();

    }
}
