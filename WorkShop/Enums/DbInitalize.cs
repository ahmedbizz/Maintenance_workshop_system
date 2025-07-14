using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.Extensions.DependencyInjection;
using System.Security.AccessControl;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Enums
{
    public static class DbInitalize
    {


        public static async Task IniatialDatabase(IServiceProvider service)
        {

            using var scop = service.CreateScope();

            var _UserManager = scop.ServiceProvider.GetRequiredService<UserManager<User>>();
            var _RoleManager = scop.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var _unitOfWork = scop.ServiceProvider.GetRequiredService<IUnitOfWork>();

            //Check Role exist 


            if(! await _RoleManager.RoleExistsAsync(Roles.Admin)){
                await _RoleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            //Check Department exist

            var department = _unitOfWork.departments.FindAll().FirstOrDefault(d => d.Name == "Managment");
            if(department == null)
            {
                var NewDepartment = new Department
                {
                    Name = "Managment"
                };
                await _unitOfWork.departments.AddAsync(NewDepartment);
                await _unitOfWork.CompleteAsync();
            }

            //Check User exist 

            var Admin = await _UserManager.FindByEmailAsync("Admin@Admin.com");
            if(Admin == null)
            {
                var NewAdmin = new User
                {
                    FullName = "Administrator",
                    UserName = "Admin@Admin.com",
                    Email = "Admin@Admin.com",
                    EmailConfirmed = true
                };

               var result = await _UserManager.CreateAsync(NewAdmin, "P@ssw0rd");
                if (result.Succeeded)
                {
                    await _UserManager.AddToRoleAsync(NewAdmin,Roles.Admin);
                }
                else
                {
                    throw new Exception("❌ فشل في إنشاء المستخدم Admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

        }
    }
}
