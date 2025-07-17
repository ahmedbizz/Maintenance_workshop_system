using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using WorkShop.Context;
using WorkShop.Enums;
using WorkShop.Migrations;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;
using Microsoft.Extensions.Primitives;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Admin + "," + Roles.Engineer)]
    public class DepartmentController : Controller
    {


        public DepartmentController(IUnitOfWork unitOfWork) { 

          // this._repository = repository;
          _unitOfWork = unitOfWork;
        
        }
       // private IRepository<Department> _repository;
        private readonly IUnitOfWork _unitOfWork;



        [HttpGet]
        public IActionResult Index(string? searchTerm ,int page =1)
        {
            var pageSize = 10;
            var query = string.IsNullOrEmpty(searchTerm) ?
                  _unitOfWork.departments.FindAll().ToList() :
                  _unitOfWork.departments.FindAll().Where(d => d.Name.Contains(searchTerm)).ToList();
            var totalDepartment = query.Count;
            var pagedDepartments = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new DepartmentViewModel
            {
                departments = pagedDepartments,
                CurrentPage = page,
                searchTerm = searchTerm,
                TotalPages = (int)Math.Ceiling((double)totalDepartment / pageSize)
            };





            return View(viewModel);
        }

        public IActionResult Details(int? Id, string? searchTerm, int page = 1)
        {
            if(Id == null)
            {
                return NotFound();
            }

            int pageSize = 10;
            var department = _unitOfWork.departments.FindAll("users").FirstOrDefault(d => d.Id == Id);

            if (department == null)
            {
                // معالجة إذا لم يتم العثور على القسم
                return NotFound();
            }

            IEnumerable<User> users = department.users;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                      || u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            int totalUsers = users.Count();

            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var ViewModel = new DepartmentDetailsViewModel
            {
                Department = department,
                Users = pagedUsers,
                TotalUsers = totalUsers,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                SearchTerm = searchTerm

            };
            return View(ViewModel);
        }
        [HttpGet]
        public IActionResult Create()
        {
          
                return PartialView("_Create", new Department());
          

        }
        [HttpPost]
        public IActionResult Create(Department department)
        {
            if (ModelState.IsValid)
            {
         
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Insert(department);



                return Json(new { success = true });
            }
            else
            {
                return PartialView("_Create",department);
            }

        }
        [HttpGet]
        public IActionResult Edit(int? Id)
        {

                var item = _unitOfWork.departments.FindById(Id);
                if (item == null)
                {
                    return NotFound();
                }
                return PartialView("_Edit", item);
         
        }

        [HttpPost]
        public IActionResult Edit(Department department)
        {
            if (ModelState.IsValid)
            {
   
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Update(department);



                return Json(new { success = true }); ;
            }
            else
            {
                return PartialView("_Edit",department);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete_Department(int? Id)
        {
            try { 
            _unitOfWork.departments.Delete(Id);
            return RedirectToAction("Index");
                                    }
            catch (DbUpdateException dbEx)
            {
                // فحص إذا كان الخطأ بسبب ارتباط المستخدم ببيانات أخرى
                if (dbEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                {
                    TempData["DeleteError"] = "The store cannot be deleted because it is associated with other data..";
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
