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
        public IActionResult Index(string? searchTerm, int page = 1)
        {
            try
            {
                int pageSize = 10;
                var query = string.IsNullOrEmpty(searchTerm)
                    ? _unitOfWork.departments.FindAll().ToList()
                    : _unitOfWork.departments.FindAll().Where(d => d.Name.Contains(searchTerm)).ToList();

                int totalDepartment = query.Count;
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
            catch (Exception ex)
            {
                // سجل الخطأ أو أظهر رسالة
                TempData["Error"] = "An error occurred while loading departments."+$"{ex}"??"";
                return View(new DepartmentViewModel());
            }
        }
        public IActionResult Details(int? Id, string? searchTerm, int page = 1)
        {
            try
            {
                if (Id == null)
                {
                    TempData["Error"] = $"There are no employees in this department.";
                    return RedirectToAction("Index");
                }
                var department = _unitOfWork.departments.FindAll("UserDepartments")
                                .FirstOrDefault(d => d.UserDepartments.Any(ud => ud.DepartmentId == Id));

                if (department == null)
                {
                    TempData["Error"] = $"An error occurred while loading departments.";
                    return RedirectToAction("Index");
                }

                int pageSize = 10;
                var usersQuery = _unitOfWork.users.FindAll("UserDepartments")
                                    .Where(u => u.UserDepartments.Any(ud => ud.DepartmentId == department.Id));

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    usersQuery = usersQuery.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
                }

                int totalUsers = usersQuery.Count();
                var pagedUsers = usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while loading departments. {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
          
                return PartialView("_Create", new Department());
          

        }
        [HttpPost]
        public IActionResult Create(Department department)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Insert(department);
                    TempData["Success"] = "Create Successfully";
                    return Json(new { success = true });
                }

                return PartialView("_Create", department);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the department.");
                return PartialView("_Create", department);
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
            try
            {
                if (ModelState.IsValid)
                {
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Update(department);
                    TempData["Success"] = "Update Successfully";
                    return Json(new { success = true });
                }

                return PartialView("_Edit", department);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while editing the department.");
                return PartialView("_Edit", department);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete_Department(int? Id)
        {
            try { 
                _unitOfWork.departments.Delete(Id);
                TempData["Success"] = "Create Successfully";
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
