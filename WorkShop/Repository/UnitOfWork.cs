using Microsoft.EntityFrameworkCore;
using WorkShop.Context;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public UnitOfWork(AppDbContext context) {
        _Context = context;
            users = new MainRepository<User>(_Context);
            products = new MainRepository<Product>(_Context);
            orders = new MainRepository<Order>(_Context);
            departments = new MainRepository<Department>(_Context);
            stores = new MainRepository<Store>(_Context);
            productStoks = new MainRepository<ProductStock>(_Context);
            devices = new MainRepository<Device>(_Context);
            maintenanceCards = new MainRepository<MaintenanceCard>(_Context);
            notifications = new MainRepository<Notification>(_Context);
            deviceLogs = new MainRepository<DeviceLogs>(_Context);
            sparePartRequests = new MainRepository<SparePartRequest>(_Context);
            sparePartItems = new MainRepository<SparePartItem>(_Context);
            groups = new MainRepository<Group>(_Context);
            userGroups = new MainRepository<UserGroup>(_Context);
            groupRoles = new MainRepository<GroupRole>(_Context);
            UserDepartments = new MainRepository<UserDepartment>(_Context);
        }
        private readonly AppDbContext _Context;
        public IRepository<User> users { get; set; }

        public IRepository<Product> products { get; set; }

        public IRepository<Order> orders { get; set; }

        public IRepository<Department> departments { get; set; }

        public IRepository<Store> stores { get; set; }

        public IRepository<ProductStock> productStoks { get; set; }

        public IRepository<Device> devices { get; set; }

        public IRepository<DeviceLogs> deviceLogs { get; set; }
        public IRepository<MaintenanceCard> maintenanceCards { get; set; }

        public IRepository<Notification> notifications { get; set; }
       
        public IRepository<SparePartRequest> sparePartRequests { get; set; }
        public IRepository<SparePartItem> sparePartItems { get; private set; }

        public IRepository<Group> groups { get; private set; }

        public IRepository<UserGroup> userGroups { get; private set; }

        public IRepository<GroupRole> groupRoles { get; private set; }

        public IRepository<UserDepartment> UserDepartments { get; private set; }

        public int CommitChanges()
        {
            return _Context.SaveChanges();
        }
        public async Task CompleteAsync() => await _Context.SaveChangesAsync();
        public void Dispose()
        {
            _Context.Dispose();
        }
    }
}
