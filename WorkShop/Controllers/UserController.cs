using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using WorkShop.Context;
using WorkShop.Models;
using WorkShop.Repository.Base;
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

        public IActionResult Index()
        {
            return View(_unitOfWork.users.FindAll("department").ToList());
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
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");

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
                await user.clientFile.CopyToAsync(new FileStream(fullPath, FileMode.Create));
            }
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                // إضافة مستخدم جديد
                user.UserName = user.Email;
                user.Email = user.Email;
                user.NormalizedEmail = user.Email.ToUpper();
                user.NormalizedUserName = user.Email.ToUpper();
                user.CreateAt = DateTime.Now;
                user.UpdateAt = DateTime.Now;
                if (unigName != null)
                    user.imagePath = unigName;

                var result = await _userManager.CreateAsync(user, "P@ssw0rd");
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(user);
                }

                return RedirectToAction("Index");
            }
            else
            {
                // تعديل مستخدم موجود
                var existUser = await _userManager.FindByIdAsync(user.Id);
                if (existUser == null) return NotFound();

                existUser.Email = user.Email;
                existUser.UserName = user.Email;
                existUser.NormalizedEmail = user.Email.ToUpper();
                existUser.NormalizedUserName = user.Email.ToUpper();
                existUser.PhoneNumber = user.PhoneNumber;
                existUser.FullName = user.FullName;
                existUser.DepartmentId = user.DepartmentId;
                existUser.birthDay = user.birthDay;
                existUser.UpdateAt = DateTime.Now;
                if (unigName != null)
                    existUser.imagePath = unigName;

                var result = await _userManager.UpdateAsync(existUser);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(user);
                }

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete_user(string? Id)
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
    }
}
