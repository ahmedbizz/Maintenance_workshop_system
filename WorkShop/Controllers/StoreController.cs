using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WorkShop.Context;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + ","+ Roles.StoreKeeper +"," + Roles.Admin)]
    public class StoreController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        public StoreController(IUnitOfWork unitOfWork, UserManager<User> userManager, ILogService logService, INotificationService notificationService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }


       
        public  async Task<IActionResult> Index(string? searchTerm , int page =1)
        {
            var curentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(curentUser, Roles.Admin);
            List<Store> query;
            var pageSize = 10;
            if (isAdmin)
            {
                query = string.IsNullOrEmpty(searchTerm) ?
                    _unitOfWork.stores.FindAll("department").ToList() :
                    _unitOfWork.stores.FindAll("department").Where(s => s.Name.Contains(searchTerm)).ToList();
            }
            else
            {
                query = string.IsNullOrEmpty(searchTerm) ?
                    _unitOfWork.stores.FindAll("department").Where(s => s.DepartmentId == curentUser.DepartmentId).ToList() :
                    _unitOfWork.stores.FindAll("department").Where(s => s.Name.Contains(searchTerm) && s.DepartmentId == curentUser.DepartmentId).ToList();

            }
            var strors = query.ToList();
            var total = strors.Count;
            var paged = strors.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new StoreViewModel
            {
                stors = paged,
                CurrentPage = page,
                searchTerm = searchTerm,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };


            return View(viewModel);
        }

        public IActionResult Details(int? Id,int page = 1)
        {
            if (Id == null)
            {
                TempData["Error"] = "❌ Store ID is required.";
                return RedirectToAction("Index", "Store");
            }
            var pageSize = 10;
            var store = _unitOfWork.stores.FindById(Id);
            if (store == null)
            {
                TempData["Error"] = "❌ Store not found.";
                return RedirectToAction("Index", "Store");
            }

            var products = _unitOfWork.productStoks.FindAll("product").Where(p => p.storeId == Id).ToList();
            var total = products.Count;
            var paged = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var ViewModel = new StoreDetailsViewModel
            {
                store = store.Name,
                products = paged,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                CurrentPage =page

            };

            return View(ViewModel);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Engineer +","+Roles.Admin)]
        public async Task <IActionResult> Create(int? Id) {
            var curentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(curentUser,Roles.Admin);
            List<Department> departments;


            if (isAdmin)
            {
                departments = _unitOfWork.departments.FindAll().ToList();
            }
            else
            {
                departments = _unitOfWork.departments.FindAll().Where(d => d.Id == curentUser.DepartmentId).ToList();
            }
            ViewBag.Departments = new SelectList(departments, "Id", "Name");
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
        [Authorize(Roles = Roles.Engineer + "," + Roles.Admin)]
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

        [Authorize(Roles = Roles.Engineer + "," + Roles.Admin)]
        public IActionResult Delete(int? id)
        {
            try { 
            var store = _unitOfWork.stores.FindById(id);
            if (store == null)
            {
                return NotFound();
            }

            _unitOfWork.stores.Delete(store.Id);

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


        //===============================StoreKeeper=============================


        [HttpGet]
        public async Task<IActionResult> PendingDeliveries()
        {
            var requsets = _unitOfWork.sparePartRequests.FindAll("Device", "Items.Product")
                .Where(r => r.Status == MaintenanceStatus.ApprovedByOfficer.ToString())
                .ToList();

            return View(requsets);
        }
        [HttpPost]
        [Authorize(Roles = Roles.StoreKeeper)]
        public async Task<IActionResult> PendingDeliveries(int RequestId)
        {
            var request = _unitOfWork.sparePartRequests.FindAll("Items.Product")
                            .FirstOrDefault(r => r.Id == RequestId);
            if (request == null)
            {
                return NotFound();
            }

            foreach (var item in request.Items)
            {
                var product = _unitOfWork.productStoks.FindAll().SingleOrDefault(p => p.productId == item.ProductId && p.storeId == item.StoreId);
                if (product == null || product.quantity < item.Quantity)
                {
                    TempData["Massege"] = "Parts not available in store";
                    return RedirectToAction("PendingDeliveries");

                }

                product.quantity -= item.Quantity;

            }//end for ech

            request.Status = MaintenanceStatus.Delivered.ToString();
            request.IsFinalized = true;
            var StoreKeeper = await _userManager.GetUserAsync(User);
            var device = _unitOfWork.devices.FindById(request.DeviceId);
            if (device == null)
            {
                return NotFound("Device not found.");
            }
  
            var engnieers = await _userManager.GetUsersInRoleAsync(Roles.Engineer);
            var engineer = engnieers.FirstOrDefault(e => e.DepartmentId == device.DepartmentId);

            var Officers = await _userManager.GetUsersInRoleAsync(Roles.Officer);
            var DepartmentOfficers = Officers.Where(o => o.DepartmentId == device.DepartmentId).ToList();


            // سجل الحدث
            await _logService.LogAsync(
                device.Id,
                "Spare parts have been dispensed.",
                $"Spare parts have been dispensed form stor By {new string(StoreKeeper.FullName.Take(10).ToArray())}",
                MaintenanceStatus.Delivered.ToString(),
               "Spare parts have been dispensed.",
                Roles.StoreKeeper,
                StoreKeeper.Id);

            // إشعار الهندس
            await _notificationService.NotifyUsersAsync(
                   request.ManagerId,
                  "Spare Parts Approved",
                   $"Spare parts have been dispensed form stor By {new string(StoreKeeper.FullName.Take(10).ToArray())}" +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
            // إشعار الفني
            await _notificationService.NotifyUsersAsync(
                  request.RequestedById,
                  "Spare Parts Approved",
                  $"Spare parts have been dispensed form stor By {new string(StoreKeeper.FullName.Take(10).ToArray())}" +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
            // إشعار الادارة 
            await _notificationService.NotifyUsersAsync(
                  DepartmentOfficers,
                  "Spare Parts Disbursement",
                  $"Spare parts have been dispensed form stor By {new string(StoreKeeper.FullName.Take(10).ToArray())}" +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );

            

            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Spare Parts Disbursement successfully";

            return RedirectToAction("PendingDeliveries");
        }
    }
}
