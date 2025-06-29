using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkShop.Context;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Controllers
{
    [Authorize]
    public class StoreController : Controller
    {
        public StoreController(IUnitOfWork unitOfWork, AppDbContext context )
        {
            //_repository = repository;
            //_repDep = repDep;
            _unitOfWork = unitOfWork;
            _Context = context;
        }

        //protected readonly IRepository<Store> _repository;
        //protected readonly IRepository<Department> _repDep;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _Context;


        public IActionResult Index()
        {
            return View(_unitOfWork.stores.FindAll("department").ToList());
        }

        public IActionResult Details(int? Id)
        {
            var Result = _Context.stores.Include(s => s.productStocks)
                .ThenInclude(sp => sp.product).FirstOrDefault(s => s.Id == Id);
            if(Result == null)
            {
                return NotFound();                
            }

            return View(Result);
        }

        [HttpGet]
        public IActionResult Create(int? Id) {

            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if (Id == null || Id == 0)
            {
                return View();
            }
            else
            {
                return View(_unitOfWork.stores.FindById(Id));
            }
    
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Store store) {
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if (ModelState.IsValid) { 
                if(store.Id == 0)
  
                {
                    store.CreateAt = DateTime.Now;
                    store.UpdateAt = DateTime.Now;
                    _unitOfWork.stores.Insert(store);
                }
                else
                {
                    var existingStore = _unitOfWork.stores.FindById(store.Id);
                    if (existingStore == null)
                    {
                        return NotFound();
                    }
                    existingStore.Name = store.Name;
                    existingStore.DepartmentId = store.DepartmentId;
                    existingStore.Location = store.Location;
                    existingStore.UpdateAt = DateTime.Now;
                    _unitOfWork.stores.Update(existingStore);
                }

                return RedirectToAction("Index");
            
            } else {


                return View(store);
            }


        }

        public IActionResult Delete(int? id)
        {
            var store = _unitOfWork.stores.FindById(id);
            if (store == null)
            {
                return NotFound();
            }

            _unitOfWork.stores.Delete(store.Id);

            return RedirectToAction("Index");

        }
    }
}
