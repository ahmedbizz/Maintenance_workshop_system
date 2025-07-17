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

        public UserController(IUnitOfWork unitOfWork,UserManager<User> userManager, IHostingEnvironment hostingEnvironment)
        {
            //_repository = repository;
            //_repDept = repDept;

            _unitOfWork = unitOfWork;
            _environment = hostingEnvironment;
            _userManager = userManager;
        }


        //private readonly IRepository<User> _repository;
        //private readonly IRepository<Department> _repDept;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostingEnvironment _environment;

        public IActionResult Index(string searchTerm, int page = 1)
        {

            var pageSize = 10;

            var query = string.IsNullOrEmpty(searchTerm) ?
                _unitOfWork.users.FindAll("UserDepartments.Department") :
                _unitOfWork.users.SearchBycondition(u => u.FullName.Contains(searchTerm) ||
                u.Email.Contains(searchTerm), "UserDepartments.Department");
            int totalItems = query.Count();

            var users = query.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new UserViewModel
            {
                users = users,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)

            };
            return View(viewModel);
        }
        [HttpGet]
        public IActionResult Create_user_Edite(string? Id)
        {
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if ( Id == null)
            {
                return View();
            }
            else
            {
                var user = _unitOfWork.users.FindById(Id);
                if (user == null)
                {
                    return NotFound();
                }
                else
                {

                    return View(user);
                }
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create_user_Edite(User user)
        {
            ViewBag.Departments = new MultiSelectList(_unitOfWork.departments.FindAll(), "Id", "Name", user.SelectedDepartmentIds);

            var currentUser = await _userManager.GetUserAsync(User);
            if (!ModelState.IsValid)
                return View(user);

            string unigName = null;

            // حفظ الصورة إذا كانت موجودة
            if (user.clientFile != null && user.clientFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                unigName = Guid.NewGuid().ToString() + Path.GetExtension(user.clientFile.FileName);
                string fullPath = Path.Combine(uploadFolder, unigName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await user.clientFile.CopyToAsync(stream);
                }
            }

            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                // ✅ إضافة مستخدم جديد
                user.UserName = user.Email;
                user.Email = user.Email;
                user.NormalizedEmail = user.Email.ToUpper();
                user.NormalizedUserName = user.Email.ToUpper();
                user.CreateAt = DateTime.Now;
                user.UpdateAt = DateTime.Now;

                if (unigName != null)
                    user.imagePath = unigName;

                // ✅ إنشاء علاقات الأقسام للمستخدم الجديد
                user.UserDepartments = user.SelectedDepartmentIds
                    .Select(depId => new UserDepartment { DepartmentId = depId, UserId = user.Id })
                    .ToList();

                var result = await _userManager.CreateAsync(user, "P@ssw0rd");
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(user);
                }

                await _unitOfWork.CompleteAsync(); // فقط مرة واحدة تكفي

                return RedirectToAction("Index");
            }
            else
            {
                // ✅ تعديل مستخدم موجود
                existingUser.Email = user.Email;
                existingUser.UserName = user.Email;
                existingUser.NormalizedEmail = user.Email.ToUpper();
                existingUser.NormalizedUserName = user.Email.ToUpper();
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.FullName = user.FullName;
                existingUser.birthDay = user.birthDay;
                existingUser.UpdateAt = DateTime.Now;

                if (unigName != null)
                    existingUser.imagePath = unigName;

                var result = await _userManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(user);
                }

                // ✅ تحديث الأقسام (حذف القديم + إضافة الجديد)
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

                await _unitOfWork.CompleteAsync();

                return User.IsInRole(Roles.Admin)
                    ? RedirectToAction("Index", "User")
                    : RedirectToAction("Index", "Home");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete_user(string? Id)
        {
            try
            {
                var existUser = await _userManager.FindByIdAsync(Id);
                if (existUser == null)
                {
                    return NotFound();
                }
                else
                {
                    var result = await _userManager.DeleteAsync(existUser);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                      
                        return View("Index");
                    }
                    return RedirectToAction("Index");
                }
            }
            catch (DbUpdateException dbEx)
            {
                // فحص إذا كان الخطأ بسبب ارتباط المستخدم ببيانات أخرى
                if (dbEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                {
                    TempData["DeleteError"] = "The user cannot be deleted because it is associated with other data..";
                }
                else
                {
                    TempData["DeleteError"] = "An error occurred during the deletion process.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex){
                TempData["DeleteError"] = $"An error occurred during the deletion process. {ex.Message}";
                return View("Index");
            }

        }





    }
}
