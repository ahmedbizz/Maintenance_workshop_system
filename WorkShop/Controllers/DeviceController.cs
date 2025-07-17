using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using NuGet.Packaging.Signing;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.Services.MainService;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{




    [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," +Roles.Technion+"," +Roles.StoreKeeper)]
    public class DeviceController : Controller
    {

        public DeviceController(IUnitOfWork unitOfWork,UserManager<User> userManager, INotificationService notificationService,ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationService = notificationService;
            _logService = logService;
        }
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;


        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm , int page =1,string status = null, int? departmentId = null)
        {
            var filters = new List<IDeviceFilter>
                {
                    new StatusFilter(status),
                    new DepartmentFilter(departmentId),
                    // مستقبلاً يمكنك إضافة: new CreatedDateFilter(), new RepairedFilter() ... إلخ
                };
            var pageSize = 10;
            var curentUser = await _userManager.GetUserAsync(User);
            var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            IQueryable<Device>  query = string.IsNullOrEmpty(searchTerm) ?
                 _unitOfWork.devices.FindAll("Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId)).AsQueryable():
                 _unitOfWork.devices.SearchBycondition(d => d.SerialNumber.Contains(searchTerm)||
                 d.Product.Name.Contains(searchTerm) , "Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId)).AsQueryable(); ;
         
            foreach (var filter in filters)
            {
                query = filter.Apply(query);
            }
            var devices = query.ToList();
            var totalDevices = devices.Count;
             var pagedDevices = devices.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new DevicesViewModel
            {
                devices = pagedDevices,
                CurrentPage= page,
                searchTerm = searchTerm,
                TotalPages = (int)Math.Ceiling((double)totalDevices / pageSize)
            };
    
            return View(viewModel);
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice()
        {
            var Tech_ALL= await _userManager.GetUsersInRoleAsync("Technion");
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound();
            }
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();

            if (userDepartmentIds == null)
            {
                TempData["Error"] = "Access Denied.";
                return RedirectToAction("Index");
            }
            var viewModel = new AddDeviceViewModel
            {
                Products = _unitOfWork.products.FindAll()
                .Where(p => userDepartmentIds.Contains(p.DepartmentId))
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Departments = _unitOfWork.departments.FindAll()
                .Where(d => userDepartmentIds.Contains(d.Id))
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Technicians = Tech_ALL
                .Where(t => t.UserDepartments.Any(ud => userDepartmentIds.Contains(ud.DepartmentId)))
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.FullName })
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice(AddDeviceViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound();
            }
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            if (userDepartmentIds == null)
            {
                TempData["Error"] = "Access Denied";
                return RedirectToAction("Index");
            }

            void PopulateDropDowns()
            {
                model.Departments = _unitOfWork.departments.FindAll()
                    .Where(d => userDepartmentIds.Contains(d.Id))
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name });

                model.Products = _unitOfWork.products.FindAll()
                    .Where(p => userDepartmentIds.Contains(p.DepartmentId))
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name });

                model.Technicians = _unitOfWork.users.FindAll()
                    .Where(t => t.UserDepartments.Any(ud => userDepartmentIds.Contains(ud.DepartmentId)))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName });
            }
            if (!ModelState.IsValid)
            {
                PopulateDropDowns();
                return View(model);
         
            }
            // تحقق من الانتماء للقسم
            if (userDepartmentIds.Contains(model.DepartmentId))
            {
                ModelState.AddModelError("", "Access Denied");
                PopulateDropDowns();
                return View(model);
            }

            // تحقق من تكرار الجهاز بنفس الرقم التسلسلي داخل نفس القسم
            var existingDevice = _unitOfWork.devices.FindAll()
                .FirstOrDefault(d => d.SerialNumber == model.SerialNumber && 
                d.DepartmentId == model.DepartmentId && 
                !d.Status.Contains(MaintenanceStatus.Repaired.ToString()));
            if (existingDevice != null)
            {
                ModelState.AddModelError("", "This device already have Tiket open ");
                PopulateDropDowns();
                return View(model);
            }


         
                var device = new Device
                {
                    ProductId = model.productId,
                    SerialNumber = model.SerialNumber,
                    FromLocation = model.FromLocation,
                    FaultDate = model.FaultDate,
                    TechnicianId = model.TechnicianId,
                    DepartmentId = model.DepartmentId,
                    CreatedAt = DateTime.Now,
                    Status = "New"
                };
                await _unitOfWork.devices.AddAsync(device);
                await _unitOfWork.CompleteAsync();
                var card = new MaintenanceCard
                    {
                        DeviceId =device.Id,
                        CreatedAt = DateTime.Now,
                        Notes = "AwaitingTechnician",
                        AssignedToTechnicianAt = DateTime.Now,
                        Status = MaintenanceStatus.AwaitingTechnician.ToString()
                    };

                await _unitOfWork.maintenanceCards.AddAsync(card);
                await _unitOfWork.CompleteAsync();

            var LogTask = _logService.LogAsync(
                        device.Id, 
                        " Create new Teckit",
                        "Add New Device & Create Maintenance Card",
                        MaintenanceStatus.AwaitingTechnician.ToString(),
                        "Note",
                        Roles.Technion,
                        user.Id);

                // إرسال إشعار للفنين
                 var NotifyTecnition = _notificationService.NotifyUsersAsync(
                        device.TechnicianId,
                        "Maintenance ticket Open",
                        $"New device added  S/N: {device.SerialNumber}",
                        device.Id
                        );

            await Task.WhenAll(LogTask, NotifyTecnition);

            return RedirectToAction("Index");

        }// end Add Device


        //================ Technicion=========================
  
        [Authorize(Roles = Roles.Technion)]
        public async Task<IActionResult> TechnicionDevices()
        {

            var currentUser = await _userManager.GetUserAsync(User);
            if(currentUser == null)
            {
                return NotFound();
            }
            var userDepartmentIds = currentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            if (currentUser == null)
                return Unauthorized();
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician")
                .Where(d => d.TechnicianId == currentUser.Id && userDepartmentIds.Contains(d.DepartmentId) 
                && d.Status != "Repaired")
                .ToList();

            return View(devices);
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," + Roles.Technion)]
        public async Task<IActionResult> DeviceDetails(int? Id)
        {
            try { 
            var currentUser = await _userManager.GetUserAsync(User);
            var isEngineer = await _userManager.IsInRoleAsync(currentUser, "Engineer");
            var userDepartmentIds = currentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();


                var query = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product");
                Device device;
                if (isEngineer)
                {
                    // المهندس يشاهد الأجهزة في قسمه
                    device = query.FirstOrDefault(d =>
                       userDepartmentIds.Contains(d.DepartmentId) && d.Id == Id);
                }
                else
                {
                    // الفني يشاهد فقط أجهزته
                    device = query.FirstOrDefault(d =>
                        d.TechnicianId == currentUser.Id && d.Id == Id);
                }

                if (device == null)
                    if (device == null) return NotFound();

                var spareRequest = device.SparePartRequests.SelectMany(r => r.Items).ToList();
                var availableStores = _unitOfWork.stores.FindAll()
                                    .Where(s =>userDepartmentIds.Contains(s.DepartmentId))
                                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                    .ToList();
        
                var viewModel = new DeviceDetailsViewModel
                {
                    DeviceId = device.Id,
                    ProductName = device.Product?.Name,
                    SerialNumber = device.SerialNumber,
                    DepartmentName = device.Department?.Name,
                    FaultDate = device.FaultDate,
                    TechnicianReport = string.IsNullOrWhiteSpace(device.MaintenanceCard?.TechnicianReport) ? "" : device.MaintenanceCard?.TechnicianReport,
                    DeviceStatus = device.MaintenanceCard?.Status ?? "Null",
                    SparePartRequest = new SparePartRequestViewModel
                    {
                        DeviceId = device.Id,
                        DeviceSerialNumber = device.SerialNumber,
                        IsFinalized = device.SparePartRequests
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault()?.IsFinalized ?? false,
                        Status = device.SparePartRequests
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault()?.Status ?? "",
                        Items = spareRequest.Any()
                                ? spareRequest.Select(i => new SparePartItemViewModel
                                {
                                    Id = i.Id,
                                    ProductId = i.ProductId,
                                    Quantity = i.Quantity,
                                    StoreId = i.StoreId,
                                    AvailableStores = availableStores
                                }).ToList()
                                : new List<SparePartItemViewModel>
                                {
                                    new SparePartItemViewModel
                                    {
                                        AvailableStores = availableStores
                                    }
                                }

                    },
                    Products = _unitOfWork.products.FindAll()
                        .Where(p => userDepartmentIds.Contains(p.DepartmentId))
                        .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                        .ToList()
                };

            await _unitOfWork.CompleteAsync();


            return View(viewModel);
            }
            catch(Exception ex) {

                return Content($"Erorr: {ex.Message}");
            }

        }//end



  
    }//end Main method 


}
