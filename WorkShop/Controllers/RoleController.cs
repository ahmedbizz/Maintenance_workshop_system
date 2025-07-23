using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Admin)]
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
        [Authorize(Roles = Roles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(IdentityRole model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError("Name", "Role name is required.");
                    return PartialView("_CreateRole", model); // Return modal content
                }

                var roleExist = await _roleManager.RoleExistsAsync(model.Name);
                if (roleExist)
                {
                    ModelState.AddModelError("Name", "Role already exists.");
                    return PartialView("_CreateRole", model); // Show error in modal
                }

                var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Error creating role.");
                    return PartialView("_CreateRole", model);
                }
                TempData["Success"] = "New Role Created Successfully";
                return RedirectToAction("ListRole");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("ListRole");
            }
        }


        [HttpGet]
        [Authorize(Roles = Roles.Admin)]

        public IActionResult ListRole() {

            var Roles = _roleManager.Roles;
            return View(Roles);
        
        }//end

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]

        public IActionResult ListUsers()
        {

            var Users = _userManager.Users;
            return View(Users);

        }//end

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]

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
        [Authorize(Roles = Roles.Admin)]

        public async Task<IActionResult> MangamentUserRole(List<UserRoleViewModel> model,string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("ListUsers");
                }

                var reols = await _userManager.GetRolesAsync(user);

                await _userManager.RemoveFromRolesAsync(user, reols);
                var SelectedRole = model.Where(r => r.IsSelected).Select(r => r.RoleName);
         
                await _userManager.AddToRolesAsync(user, SelectedRole);


                TempData["Success"] = $"Role Add Successfully";
                return RedirectToAction("ListUsers");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("ListUsers");
            }

        }//end



    }
}
