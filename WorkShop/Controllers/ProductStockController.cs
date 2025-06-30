using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    [Authorize]
    public class ProductStockController : Controller
    {

        public ProductStockController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;    
        }
        private readonly IUnitOfWork _unitOfWork;
 
        public IActionResult Index(string searchTerm, int page = 1)
        {
            var pgeSize = 10; // عدد العناصر في كل صفحة
            
            var query = string.IsNullOrEmpty(searchTerm) ?
                _unitOfWork.productStoks.FindAll("product", "store") :
                _unitOfWork.productStoks.SearchBycondition(
                    ps => ps.product.Name.Contains(searchTerm) || 
                    ps.store.Name.Contains(searchTerm), "product", "store");

            int totalItems = query.Count();

            var productStocks = query
                .Skip((page - 1) * pgeSize)
                .Take(pgeSize)
                .ToList();

            var viewModel = new ProductStockViewModel
            {
                ProductStocks = productStocks,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pgeSize)
            };


            return View(viewModel);
        }
        public IActionResult Details() { return View(); }

        [HttpGet]
        public IActionResult Create(int? proId, int? storeId)
        {
            ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll(), "Id", "Name");
            ViewBag.Products = new SelectList(_unitOfWork.products.FindAll(), "Id", "Name");

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
        public IActionResult Create(ProductStock stock)
        {
            ViewBag.Stores = new SelectList(_unitOfWork.stores.FindAll(), "Id", "Name");
            ViewBag.Products = new SelectList(_unitOfWork.products.FindAll(), "Id", "Name");
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
