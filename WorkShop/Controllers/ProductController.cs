using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace WorkShop.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {

        public ProductController(IUnitOfWork unitOfWork , IHostingEnvironment hostingEnvironment)
        {

           // _repository = repository;
           // _store = repStore;
            //_department = repDep;

            _unitOfWork = unitOfWork;
            _environment = hostingEnvironment;

        }
        //protected readonly IRepository<Product> _repository;
        //protected readonly IRepository<Store> _store;
        //protected readonly IRepository<Department> _department;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostingEnvironment _environment;
        public IActionResult Index(string searchTerm, int page = 1)
        {
            int pageSize = 10;
            var query = string.IsNullOrEmpty(searchTerm) ?
                _unitOfWork.products.FindAll("department") :
                _unitOfWork.products.SearchBycondition(p => p.Name.Contains(searchTerm) || p.SerialNumber.Contains(searchTerm) ,"department");
            int totalItems = query.Count();

            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ProductListViewModel
            {
                Products = products,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
            return View(viewModel);
        }

        public IActionResult Details(int? Id)
        {

            return View();
        }

        [HttpGet]
        public IActionResult Create(int? Id)
        {

            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if (Id == null || Id == 0)
            {
                return View();
            }
            else
            {
                return View(_unitOfWork.products.FindById(Id));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            ViewBag.Departments = new SelectList(_unitOfWork.departments.FindAll(), "Id", "Name");
            if (ModelState.IsValid)
            {
                if (product.Id == 0)

                {
                    string filename = string.Empty;
                    if(product.clientFile != null)
                    {
                        string Upload = Path.Combine(_environment.WebRootPath, "images");
                        filename = product.clientFile.FileName;
                        string FullPath = Path.Combine(Upload,filename);
                        using (var strem = new FileStream(FullPath, FileMode.Create))
                        {
                            product.clientFile.CopyTo(strem);
                        }
                        
                        product.imagePath = filename;
                    }


                    product.CreateAt = DateTime.Now;
                    product.UpdateAt = DateTime.Now;
                    _unitOfWork.products.Insert(product);
                }
                else
                {
                    var existingProduct = _unitOfWork.products.FindById(product.Id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }
                    string filename = string.Empty;
                    if (product.clientFile != null)
                    {
                        string Upload = Path.Combine(_environment.WebRootPath, "images");
                        filename = product.clientFile.FileName;
                        string FullPath = Path.Combine(Upload, filename);
                        using (var strem = new FileStream(FullPath, FileMode.Create))
                        {
                            product.clientFile.CopyTo(strem);
                        }
                        existingProduct.imagePath = filename;
                    }
                    existingProduct.Name = product.Name;
                    existingProduct.SerialNumber = product.SerialNumber;
                    existingProduct.Desc = product.Desc;
                    existingProduct.DepartmentId = product.DepartmentId;
                  
                    existingProduct.UpdateAt = DateTime.Now;
                    _unitOfWork.products.Update(existingProduct);
                }

                return RedirectToAction("Index");

            }
            else
            {


                return View(product);
            }


        }

        public IActionResult Delete(int? id)
        {
            var product = _unitOfWork.products.FindById(id);
            if (product == null)
            {
                return NotFound();
            }

            _unitOfWork.products.Delete(product.Id);

            return RedirectToAction("Index");

        }


    }
}
