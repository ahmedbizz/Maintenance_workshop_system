using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkShop.Models;

namespace WorkShop.Context
{
    public class AppDbContext : IdentityDbContext<User>
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {  
        }

        public DbSet<Product> products { get; set; }
        public DbSet<Order> orders { get;   set; }
        public DbSet<Store> stores { get; set; }
        public DbSet<Department> departments { get; set; }
        public DbSet<Device> device { get; set; }
        public DbSet<MaintenanceCard> maintenanceCards { get; set; }
        public DbSet<DeviceLogs> deviceLogs { get; set; }
        public DbSet<SparePartRequest> sparePartRequests { get; set; }
        public DbSet<Notification> notifications { get; set; }
        public DbSet<Group> groups { get; set; }
        public DbSet<UserGroup> userGroups { get; set; }
        public DbSet<GroupRole> groupRoles { get; set; }
        public DbSet<UserDepartment> UserDepartments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // One-to-One بين Device و MaintenanceCard
            modelBuilder.Entity<Device>()
                .HasOne(d => d.MaintenanceCard)
                .WithOne(m => m.Device)
                .HasForeignKey<MaintenanceCard>(m => m.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserDepartment>()
                 .HasKey(ud => new { ud.UserId, ud.DepartmentId });

            modelBuilder.Entity<UserDepartment>()
                .HasOne(ud => ud.User)
                .WithMany(u => u.UserDepartments)
                .HasForeignKey(ud => ud.UserId);

            modelBuilder.Entity<UserDepartment>()
                .HasOne(ud => ud.Department)
                .WithMany(d => d.UserDepartments)
                .HasForeignKey(ud => ud.DepartmentId);

            foreach (var item in modelBuilder.Model.GetEntityTypes()
                .SelectMany( m => m .GetForeignKeys()))
            {
                item.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<ProductStock>()
            .HasKey(ps => new { ps.productId, ps.storeId });


            modelBuilder.HasSequence<int>("Sequen-Emp-Id").StartsAt(101).IncrementsBy(1);
            modelBuilder.Entity<User>().Property(x => x.EmployeeNumber).HasDefaultValueSql(
                "NEXT VALUE FOR [Sequen-Emp-Id]");

            modelBuilder.HasSequence<int>("Sequen-Oreder").StartsAt(1).IncrementsBy(1);
            modelBuilder.Entity<Order>().Property(x => x.OrederNumber).HasDefaultValueSql(
                "NEXT VALUE FOR [Sequen-Oreder]");

            modelBuilder.Entity<Department>().Property(x => x.CreateAt).HasDefaultValueSql("GETDATE()");
        }
    }
}
