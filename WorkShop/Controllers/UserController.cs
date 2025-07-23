using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WorkShop.Context;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace WorkShop.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostingEnvironment _environment;

        public UserController(IUnitOfWork unitOfWork, UserManager<User> userManager, IHostingEnvironment hostingEnvironment)
        {
            _unitOfWork = unitOfWork;
            _environment = hostingEnvironment;
            _userManager = userManager;
        }

        public IActionResult Index(string searchTerm, int page = 1)
        {
            try
            {
                const int pageSize = 10;

                var query = string.IsNullOrWhiteSpace(searchTerm) ?
                    _unitOfWork.users.FindAll("UserDepartments.Department") :
                    _unitOfWork.users.SearchBycondition(
                        u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm),
                        "UserDepartments.Department");

                int totalItems = query.Count();
                var users = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var viewModel = new UserViewModel
                {
                    users = users,
                    SearchTerm = searchTerm,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while loading users. {ex.Message}";
                return View(new UserViewModel());
            }
        }

        [HttpGet]
        public IActionResult Create_user_Edite(string? id)
        {
            try
            {
                ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");

                if (string.IsNullOrEmpty(id))
                    return View();

                var user = _unitOfWork.users.FindById(id);
                return user == null ? NotFound() : View(user);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while retrieving the user. {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create_user_Edite(User user)
        {
            ViewBag.Departments = new MultiSelectList(_unitOfWork.departments.FindAll(), "Id", "Name", user.SelectedDepartmentIds);

            if (!ModelState.IsValid)
                return View(user);

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                string unigName = null;
                if (user.clientFile != null && user.clientFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "images");
                    Directory.CreateDirectory(uploadFolder);

                    unigName = Guid.NewGuid() + Path.GetExtension(user.clientFile.FileName);
                    string fullPath = Path.Combine(uploadFolder, unigName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await user.clientFile.CopyToAsync(stream);
                }

                var existingUser = await _userManager.FindByIdAsync(user.Id);

                if (existingUser == null)
                {
                    user.UserName = user.Email;
                    user.NormalizedEmail = user.Email.ToUpper();
                    user.NormalizedUserName = user.Email.ToUpper();
                    user.CreateAt = DateTime.Now;
                    user.UpdateAt = DateTime.Now;
                    user.imagePath = unigName;

                    user.UserDepartments = user.SelectedDepartmentIds.Select(depId => new UserDepartment
                    {
                        DepartmentId = depId,
                        UserId = user.Id
                    }).ToList();

                    var result = await _userManager.CreateAsync(user, "P@ssw0rd");

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                        return View(user);
                    }
                    TempData["Success"] = $"Created Successfully";
                }
                else
                {
                    existingUser.Email = user.Email;
                    existingUser.UserName = user.Email;
                    existingUser.NormalizedEmail = user.Email.ToUpper();
                    existingUser.NormalizedUserName = user.Email.ToUpper();
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.FullName = user.FullName;
                    existingUser.birthDay = user.birthDay;
                    existingUser.UpdateAt = DateTime.Now;
                    existingUser.imagePath = unigName ?? existingUser.imagePath;

                    var result = await _userManager.UpdateAsync(existingUser);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                        return View(user);
                    }

                    var oldDeps = _unitOfWork.UserDepartments.FindAll().Where(ud => ud.UserId == existingUser.Id).ToList();
                    _unitOfWork.UserDepartments.DeleteList(oldDeps);

                    foreach (var depId in user.SelectedDepartmentIds.Distinct())
                    {
                        await _unitOfWork.UserDepartments.AddAsync(new UserDepartment
                        {
                            UserId = existingUser.Id,
                            DepartmentId = depId
                        });
                    }
                    TempData["Success"] = $"Update Successfully";
                }

                await _unitOfWork.CompleteAsync();

                return User.IsInRole(Roles.Admin) ? RedirectToAction("Index") : RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while saving user data. {ex.Message}";
                return View(user);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete_user(string? id)
        {
            try
            {
                var existUser = await _userManager.FindByIdAsync(id);
                if (existUser == null)
                    return NotFound();

                var result = await _userManager.DeleteAsync(existUser);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View("Index");
                }

                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                    TempData["DeleteError"] = "The user cannot be deleted because it is associated with other data.";
                else
                    TempData["DeleteError"] = "An error occurred during the deletion process.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["DeleteError"] = $"An error occurred during the deletion process. {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
