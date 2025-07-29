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
using NuGet.Configuration;
using NuGet.Packaging.Signing;
using Rotativa.AspNetCore;
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

        public DeviceController(IUnitOfWork unitOfWork,UserManager<User> userManager, 
            INotificationService notificationService,ILogService logService,
            RoleManager<IdentityRole> roleManager,
            ILogger<DeviceController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationService = notificationService;
            _logService = logService;
            _roleManager = roleManager;
            _logger = logger;
        }
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DeviceController> _logger;


        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm , int page =1,string status = null, int? departmentId = null )
        {
            try
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
                IQueryable<Device> query = string.IsNullOrEmpty(searchTerm) ?
                     _unitOfWork.devices.FindAll("Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId)).OrderByDescending(d => d.CreatedAt).AsQueryable() :
                     _unitOfWork.devices.FindAll("Product", "Department", "Technician").Where(d => userDepartmentIds.Contains(d.DepartmentId))
                     .Where(d => d.SerialNumber.Contains(searchTerm) ||
                     d.Product.Name.Contains(searchTerm)).AsQueryable();

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
                        technicianIds.Contains(u.Id) &&
                        u.UserDepartments.Any(ud => userDepartmentIds.Contains(ud.DepartmentId))
                    )
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FullName
                    })
                    .ToList();



                var viewModel = new DevicesViewModel
                {
                    devices = pagedDevices,
                    CurrentPage = page,
                    searchTerm = searchTerm,
                    TotalPages = (int)Math.Ceiling((double)totalDevices / pageSize),
                    Technicians = technicians
                };

                return View(viewModel);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index method");
                TempData["Error"] = "Can't loade this page Error hapen.";
                return RedirectToAction("Index","Home");
            }
  
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice(int? Id)
        {

            try
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
                var ErrorKeywords = _unitOfWork.repairReports.FindAll()
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
                    ErrorSuggestions = ErrorKeywords
                };
                return View(viewModel);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in AddDevice method");
                TempData["Error"] = "Can't loade this page  Error hapen.";
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> AddDevice(AddDeviceViewModel model)
        {
            try
            {
                var Tech_ALL = await _userManager.GetUsersInRoleAsync(Roles.Technion);
                var user = await _userManager.Users
                    .Include(u => u.UserDepartments)
                    .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
                
                if (user == null)
                {      
                    TempData["Error"] = "Can't loade this page Access Denied.";
                    return RedirectToAction("Index");
                }
                var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                if (userDepartmentIds == null)
                {
                    TempData["Error"] = "Access Denied";
                    return RedirectToAction("Index");
                }
                var users = await _userManager.GetUsersInRoleAsync(Roles.Technion);
                var technicians = users;

                void PopulateDropDowns()
                {

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
                    model.Departments = _unitOfWork.departments.FindAll()
                        .Where(d => userDepartmentIds.Contains(d.Id))
                        .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToList();

                    model.Products = _unitOfWork.products.FindAll()
                        .Where(p => userDepartmentIds.Contains(p.DepartmentId))
                        .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToList();

                    model.Technicians = technicians.Select(t => new SelectListItem
                    {
                        Value = t.Id,
                        Text = t.FullName // أو أي خاصية لعرض اسم الفني
                    }).ToList();

                    model.ErrorSuggestions = errorSuggestions;
                }
                if (!ModelState.IsValid)
                {
                    PopulateDropDowns();
                    return View(model);

                }
                // تحقق من الانتماء للقسم
                if (!userDepartmentIds.Contains(model.DepartmentId))
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
                    FaultDescription = model.FaultDescription,
                    SelectedErrorKeyword = model.SelectedErrorKeyword,
                    ErrorKeyword = string.IsNullOrWhiteSpace(model.SelectedErrorKeyword) ? null : model.SelectedErrorKeyword
                };
                await _unitOfWork.devices.AddAsync(device);
                await _unitOfWork.CompleteAsync();
                var card = new MaintenanceCard
                {
                    DeviceId = device.Id,
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
                TempData["Success"] = "Tekit Create Successfully";
                return RedirectToAction("AddDevice");
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index method");
                TempData["Error"] = "Can't loade this page create product Error hapen.";
                return RedirectToAction("Index");
            }
     

        }// end Add Device


        //================ Technicion=========================
        [HttpGet]
        [Authorize(Roles = Roles.Technion)]
        public async Task<IActionResult> TechnicionDevices()
        {
            try
            {
                var curentUser = await _userManager.Users
                  .Include(u => u.UserDepartments)
                  .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
                if (curentUser == null)
                {

                    TempData["Error"] = "you are not Authorized , Access denied";
                    return RedirectToAction("Index");
                }
                var userDepartmentIds = curentUser.UserDepartments.Select(ud => ud.DepartmentId).ToList();

                var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician")
                    .Where(d => d.TechnicianId == curentUser.Id && userDepartmentIds.Contains(d.DepartmentId)
                    && d.Status != "Repaired")
                    .ToList();

                return View(devices);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in TechnicionDevices method");
                TempData["Error"] = "Can't loade this page Error hapen.";
                return RedirectToAction("Index");
            }
        
        }
        [HttpGet]
        [Authorize(Roles = Roles.Engineer + "," + Roles.Officer + "," + Roles.Technion)]
        public async Task<IActionResult> DeviceDetails(int? Id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var isEngineer = await _userManager.IsInRoleAsync(currentUser,Roles.Engineer);

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

                if (query == null)
                {
                    TempData["Error"] = "An error occurred that stopped the process. Please try again.";
                    return RedirectToAction("Index");
                }

                Device device;
                if (isEngineer)
                {
                    // المهندس يشاهد الأجهزة في قسمه
                    device = query.FirstOrDefault(d => userDepartmentIds.Contains(d.DepartmentId) && d.Id == Id);
                }
                else
                {
                    // الفني يشاهد فقط أجهزته
                    device = query.FirstOrDefault(d => d.TechnicianId == currentUser.Id && d.Id == Id);
                }

                if (device == null)
                {
                    TempData["Error"] = "An error occurred that stopped the process. Please try again.";
                    return RedirectToAction("Index");
                }

                var spareRequest = device.SparePartRequests.SelectMany(r => r.Items).ToList();

                var availableStores = _unitOfWork.stores.FindAll()
                                        .Where(s => userDepartmentIds.Contains(s.DepartmentId))
                                        .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                        .ToList();

                // استخراج الاقتراحات من RepairReports
                var productId = device?.ProductId;
                var suggestions = _unitOfWork.repairReports
                    .FindAll("Product")
                    .Where(r =>
                        r.ProductId == productId &&
                        !string.IsNullOrWhiteSpace(device?.ErrorKeyword) &&
                        !string.IsNullOrWhiteSpace(r.ErrorKeyword) &&
                        r.ErrorKeyword.Contains(device.ErrorKeyword) &&
                        !string.IsNullOrWhiteSpace(r.SuggestedFix)
                    )
                    .Select(r =>
                    {
                        var partsList = r.UsedParts?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(p => p.Trim())
                                                    .ToList();

                        var formattedParts = partsList != null && partsList.Any()
                            ? string.Join("\n- ", partsList.Prepend("")) // يبدأ بسطر جديد ثم يضيف علامة - قبل كل جزء
                            : "None";

                        return $"🔧 Suggested Fix:\n{r.SuggestedFix}\n🧩 Used Parts:{formattedParts}";
                    })
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
                    FromLocation = device.FromLocation,
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
                _logger.LogError(ex, "Error in DeviceDetails method");
                TempData["Error"] = "Can't loade this page  Error hapen.";
                return RedirectToAction("Index");
            }
        }


        //============================ Recived Devices==========================


        [HttpGet]
        [Authorize(Roles = Roles.DeviceReceiver)]
        public async Task<IActionResult> RecivedAdd()
        {
            try
            {
                var user = await _userManager.Users
                                .Include(u => u.UserDepartments)
                                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
                if (user == null)
                    return Unauthorized();

                // جلب الأقسام التي ينتمي إليها المستخدم
                var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
                if (user.UserDepartments == null || !user.UserDepartments.Any())
                {
                    TempData["Error"] = "You are not assigned to any department.";
                    return RedirectToAction("Index");
                }

                var departments = _unitOfWork.departments.FindAll();
                var products = _unitOfWork.products.FindAll();


                var engineersFromRole = await _userManager.GetUsersInRoleAsync(Roles.Engineer);
                var engineersIds = engineersFromRole.Select(t => t.Id).ToList();

                var engineers = _unitOfWork.users
                    .FindAll("UserDepartments.Department")
                    .Where(u =>
                        engineersIds.Contains(u.Id) &&
                        u.UserDepartments.Any(ud => userDepartmentIds.Contains(ud.DepartmentId))
                    )
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FullName
                    })
                    .ToList();
                // جلب الأعطال السابقة من RepairReport
                var ErrorKeywords = _unitOfWork.repairReports.FindAll()
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
                var viewModel = new DeviceInputViewModel
                {

                    Departments = new SelectList(departments ?? new List<Department>(), "Id", "Name"),
                    ComingFromDepartments = new SelectList(departments ?? new List<Department>(), "Id", "Name"),
                    Products = new SelectList(products ?? new List<Product>(), "Id", "Name"),
                    Engineers = new SelectList(engineers ?? new List<SelectListItem>(), "Value", "Text"),
                    ErrorKeywords = new SelectList(ErrorKeywords ?? new List<SelectListItem>(), "Value", "Text")

                };

                return View(viewModel);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in RecivedAdd method");
                TempData["Error"] = $"Can't loade this page Assign  Error hapen.{ex.Message}";
                return RedirectToAction("Index");
            }

        }


        [HttpPost]
        [Authorize(Roles = Roles.DeviceReceiver)]
        public async Task<IActionResult> RecivedAdd(DeviceInputViewModel model)
        {
            try
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

                    model.Products = new SelectList(products, "Id", "Name");
                    var engineers = _unitOfWork.users.FindAll();
                    model.Engineers = new SelectList(engineers, "Id", "FullName");

                    model.ErrorKeywords = _unitOfWork.repairReports.FindAll()
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
                    FaultDescription = model.FaultDescription,
                    ErrorKeyword = string.IsNullOrWhiteSpace(model.ErrorKeyword) ? null : model.ErrorKeyword


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
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Recive Device method");
                TempData["Error"] = "Can't loade this page create tiket Error hapen.";
                return RedirectToAction("Index","Home");
            }

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
                _logger.LogError(ex, "Error in assign method");
                TempData["Error"] = "Can't loade this page Assign  Error hapen.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> SendDevice(int? DeviceId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var device = _unitOfWork.devices.FindById(DeviceId);
                var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == DeviceId);
                if (device == null || card == null || user == null)
                {
                    TempData["Error"] = "Can't loade this page Send  Error hapen.";
                    return RedirectToAction("Index");
                }

                device.Status = MaintenanceStatus.Sent.ToString();
                card.Status = MaintenanceStatus.Sent.ToString();
                await _unitOfWork.CompleteAsync();
                // سجل الحدث
                var LogTask = _logService.LogAsync(
                    device.Id,
                    "Sent",
                    $"Device Sent by {new string(user.FullName.Take(10).ToArray())}",
                    MaintenanceStatus.Closed.ToString(),
                    card.TechnicianReport,
                    Roles.Technion,
                    user.Id);

                // Notefanction Engineer
                var NotifiyEngineer = _notificationService.NotifyUsersAsync(
                         device.EngineerId,
                        "Sent",
                        $"Device Sent by {new string(user.FullName.Take(10).ToArray())}",
                        device.Id
                      );

                // Notefanction Officer
                var NotifyOfficer = _notificationService.NotifyUsersAsync(
                         device.managerId,
                        "Sent",
                        $"Device Sent by {new string(user.FullName.Take(10).ToArray())}",
                        device.Id
                      );
                await Task.WhenAll(LogTask, NotifiyEngineer, NotifyOfficer);
                TempData["Success"] = "Sent successfully";
                return RedirectToAction("Index");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in assign method");
                TempData["Error"] = "Can't loade this page Send  Error hapen.";
                return RedirectToAction("Index");
            }

        }

    }//end Main method 


}
