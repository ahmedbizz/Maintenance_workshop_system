using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Models;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    public class RoleController : Controller
    {



        public RoleController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager) {

            _roleManager = roleManager;
            _userManager = userManager;
        }
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;




        [HttpGet]
        public IActionResult CreateRole() { return PartialView("_CreateRole", new IdentityRole()); }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName) && !await _roleManager.RoleExistsAsync(roleName))
            {
               await _roleManager.CreateAsync(new IdentityRole(roleName));
                return Json(new { success = true });
            }
            ModelState.AddModelError("", "Name of Role  Exists !!");
            return PartialView("_CreateRole", new IdentityRole());
        }// end 

        [HttpGet]
        public IActionResult ListRole() {

            var Roles = _roleManager.Roles;
            return View(Roles);
        
        }//end

        [HttpGet]
        public IActionResult ListUsers()
        {

            var Users = _userManager.Users;
            return View(Users);

        }//end

        [HttpGet]
        public async Task<IActionResult> MangamentUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var model = new List<UserRoleViewModel>();
            var allRols = _roleManager.Roles.ToList();

            foreach (var role in allRols) {
                var userRole = new UserRoleViewModel
                {
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                };
                model.Add(userRole);
            }//forech
            ViewBag.UserId = userId;
            return View(model);

        }//end

        [HttpPost]
        public async Task<IActionResult> MangamentUserRole(List<UserRoleViewModel> model,string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var reols = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, reols);

            foreach (var role in model.Where(r => r.IsSelected))
            {
                await _userManager.AddToRoleAsync(user, role.RoleName);
                
            }

            return RedirectToAction("ListUsers");
        }//end



    }
}
