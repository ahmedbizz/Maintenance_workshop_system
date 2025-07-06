using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace WorkShop.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {

        public AccountController(IViewLocalizer localizer,IUnitOfWork unit ,IHostingEnvironment environment, UserManager<User> userManager, SignInManager<User> signInManager) {
            _signInManager = signInManager;
            _userManager = userManager;
            _environment = environment;
            _unitOfWork = unit;
            _localizer = localizer;
        }
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostingEnvironment _environment;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IViewLocalizer _localizer;
        //===============================Login========================
        [HttpGet]
        public IActionResult Login() { return View(); }


        [HttpPost]
  
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "User Do's not Exsist!!");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null) {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe,false);
                if (result.Succeeded) {

                    return RedirectToAction("Index","Home"); }
            }
            ModelState.AddModelError("", "Email Or Password Faild !!");
            return View(model);
        }
        //================================Register=============================
        [HttpGet]
        public IActionResult Register() {
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            return View(); }

        [HttpPost]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {

         
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                birthDay = model.birthDay,
                DepartmentId = model.DepartmentId,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };
            string filename = string.Empty;
            if (model.Image != null && model.Image.Length >0){

                    string Upload = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(Upload)) { Directory.CreateDirectory(Upload); }
                string unigName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
                string FullPath = Path.Combine(Upload, unigName);
                await  model.Image.CopyToAsync(new FileStream(FullPath, FileMode.Create));
                user.imagePath = unigName;
                }

            var result = await _userManager.CreateAsync(user,model.Password);
            Console.WriteLine($"USER CREATED: {user.Email}, PASSWORD: {model.Password}");
            if (result.Succeeded) { return RedirectToAction("Login"); }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View(model);
        }


        //==================================Logout================================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
