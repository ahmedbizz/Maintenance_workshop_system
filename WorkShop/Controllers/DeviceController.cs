using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using NuGet.Packaging.Signing;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.Services.MainService;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{




    [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," +Roles.Technion+"," +Roles.StoreKeeper+","+Roles.DeviceReceiver)]
    public class DeviceController : Controller
    {

        public DeviceController(IUnitOfWork unitOfWork,UserManager<User> userManager, INotificationService notificationService,ILogService logService, RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationService = notificationService;
            _logService = logService;
            _roleManager = roleManager;
        }
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;


        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm , int page =1,string status = null, int? departmentId = null )
        {
            var filters = new List<IDeviceFilter>
                {
                    new StatusFilter(status),
                    new DepartmentFilter(departmentId),
                    // مستقبلاً يمكنك إضافة: new CreatedDateFilter(), new RepairedFilter() ... إلخ
                };
            var pageSize = 10;
            var curentUser = await _userManager.Users
                            .Include(u => u.UserDepartments)
                            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
            var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            IQueryable<Device>  query = string.IsNullOrEmpty(searchTerm) ?
                 _unitOfWork.devices.FindAll("Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId)).AsQueryable():
                 _unitOfWork.devices.SearchBycondition(d => d.SerialNumber.Contains(searchTerm)||
                 d.Product.Name.Contains(searchTerm) , "Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId)).AsQueryable();
         
            foreach (var filter in filters)
            {
                query = filter.Apply(query);
            }
            var devices = query.ToList();
            var totalDevices = devices.Count;
            var pagedDevices = devices.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var techniciansFromRole = await _userManager.GetUsersInRoleAsync(Roles.Technion);
            var technicianIds = techniciansFromRole.Select(t => t.Id).ToList();

            var technicians = _unitOfWork.users
                .FindAll("UserDepartments.Department")
                .Where(u =>
                        u.UserDepartments.Any(ud => userDepartmentIds.Contains(ud.DepartmentId)))
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FullName
                })
                .ToList();


            var viewModel = new DevicesViewModel
            {
                devices = pagedDevices,
                CurrentPage= page,
                searchTerm = searchTerm,
                TotalPages = (int)Math.Ceiling((double)totalDevices / pageSize),
                Technicians = technicians
            };
    
            return View(viewModel);
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice(int? Id)
        {
            var users = await _userManager.GetUsersInRoleAsync(Roles.Technion);




            // جلب المستخدم الحالي
            var currentUser = await _userManager.Users
                .Include(u => u.UserDepartments)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var userDepartmentIds = currentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();

            var technicians = users.ToList();


            if (userDepartmentIds == null)
            {
                TempData["Error"] = "Access Denied.";
                return RedirectToAction("Index");
            }

            // جلب الأعطال السابقة من RepairReport
            var errorSuggestions = _unitOfWork.repairReports.FindAll()
                .GroupBy(r => r.ErrorKeyword)
                .Select(g => g.Key) // فقط الكلمات المفتاحية
                .Distinct()
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => new SelectListItem
                {
                    Value = e,
                    Text = e
                })
                .ToList();
            var viewModel = new AddDeviceViewModel
            {
                Products = _unitOfWork.products.FindAll()
                .Where(p => userDepartmentIds.Contains(p.DepartmentId))
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Departments = _unitOfWork.departments.FindAll()
                .Where(d => userDepartmentIds.Contains(d.Id))
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Technicians = technicians
                
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.FullName }),
                ErrorSuggestions = errorSuggestions
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice(AddDeviceViewModel model)
        {
            var Tech_ALL = await _userManager.GetUsersInRoleAsync(Roles.Technion);
            var user = await _userManager.GetUserAsync(User);
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();



            if(user == null)
            {
                return NotFound();
            }
  
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
                    EngineerId = user.Id,
                    CreatedAt = DateTime.Now,
                    Status = "New",
                    FaultDescription = model.FaultDescription
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
        [HttpGet]
        [Authorize(Roles = Roles.Technion)]
        public async Task<IActionResult> TechnicionDevices()
        {

            var curentUser = await _userManager.Users
                  .Include(u => u.UserDepartments)
                  .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
            if (curentUser == null)
            {
                return Unauthorized();
            }
            var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();

            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician")
                .Where(d => d.TechnicianId == curentUser.Id && userDepartmentIds.Contains(d.DepartmentId) 
                && d.Status != "Repaired")
                .ToList();

            return View(devices);
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," + Roles.Technion)]
        public async Task<IActionResult> DeviceDetails(int? Id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var isEngineer = await _userManager.IsInRoleAsync(currentUser, "Engineer");

                var userDepartmentIds = _unitOfWork.UserDepartments
                   .FindAll()
                   .Where(ud => ud.UserId == currentUser.Id)
                   .Select(ud => ud.DepartmentId)
                   .ToList();

                // جلب الأجهزة مع العلاقات المطلوبة + RepairReports
                var query = _unitOfWork.devices.FindAll(
                        "Product",
                        "Department",
                        "Technician",
                        "MaintenanceCard",
                        "SparePartRequests",
                        "SparePartRequests.Items",
                        "SparePartRequests.Items.Product",
                        "RepairReports" // أضف هذا هنا فقط
                    );

                if (query == null) return NotFound();

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

                if (device == null) return NotFound();

                var spareRequest = device.SparePartRequests.SelectMany(r => r.Items).ToList();

                var availableStores = _unitOfWork.stores.FindAll()
                                        .Where(s => userDepartmentIds.Contains(s.DepartmentId))
                                        .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                        .ToList();

                // استخراج الاقتراحات من RepairReports
                var productId = device?.ProductId;
                var suggestions = _unitOfWork.repairReports
                           .FindAll("Device")
                           .Where(r => r.Device.ProductId == productId && !string.IsNullOrWhiteSpace(r.SuggestedFix))
                           .Select(r => r.SuggestedFix)
                           .ToList();

                var viewModel = new DeviceDetailsViewModel
                {
                    DeviceId = device.Id,
                    ProductName = device.Product?.Name,
                    SerialNumber = device.SerialNumber,
                    DepartmentName = device.Department?.Name,
                    FaultDate = device.FaultDate,
                    TechnicianReport = device.MaintenanceCard?.TechnicianReport ?? "",
                    DeviceStatus = device.MaintenanceCard?.Status ?? "Null",
                    Suggestions = suggestions, // أضف هذا الحقل إلى ViewModel
                    SparePartRequest = new SparePartRequestViewModel
                    {
                        DeviceId = device.Id,
                        DeviceSerialNumber = device.SerialNumber,
                        IsFinalized = device.SparePartRequests.OrderByDescending(r => r.Id).FirstOrDefault()?.IsFinalized ?? false,
                        Status = device.SparePartRequests.OrderByDescending(r => r.Id).FirstOrDefault()?.Status ?? "",
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

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }


        //============================ Recived Devices==========================


        [HttpGet]
        [Authorize(Roles = Roles.DeviceReceiver)]
        public async Task<IActionResult> RecivedAdd()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // جلب الأقسام التي ينتمي إليها المستخدم
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();

            var departments = _unitOfWork.departments.FindAll();
            var products = _unitOfWork.products.FindAll();
            var engineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer);
            var viewModel = new DeviceInputViewModel
            {
               
                Departments = new SelectList(departments, "Id", "Name"),

                ComingFromDepartments = new SelectList(departments, "Id", "Name"),

                Products = new SelectList(products,"Id", "Name"),

                Engineers = new SelectList(engineers, "Id", "FullName"),


            };

            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Roles = Roles.DeviceReceiver)]
        public async Task<IActionResult> RecivedAdd(DeviceInputViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            // إعادة تحميل القوائم قبل الإرجاع إلى الصفحة
            
            if (user == null)
                return Unauthorized();


            void PopulateDropDowns()
            {
                var departments = _unitOfWork.departments.FindAll();
                model.Departments = new SelectList(departments, "Id", "Name");

                model.ComingFromDepartments = new SelectList(departments, "Id", "Name");

                var products = _unitOfWork.products.FindAll();

                model.Products = new SelectList(products,"Id", "Name");
                var engineers = _unitOfWork.users.FindAll();
                model.Engineers = new SelectList(engineers, "Id", "FullName");
           
                   
                  
            }

            if (!ModelState.IsValid)
            {
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

            // تحويل بيانات ViewModel إلى كائن Device
            var device = new Device
            {
                SerialNumber = model.SerialNumber,
                FromLocation = model.FromLocation,
                FaultDate = model.FaultDate,
                EngineerId = model.EngineerId,
                ComingFromDepartmentId = model.ComingFromDepartmentId,
                DepartmentId = model.DepartmentId,
                ProductId = model.ProductId,
                Status = model.Status,
                CreatedAt = DateTime.Now,
                FaultDescription = model.FaultDescription
            };

            await _unitOfWork.devices.AddAsync(device);
            await _unitOfWork.CompleteAsync();

            var card = new MaintenanceCard
            {
                DeviceId = device.Id,
                CreatedAt = DateTime.Now,
                Notes = "AssignedToEngineer",
                AssignedToTechnicianAt = DateTime.Now,
                Status = MaintenanceStatus.AssignedToEngineer.ToString()
            };

            await _unitOfWork.maintenanceCards.AddAsync(card);
            await _unitOfWork.CompleteAsync();

            var LogTask = _logService.LogAsync(
            device.Id,
            " Create new Teckit",
            "Add New Device & Create Maintenance Card",
            MaintenanceStatus.AssignedToEngineer.ToString(),
            "Note",
            Roles.Technion,
            user.Id);

            // إرسال إشعار للفنين
            var NotifyEngineer = _notificationService.NotifyUsersAsync(
                   device.EngineerId,
                   "Maintenance ticket Open",
                   $"New device added  S/N: {device.SerialNumber}",
                   device.Id
                   );

            await Task.WhenAll(LogTask, NotifyEngineer);


            TempData["Success"] = "Device successfully add";

            return RedirectToAction("RecivedAdd");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> assignDevice(int? DeviceId, string? technicionId)
        {
            try
            {
                if (DeviceId == null || string.IsNullOrEmpty(technicionId))
                {
                    TempData["Error"] = "Device ID and Technician ID are required.";
                    return RedirectToAction("Index");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                var cardForAssgin = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == DeviceId);
                var device = _unitOfWork.devices.FindById(DeviceId);

                if (device == null)
                {
                    TempData["Error"] = "Device not found.";
                    return RedirectToAction("Index");
                }

                // تعديل بيانات الجهاز
                device.TechnicianId = technicionId;
                device.Status = MaintenanceStatus.AwaitingTechnician.ToString();

                // تعديل بيانات كرت الصيانة
                if (cardForAssgin != null)
                {
                    cardForAssgin.Status = MaintenanceStatus.AwaitingTechnician.ToString();
                    cardForAssgin.EngineerId = user.Id;
                }

                await _unitOfWork.CompleteAsync();

                var LogTaskAssgin = _logService.LogAsync(
                    device.Id,
                    "Assign new Technician",
                    "Assign Technician to Maintenance Card",
                    MaintenanceStatus.AwaitingTechnician.ToString(),
                    "Note",
                    Roles.Technion,
                    user.Id);

                var NotifyTecnitionAssgin = _notificationService.NotifyUsersAsync(
                    device.TechnicianId,
                    "Maintenance ticket assigned",
                    $"New device assigned. S/N: {device.SerialNumber}",
                    device.Id);

                await Task.WhenAll(LogTaskAssgin, NotifyTecnitionAssgin);

                TempData["Success"] = "Device assigned successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}.";
                return RedirectToAction("Index");
            }
        }



    }//end Main method 


}
