using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," + Roles.StoreKeeper + "," + Roles.Admin)]
    public class ProductStockController : Controller
    {

        public ProductStockController(IUnitOfWork unitOfWork , UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            var curentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);
            List<ProductStock> query;
            var pageSize = 10;
            if (isAdmin)
            {
                query = string.IsNullOrEmpty(searchTerm) ?
                 _unitOfWork.productStoks.FindAll("product", "store").ToList() :
                _unitOfWork.productStoks.SearchBycondition(p => p.product.Name.Contains(searchTerm) || p.store.Name.Contains(searchTerm), "department").ToList();
            }
            else
            {
                query = string.IsNullOrEmpty(searchTerm) ?
                        _unitOfWork.productStoks.FindAll("product", "store").Where(p => p.product.DepartmentId == curentUser.DepartmentId).ToList() :
                         _unitOfWork.productStoks.SearchBycondition(p => p.product.Name.Contains(searchTerm) || p.store.Name.Contains(searchTerm), "department").Where(p => p.product.DepartmentId == curentUser.DepartmentId).ToList();



            }
            int totalItems = query.Count();

            var productStocks = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new ProductStockViewModel
            {
                ProductStocks = productStocks,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };


            return View(viewModel);
        }
        public IActionResult Details() { return View(); }

        [HttpGet]
        [Authorize(Roles =  Roles.StoreKeeper + "," + Roles.Admin)]
        public async Task<IActionResult> Create(int? proId, int? storeId)
        {
            var curentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);

            if (isAdmin)
            {
                ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll(), "Id", "Name");
                ViewBag.Products = new SelectList(_unitOfWork.products.FindAll(), "Id", "Name");
            }
            else
            {
                ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll().Where(s =>s.DepartmentId == curentUser.DepartmentId), "Id", "Name");
                ViewBag.Products = new SelectList(_unitOfWork.products.FindAll().Where(p => p.DepartmentId == curentUser.DepartmentId), "Id", "Name");
            }
            // إذا لم يتم تمرير المفاتيح، نعرض نموذج فارغ
            if (proId == null || storeId == null)
            {
                return View();
            }

            // جلب العنصر الموجود لتعديله
            var stock = _unitOfWork.productStoks.FindByKeys((int)proId, (int)storeId);
            return View(stock);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles =  Roles.StoreKeeper + "," + Roles.Admin)]
        public async Task<IActionResult> Create(ProductStock stock)
        {
            var curentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);

            if (isAdmin)
            {
                ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll(), "Id", "Name");
                ViewBag.Products = new SelectList(_unitOfWork.products.FindAll(), "Id", "Name");
            }
            else
            {
                ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll().Where(s => s.DepartmentId == curentUser.DepartmentId), "Id", "Name");
                ViewBag.Products = new SelectList(_unitOfWork.products.FindAll().Where(p => p.DepartmentId == curentUser.DepartmentId), "Id", "Name");
            }
            if (ModelState.IsValid)
            {
                var productStok = _unitOfWork.productStoks.FindByKeys(stock.productId, stock.storeId);
                if (productStok == null)

                {
                    _unitOfWork.productStoks.Insert(stock);
                }
                else
                {
                    productStok.quantity = stock.quantity;
                    productStok.productId = stock.productId;
                    productStok.storeId = stock.storeId;
                    _unitOfWork.productStoks.Update(productStok);
                }

                return RedirectToAction("Index");

            }
            else
            {


                return View(stock);
            }


        }
        [Authorize(Roles = Roles.StoreKeeper + "," + Roles.Admin)]
        public IActionResult Delete(ProductStock stock)
        {
            var productStok = _unitOfWork.productStoks.FindByKeys(stock.productId, stock.storeId);
            if (productStok == null)
            {
                return NotFound();
            }
            if (stock.quantity <= 0)
            {
                _unitOfWork.productStoks.Delete(productStok.productId, stock.storeId);
            }
            return RedirectToAction("Index");

        }



    }
}
