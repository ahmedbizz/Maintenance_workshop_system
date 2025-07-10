using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Context;
using WorkShop.Enums;
using WorkShop.Migrations;
using WorkShop.Models;
using WorkShop.Repository.Base;

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




        public IActionResult Index()
        {
           
            return View(_unitOfWork.departments.FindAll());
        }

        public IActionResult Details(int? Id)
        {
            if(Id == null)
            {
                return NotFound();
            }
            var Department = _unitOfWork.departments.FindAll("users").FirstOrDefault(d => d.Id == Id);

            if(Department == null)
            {
                return NotFound();
            }
            return View(Department);
        }
        [HttpGet]
        public IActionResult Create(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return View();
            }
            else
            {
                var item = _unitOfWork.departments.FindById(Id);
                if (item == null)
                {
                    return NotFound();
                }
                return View(item);
            }
        }
        [HttpPost]
        public IActionResult Create(Department department)
        {
            if (ModelState.IsValid)
            {
                if (department.Id == 0)
                {
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Insert(department);
                }
                else
                {
                    department.UpdateAt = DateTime.Now;
                    _unitOfWork.departments.Update(department);
                }

              
                return RedirectToAction("Index");
            }
            else
            {
                return View(department);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete_Department(int? Id)
        {
            _unitOfWork.departments.Delete(Id);
            return RedirectToAction("Index");

        }
    }
}
