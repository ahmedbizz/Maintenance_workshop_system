using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," + Roles.StoreKeeper + "," + Roles.Admin)]
    public class ProductController : Controller
    {

        public ProductController(IUnitOfWork unitOfWork , IHostingEnvironment hostingEnvironment, UserManager<User> userManager)
        {

            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _environment = hostingEnvironment;

        }

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHostingEnvironment _environment;
        private readonly UserManager<User> _userManager;
        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            try {
                var curentUser = await _userManager.Users
                        .Include(u => u.UserDepartments)
                        .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
                var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);
                List<Product> query;
                var pageSize = 10;
                if (isAdmin)
                {
                    query = string.IsNullOrEmpty(searchTerm) ?
                            _unitOfWork.products.FindAll("department").ToList() :
                            _unitOfWork.products.SearchBycondition(p => p.Name.Contains(searchTerm) || p.PartNumber.Contains(searchTerm), "department").ToList();
                        }
                else
                {
                    query = string.IsNullOrEmpty(searchTerm) ?
                            _unitOfWork.products.FindAll("department").Where(p => userDepartmentIds.Contains(p.DepartmentId)).ToList() :
                            _unitOfWork.products.SearchBycondition(p => p.Name.Contains(searchTerm) || p.PartNumber.Contains(searchTerm), "department").ToList();



                }


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
            } catch (Exception ex) {
                TempData["Error"] = "Error when loading products.";
                return RedirectToAction("Index");
            
            }

        }

        public IActionResult Details(int? Id)
        {

            return View();
        }

        [HttpGet]

        public async Task<IActionResult> Create(int? Id)
        {
            try
            {
                var curentUser = await _userManager.GetUserAsync(User);
                var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);
                List<Department> departments;


                if (isAdmin)
                {
                    departments = _unitOfWork.departments.FindAll().ToList();
                }
                else
                {
                    departments = _unitOfWork.departments.FindAll().Where(d => userDepartmentIds.Contains(d.Id)).ToList();
                }
                ViewBag.Departments = new SelectList(departments, "Id", "Name");
                if (Id == null || Id == 0)
                {
                    return View();
                }
                else
                {
                    return View(_unitOfWork.products.FindById(Id));
                }
            } catch (Exception ex) {
                TempData["Error"] = "Can't loade this page create product Error hapen.";
                return RedirectToAction("Index");
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            try {
                var curentUser = await _userManager.GetUserAsync(User);
                var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);
                List<Department> departments;


                if (isAdmin)
                {
                    departments = _unitOfWork.departments.FindAll().ToList();
                }
                else
                {
                    departments = _unitOfWork.departments.FindAll().Where(d => userDepartmentIds.Contains(d.Id)).ToList();
                }
                ViewBag.Departments = new SelectList(departments, "Id", "Name");
                if (ModelState.IsValid)
                {
                    if (product.Id == 0)

                    {
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

                            product.imagePath = filename;
                        }

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        if (product.clientFile != null)
                        {
                            var extension = Path.GetExtension(product.clientFile.FileName).ToLower();
                            if (!allowedExtensions.Contains(extension))
                            {
                                ModelState.AddModelError("", "Only image files are allowed.");
                                return View(product);
                            }
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
                            TempData["Error"] = "An error occurred while creating the product.";
                            return RedirectToAction("Index");
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
                        existingProduct.PartNumber = product.PartNumber;
                        existingProduct.Desc = product.Desc;
                        existingProduct.DepartmentId = product.DepartmentId;

                        existingProduct.UpdateAt = DateTime.Now;
                        _unitOfWork.products.Update(existingProduct);
                    }
                    TempData["Success"] = "Created Successfully.";
                    return RedirectToAction("Index");

                }
                else
                {


                    return View(product);
                }
            }
            catch(Exception ex) {
                TempData["Error"] = "An error occurred while creating the product.";
                return RedirectToAction("Index");
       
            }
 


        }

        public   IActionResult Delete(int? id)
        {
            try { 
            var product = _unitOfWork.products.FindById(id);
            if (product == null)
            {
                    TempData["Error"] = "An error occurred while deleteing the product.";
                    return RedirectToAction("Index");
                }

            _unitOfWork.products.Delete(product.Id);

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
