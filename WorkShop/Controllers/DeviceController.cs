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
using System.Diagnostics;
using System.Threading.Tasks;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{




    [Authorize(Roles = "Technion,Engineer,Officer,StoreKeeper")]
    public class DeviceController : Controller
    {

        public DeviceController(IUnitOfWork unitOfWork,UserManager<User> userManager) {
        _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public IActionResult Index()
        {
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician").ToList();
            return View(devices);
        }
        [HttpGet]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> AddDevice()
        {
            var Tech_ALL= await _userManager.GetUsersInRoleAsync("Technion");
            var user = await _userManager.GetUserAsync(User);

    
            if (user?.DepartmentId == null)
            {
                TempData["Error"] = "لا يمكنك الوصول إلى هذه الصفحة. لم يتم تعيينك في أي قسم.";
                return RedirectToAction("Index");
            }
            var viewModel = new AddDeviceViewModel
            {
                Products = _unitOfWork.products.FindAll()
                .Where(p => p.DepartmentId == user.DepartmentId)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Departments = _unitOfWork.departments.FindAll()
                .Where(d => d.Id == user.DepartmentId)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                Technicians = Tech_ALL
                .Where(t => t.DepartmentId == user.DepartmentId)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.FullName })
            };
            return View(viewModel);
        }


        //====================== ENUM FOR STATUS =========================


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> AddDevice(AddDeviceViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.DepartmentId == null)
            {
                TempData["Error"] = "لا يمكنك الوصول إلى هذه الصفحة. لم يتم تعيينك في أي قسم.";
                return RedirectToAction("Index");
            }

            void PopulateDropDowns()
            {
                model.Departments = _unitOfWork.departments.FindAll()
                    .Where(d => d.Id == user.DepartmentId)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name });

                model.Products = _unitOfWork.products.FindAll()
                    .Where(p => p.DepartmentId == user.DepartmentId)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name });

                model.Technicians = _unitOfWork.users.FindAll()
                    .Where(t => t.DepartmentId == user.DepartmentId)
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName });
            }
            if (!ModelState.IsValid)
            {
                PopulateDropDowns();
                return View(model);
         
            }
            // تحقق من الانتماء للقسم
            if (user.DepartmentId != model.DepartmentId)
            {
                ModelState.AddModelError("", "لا يمكنك إضافة جهاز إلى قسم لا تنتمي إليه.");
                PopulateDropDowns();
                return View(model);
            }

            // تحقق من تكرار الجهاز بنفس الرقم التسلسلي داخل نفس القسم
            var existingDevice = _unitOfWork.devices.FindAll()
                .FirstOrDefault(d => d.SerialNumber == model.SerialNumber && d.DepartmentId == model.DepartmentId);
            if (existingDevice != null)
            {
                ModelState.AddModelError("", "يوجد جهاز بنفس الرقم التسلسلي في هذا القسم.");
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
                    Status = MaintenanceStatus.AwaitingTechnician.ToString()
                };

                await _unitOfWork.maintenanceCards.AddAsync(card);

            var log = new DeviceLogs
            {
                DeviceId = device.Id,
                Action = "تم إنشاء الجهاز وكرت الصيانة",
                status = MaintenanceStatus.AwaitingTechnician.ToString(),
                Notes = $"تم إنشاء الجهاز بالرقم التسلسلي {device.SerialNumber} وتم فتح كرت صيانة جديد.",
                userId = user.Id,
                description = "تم إنشاء الجهاز وكرت الصيانة",
                Role = "Technion",
                CreatedAt = DateTime.Now
            };

              await _unitOfWork.deviceLogs.AddAsync(log);



                // إرسال إشعار للفنين
                    
                var notification = new Notification
                {
                    Title = "جهاز جديد للإصلاح",
                    Message = $"تم إضافة جهاز جديد بالرقم التسلسلي {device.SerialNumber}.",
                    ReceiverId = device.TechnicianId,
                    DeviceId = device.Id,
                    CreatedAt = DateTime.Now
                };

                        _unitOfWork.notifications.Insert(notification);

                    

                await _unitOfWork.CompleteAsync();




                return RedirectToAction("Index");

           

        }// end AddDevice


        //================ Technicion=========================

        public async Task<IActionResult> TechnicionDevices()
        {

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician")
                .Where(d => d.TechnicianId == currentUser.Id && d.DepartmentId == currentUser.DepartmentId 
                && d.Status != "Repaired")
                .ToList();

            return View(devices);
        }
        [HttpGet]
        public async Task<IActionResult> DeviceDetails(int? Id)
        {
            try { 
            var currentUser = await _userManager.GetUserAsync(User);
            var isEngineer = await _userManager.IsInRoleAsync(currentUser, "Engineer");

              

                var query = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product");
                Device device;
                if (isEngineer)
                {
                    // المهندس يشاهد الأجهزة في قسمه
                    device = query.FirstOrDefault(d =>
                        d.DepartmentId == currentUser.DepartmentId && d.Id == Id);
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
                                    .Where(s => s.DepartmentId == currentUser.DepartmentId)
                                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                                    .ToList();
        
                var viewModel = new DeviceDetailsViewModel
                {
                    DeviceId = device.Id,
                    ProductName = device.Product?.Name,
                    SerialNumber = device.SerialNumber,
                    DepartmentName = device.Department?.Name,
                    FaultDate = device.FaultDate,
                    TechnicianReport = string.IsNullOrWhiteSpace(device.MaintenanceCard?.TechnicianReport) ? "لم يتم كتابة تقرير" : device.MaintenanceCard?.TechnicianReport,
                    DeviceStatus = device.MaintenanceCard?.Status ?? "غير محدد",
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
                        .Where(p => p.DepartmentId == currentUser.DepartmentId)
                        .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                        .ToList()
                };

            await _unitOfWork.CompleteAsync();


            return View(viewModel);
            }
            catch(Exception ex) {

                return Content($"حدث خطأ: {ex.Message}");
            }

        }//end


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(DeviceDetailsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var device = _unitOfWork.devices
                .FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product")
                .FirstOrDefault(d => d.Id == model.DeviceId);

            if (device == null) return NotFound();

            var card = _unitOfWork.maintenanceCards
                .FindAll()
                .FirstOrDefault(c => c.DeviceId == device.Id);

            if (card == null) return NotFound();

            // تقرير الفني
            card.TechnicianReport = string.IsNullOrWhiteSpace(model.TechnicianReport) ? "لم يتم كتابة تقرير" : model.TechnicianReport;
            card.UpdatedAt = DateTime.Now;

            // تجهيز قائمة القطع المختارة
            var selectedItems = model.SparePartRequest?.Items ?? new List<SparePartItemViewModel>();
            bool hasParts = model.RequestSpareParts && selectedItems.Any(i => i.Quantity > 0);

            if (model.RequestSpareParts &&
                 model.SparePartRequest.Items != null &&
                 model.SparePartRequest.Items.Any(i => i.Quantity > 0))
            {

                // هل يوجد طلب سابق؟
                var existingRequest = _unitOfWork.sparePartRequests
                    .FindAll("Items")
                    .FirstOrDefault(r => r.DeviceId == model.DeviceId &&
                                            r.Status != MaintenanceStatus.Delivered.ToString());

                if (existingRequest != null)
                {
                    if (existingRequest.Items == null)
                        existingRequest.Items = new List<SparePartItem>();

                    var modelProductIds = selectedItems.Select(i => i.ProductId).ToHashSet();

                    var originalItems = _unitOfWork.sparePartItems
                        .FindAll()
                        .Where(i => i.RequestId == existingRequest.Id)
                        .ToList();

                    var itemsToRemove = originalItems
                        .Where(oldItem => !modelProductIds.Contains(oldItem.ProductId))
                        .ToList();

                    foreach (var item in itemsToRemove)
                        _unitOfWork.sparePartItems.Delete(item);

                    foreach (var item in selectedItems.Where(i => i.Quantity > 0))
                    {
                        var existingItem = existingRequest.Items
                            .FirstOrDefault(i => i.ProductId == item.ProductId);

                        if (existingItem != null)
                            existingItem.Quantity = item.Quantity;
                        else
                            existingRequest.Items.Add(new SparePartItem
                            {
                                ProductId = item.ProductId,
                                Quantity = item.Quantity
                                ,StoreId = item.StoreId
                            });
                    }
                }
                else
                {
                    // إنشاء طلب جديد
                    var newRequest = new SparePartRequest
                    {
                        DeviceId = model.DeviceId,
                        RequestDate = DateTime.Now,
                        RequestedById = user.Id,
                        Status = MaintenanceStatus.Pending.ToString(),
                        Items = selectedItems
                            .Where(i => i.Quantity > 0)
                            .Select(i => new SparePartItem
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity,
                                StoreId = i.StoreId,
                            }).ToList()
                    };

                    await _unitOfWork.sparePartRequests.AddAsync(newRequest);
                }

                // تحديث الحالة
                device.Status = MaintenanceStatus.NeedsParts.ToString();
                card.Status = MaintenanceStatus.NeedsParts.ToString();

                // سجل الحدث
                var log = new DeviceLogs
                {
                    DeviceId = device.Id,
                    Action = "طلب/تعديل قطع غيار",
                    description = "تم طلب أو تعديل قطع غيار من قبل الفني",
                    status = MaintenanceStatus.AwaitingEngineer.ToString(),
                    Notes = card.TechnicianReport,
                    userId = user.Id,
                    Role = "Technician",
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.deviceLogs.AddAsync(log);

                // إشعار للمهندس
                var managers = await _userManager.GetUsersInRoleAsync("Engineer");
                var DepartmentManger = managers.Where(m => m.DepartmentId == device.DepartmentId);
                foreach (var manager in DepartmentManger)
                {
                    var notification = new Notification
                    {
                        Title = "طلب قطع غيار",
                        Message = $"تم تقديم أو تعديل طلب قطع غيار للجهاز رقم {model.SerialNumber}.",
                        ReceiverId = manager.Id,
                        DeviceId = model.DeviceId,
                        CreatedAt = DateTime.Now
                    };
                    _unitOfWork.notifications.Insert(notification);
                }
            }

   
            // في حال تم الإصلاح بدون قطع
            if (model.IsRepaired && !hasParts)
            {
                var existingRequest = _unitOfWork.sparePartRequests
                    .FindAll("Items")
                    .FirstOrDefault(r => r.DeviceId == model.DeviceId && r.Status != MaintenanceStatus.Delivered.ToString());

                if (existingRequest != null && existingRequest.Items.Any())
                {
                    TempData["Massege"] = "كرت الصيانة يحتوي على قطع غيار مضافة ";
                    return RedirectToAction("DeviceDetails", new { id = model.DeviceId });
                }

                device.Status = MaintenanceStatus.Repaired.ToString();
                card.Status = MaintenanceStatus.Closed.ToString();

                var log = new DeviceLogs
                {
                    DeviceId = device.Id,
                    Action = "تم الإصلاح",
                    description = "تم إصلاح الجهاز من قبل الفني بدون طلب قطع",
                    status = MaintenanceStatus.Closed.ToString(),
                    Notes = card.TechnicianReport,
                    userId = user.Id,
                    Role = "Technician",
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.deviceLogs.AddAsync(log);
                var managers = await _userManager.GetUsersInRoleAsync("Engineer");
                var DepartmentManger = managers.Where(m => m.DepartmentId == device.DepartmentId);
                foreach (var manager in DepartmentManger)
                {
                    var notification = new Notification
                    {
                        Title = "طلب قطع غيار",
                        Message = $"تم تقديم أو تعديل طلب قطع غيار للجهاز رقم {model.SerialNumber}.",
                        ReceiverId = manager.Id,
                        DeviceId = model.DeviceId,
                        CreatedAt = DateTime.Now
                    };
                    _unitOfWork.notifications.Insert(notification);
                }


            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction("DeviceDetails", new { id = model.DeviceId });
        }

        public async Task<IActionResult> Notifications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var notifications = await _unitOfWork.notifications.GetUnreadForUserAsync(currentUser.Id);
            return View(notifications);
        }

        //===============================Engineer========================================


        [HttpGet]
        [Authorize(Roles ="Engineer")]
        public async Task<IActionResult> ReviewPartsRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => d.DepartmentId == user.DepartmentId && d.MaintenanceCard != null && d.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString()).ToList();


            return View(devices);
        }

        [HttpGet]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> DetailsPartsRequests(int Id)
        {
            var request = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product").FirstOrDefault(r => r.Id == Id && r.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString());
            if (request == null) { return NotFound(); }

            if (request == null)
            {
                return NotFound("لم يتم العثور على الجهاز أو لا يحتوي على طلب قطع غيار في حالة NeedsParts.");
            }

            if (request.SparePartRequests == null || !request.SparePartRequests.Any())
            {
                TempData["Message"] = "لا توجد طلبات قطع غيار مسجلة لهذا الجهاز.";
            }

            return View(request);
        }

        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> ApproveParts(int Id)
        {
            var engineer = await _userManager.GetUserAsync(User);
            var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
            var device = _unitOfWork.devices.FindById(Id);
            if (request == null || device == null || card == null) return NotFound();


            request.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
            card.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
            device.Status = MaintenanceStatus.AwaitingOfficer.ToString();

           
            await _unitOfWork.deviceLogs.AddAsync(
                new DeviceLogs
                {
                    DeviceId = Id,
                    Action = "الموافقة على طلب قطع الغيار",
                    Notes= "الموافقة على طلب قطع الغيار",
                    description = "تمت الموافقة على طلب القطع من قبل المهندس.",
                    Role = "Engineer",
                    userId = engineer.Id.ToString(),
                    status = "ApprovedByEngineer",
                    CreatedAt = DateTime.Now
                });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "تمت الموافقة على طلبك",
                Message = "وافق المهندس على طلب قطع الغيار.",
                ReceiverId = request.RequestedById,
                CreatedAt = DateTime.Now
            });
            var officers = await _userManager.GetUsersInRoleAsync("Officer");
            var officer = officers
                .FirstOrDefault(u => u.DepartmentId == request.Device.DepartmentId);
            if (officer != null)
            {
                var notification = new Notification
                {
                    Title = "طلب قطع غيار ينتظر الاعتماد",
                    Message = $"تمت الموافقة من المهندس على طلب قطع الغيار للجهاز {device.SerialNumber}.",
                    ReceiverId = officer.Id,
                    DeviceId = request.Device.Id,
                    CreatedAt = DateTime.Now
                };

                _unitOfWork.notifications.Insert(notification);
              
            }
            else
            {
                var notification = new Notification
                {
                    Title = "لا يوجد ضابط مسؤل  ",
                    Message = $"الرجاء التواصل مع المدير المسؤل  لحل الاشكالية ",
                    ReceiverId = officer.Id,
                    DeviceId = request.Device.Id,
                    CreatedAt = DateTime.Now
                };

                _unitOfWork.notifications.Insert(notification);
                await _unitOfWork.CompleteAsync();
            }

                await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequests");


        }


        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> RejectParts(int Id)
        {
            var engineer = await _userManager.GetUserAsync(User);
            var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
            var device = _unitOfWork.devices.FindById(Id);
            if (request == null || device == null || card == null) return NotFound();


            request.Status = MaintenanceStatus.RejectedByEngineer.ToString();
            card.Status = MaintenanceStatus.RejectedByEngineer.ToString();
            device.Status = MaintenanceStatus.AwaitingEngineer.ToString();


            await _unitOfWork.deviceLogs.AddAsync(
                new DeviceLogs
                {
                    DeviceId = Id,
                    Action = "رفض  طلب قطع الغيار",
                    Notes = "رفض  طلب قطع الغيار",
                    description = "رفضت الموافقة على طلب القطع من قبل المهندس.",
                    Role = "Engineer",
                    userId = engineer.Id.ToString(),
                    status = "ApprovedByEngineer",
                    CreatedAt = DateTime.Now
                });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "رفضت الموافقة على طلبك",
                Message = "رفض المهندس  طلب قطع الغيار.",
                ReceiverId = request.RequestedById,
                CreatedAt = DateTime.Now
            });


            await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequests");
        }

        //============================ Manager ========================
        [HttpGet]
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> ReviewPartsRequestsByOfficer()
        {
            var user = await _userManager.GetUserAsync(User);
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => d.DepartmentId == user.DepartmentId && d.MaintenanceCard.Status == "ApprovedByEngineer").ToList();
            return View(devices);
        }

        [HttpGet]
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> DetailsPartsRequestsByOfficer(int Id)
        {
            var request = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product").FirstOrDefault(r => r.Id == Id && r.MaintenanceCard.Status == "ApprovedByEngineer");
            if (request == null) { return NotFound(); }

            return View(request);
        }
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> ApproveByOfficer(int Id)
        {

            var device = _unitOfWork.devices.FindById(Id);
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
            var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
            var Officer = await _userManager.GetUserAsync(User);
            if (device == null || card == null || request == null) return NotFound();

            device.Status = MaintenanceStatus.ApprovedByOfficer.ToString();
            request.Status = MaintenanceStatus.ApprovedByOfficer.ToString();
            request.ManagerId = Officer.Id;
            card.Status = MaintenanceStatus.ApprovedByOfficer.ToString();
           
            await _unitOfWork.deviceLogs.AddAsync(
                new DeviceLogs
                {
       
                    Notes = "تمت الموافقة على طلب قطع الغيار",
                    DeviceId = Id,
                    Action = "اعتماد الضابط",
                    status = card.Status,
                    description = "تم اعتماد طلب قطع الغيار من قبل الضابط",
                    userId = Officer.Id,
                    Role = "Officer",
                    CreatedAt = DateTime.Now
                });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "تمت الموافقة على طلبك",
                Message = "وافق الضابط على طلب قطع الغيار.",
                ReceiverId = request.RequestedById,
                CreatedAt = DateTime.Now
            });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "تمت الموافقة على طلبك",
                Message = "وافق  الضابط على طلب قطع الغيار.",
                ReceiverId = request.ManagerId,
                CreatedAt = DateTime.Now
            });

            await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequestsByOfficer");


        }

        [Authorize(Roles = "Officer")]
        [HttpPost]
        public async Task<IActionResult> RejectByOfficer(int Id)
        {
            var device = _unitOfWork.devices.FindById(Id);
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
            var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
            if (device == null || card == null || request == null) return NotFound();

            device.Status = MaintenanceStatus.RejectedByOfficer.ToString();
            request.Status = MaintenanceStatus.RejectedByOfficer.ToString();
            card.Status = MaintenanceStatus.RejectedByOfficer.ToString();

            var Officer = await _userManager.GetUserAsync(User);
            await _unitOfWork.deviceLogs.AddAsync(
                new DeviceLogs
                {

                    Notes = "تم رفض طلب قطع الغيار",
                    DeviceId = Id,
                    Action = "رفض الضابط",
                    status = card.Status,
                    description = "تم رفض طلب قطع الغيار من قبل الضابط",
                    userId = Officer.Id,
                    Role = "Officer",
                    CreatedAt = DateTime.Now
                });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "رفضت الموافقة على طلبك",
                Message = "رفض الضابط  طلب قطع الغيار.",
                ReceiverId = request.RequestedById,
                CreatedAt = DateTime.Now
            });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "رفضت الموافقة على طلبك",
                Message = "رفض  الضابط  طلب قطع الغيار.",
                ReceiverId = request.ManagerId,
                CreatedAt = DateTime.Now
            });

            await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequestsByOfficer");
        }

        // ======================== Sper Part Request ==================


        [HttpGet]

        public IActionResult RequestSpareParts(int deviceId) {

            var device = _unitOfWork.devices.FindById(deviceId);
            if (device == null) { return NotFound(); }
            var SparePart = _unitOfWork.sparePartRequests.FindAll("Items")
                            .Where(r => r.DeviceId == deviceId &&r.Status==MaintenanceStatus.Pending.ToString())
                            .FirstOrDefault();
            if (SparePart !=null)
            {
                var model = new SparePartRequestViewModel
                {
                    DeviceId = SparePart.DeviceId,
                    DeviceSerialNumber = device.SerialNumber,
                    
                    Products = _unitOfWork.products.FindAll()
                             .Select(p => new SelectListItem { Value = p.Id.    ToString(), Text = p.Name })
                             .ToList(),
                    Items = (SparePart.Items ?? new List<SparePartItem>())
                            .Select(i => new SparePartItemViewModel
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity
                            }).ToList()
                                };

                return View(model);
            }
            else
            {
                var model = new SparePartRequestViewModel
                {
                    DeviceId = device.Id,
                    DeviceSerialNumber = device.SerialNumber,
                    Products = _unitOfWork.products.FindAll()
                             .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                             .ToList(),

                    Items = new List<SparePartItemViewModel> { new SparePartItemViewModel() } // عنصر واحد مبدأياً
                };

                return View(model);
            }



        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSpareParts(SparePartRequestViewModel model)
        {
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == model.DeviceId);
            var device = _unitOfWork.devices.FindById(model.DeviceId);
            if(card == null || device == null) { return NotFound(); }
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                model.Products = _unitOfWork.products.FindAll()
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                    .ToList();
                return View(model);
            }

            // تحقق من وجود طلب قطع غيار مفتوح بنفس الحالة
            var existingRequest = _unitOfWork.sparePartRequests
                .FindAll("Items")
                .Where(r => r.DeviceId == model.DeviceId && r.Status == MaintenanceStatus.Pending.ToString())
                .FirstOrDefault();

            if (existingRequest != null)
            {
                if (existingRequest.Items == null)
                {
                    existingRequest.Items = new List<SparePartItem>();
                }

                var modelProductIds = model.Items.Select(i => i.ProductId).ToHashSet();

                var originalItems = _unitOfWork.sparePartItems
                    .FindAll()
                    .Where(i => i.RequestId == existingRequest.Id)
                    .ToList();

                var itemsToRemove = originalItems
                    .Where(oldItem => !modelProductIds.Contains(oldItem.ProductId))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    _unitOfWork.sparePartItems.Delete(item);
                }

                foreach (var item in model.Items.Where(i => i.Quantity > 0))
                {
                    var existingItem = existingRequest.Items
                        .FirstOrDefault(i => i.ProductId == item.ProductId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity = item.Quantity;
                    }
                    else
                    {
                        existingRequest.Items.Add(new SparePartItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        });
                    }
                }
                
                card.Status = MaintenanceStatus.NeedsParts.ToString();
                device.Status = MaintenanceStatus.NeedsParts.ToString();
              
                var log = new DeviceLogs
                {
                    DeviceId = device.Id,
                    Action = "تعديل قطع غيار ",
                    description = "تعديل قطع غيار ",
                    status = MaintenanceStatus.AwaitingEngineer.ToString(),
                    Notes = "لا توجد ملاحظات",
                    userId = user.Id,
                    Role = "Technician",
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.deviceLogs.AddAsync(log);
                var managers = await _userManager.GetUsersInRoleAsync("Engineer");
                foreach (var manager in managers)
                {
                    var notification = new Notification
                    {
                        Title = "تعديل طلب قطع غيار",
                        Message = $"تم تعديل طلب قطع غيار للجهاز رقم {model.DeviceSerialNumber}.",
                        ReceiverId = manager.Id,
                        DeviceId = model.DeviceId,
                        CreatedAt = DateTime.Now
                    };

                    _unitOfWork.notifications.Insert(notification);
                }

                await _unitOfWork.CompleteAsync();
            }
            else
            {
                // إنشاء طلب جديد
                var newRequest = new SparePartRequest
                {
                    DeviceId = model.DeviceId,
                    RequestDate = DateTime.Now,
                    RequestedById = user.Id,
                    Status = MaintenanceStatus.Pending.ToString(), // ✅ هنا التعديل المهم
                    Items = model.Items
                        .Where(i => i.Quantity > 0)
                        .Select(i => new SparePartItem
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity
                        }).ToList()
                };

                await _unitOfWork.sparePartRequests.AddAsync(newRequest);
                card.Status = MaintenanceStatus.NeedsParts.ToString();
                device.Status = MaintenanceStatus.NeedsParts.ToString();

                var log = new DeviceLogs
                {
                    DeviceId = device.Id,
                    Action = "طلب قطع غيار ",
                    description = "طلب قطع غيار صيانة ",
                    status = MaintenanceStatus.AwaitingEngineer.ToString(),
                    Notes = "لا توجد ملاحظات",
                    userId = user.Id,
                    Role = "Technician",
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.deviceLogs.AddAsync(log);
                var managers = await _userManager.GetUsersInRoleAsync("Engineer");
                foreach (var manager in managers)
                {
                    var notification = new Notification
                    {
                        Title = "طلب قطع غيار",
                        Message = $"تم تقديم طلب قطع غيار للجهاز رقم {model.DeviceSerialNumber}.",
                        ReceiverId = manager.Id,
                        DeviceId = model.DeviceId,
                        CreatedAt = DateTime.Now
                    };

                    _unitOfWork.notifications.Insert(notification);
                }

                await _unitOfWork.CompleteAsync();
            }

            return RedirectToAction("DeviceDetails", new { id = model.DeviceId });
        }

        [HttpPost]

        public async Task<IActionResult> DeleteSparParts(DeviceDetailsViewModel model) {

            var user = await _userManager.GetUserAsync(User);
            var requests = _unitOfWork.sparePartRequests.FindAll("Items").Where(s => s.DeviceId == model.DeviceId);
            foreach (var request in requests)
            {
                // حذف العناصر أولًا
                if (request.Items != null && request.Items.Any())
                {
                    foreach (var item in request.Items.ToList())
                    {
                        _unitOfWork.sparePartItems.Delete(item);
                    }
                }

                // حذف الطلب نفسه
                _unitOfWork.sparePartRequests.Delete(request);
            }
            await _unitOfWork.CompleteAsync();
            TempData["Massege"] = "تم حذف طلب قطع الغيار بنجاح";
            return RedirectToAction("DeviceDetails", new { id = model.DeviceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItemPart(int DeviceId, int Id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var requests = _unitOfWork.sparePartRequests.FindAll("Items")
                             .Where(s => s.DeviceId == DeviceId);

            foreach (var request in requests)
            {
                var part = request.Items.FirstOrDefault(p => p.Id == Id);
                if (part != null)
                {
                    _unitOfWork.sparePartItems.Delete(part);
                    await _unitOfWork.CompleteAsync();
                    TempData["Massege"] = "✅ تم حذف القطعة بنجاح";
                    return RedirectToAction("DeviceDetails", new { id = DeviceId });
                }
                else
                {
                    TempData["Massege"] = "❌ لم يتم العثور على القطعة.";
                    return RedirectToAction("DeviceDetails", new { id = DeviceId });
                }
            }

            await _unitOfWork.CompleteAsync();
            TempData["Massege"] = "تم حذف القطعة بنجاح";
            return RedirectToAction("DeviceDetails", new { id = DeviceId });
        }

        // For Delete Notifcation 
        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = _unitOfWork.notifications.FindById(id);
            if (notification == null)
                return NotFound();

            _unitOfWork.notifications.Delete(notification.Id);
            await _unitOfWork.CompleteAsync();
            return RedirectToAction("Index");
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
        [Authorize(Roles = "StoreKeeper")]
        public async Task<IActionResult> Deliver(int RequestId)
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
                    TempData["Massege"] = "القطع غير متوفرة او غير كافية  ";
                    return RedirectToAction("PendingDeliveries");

                }

                product.quantity -= item.Quantity;

            }//end for ech

            request.Status = MaintenanceStatus.Delivered.ToString();
            request.IsFinalized = true;
            var StoreKeeper = await _userManager.GetUserAsync(User);
            await _unitOfWork.deviceLogs.AddAsync(
                new DeviceLogs
                {

                    Notes = "تم صرف  قطع الغيار",
                    DeviceId = request.DeviceId,
                    Action = "تم صرف  ",
                    status = request.Status,
                    description = "تم صرف  طلب قطع الغيار من قبل امين المستودع",
                    userId = StoreKeeper.Id,
                    Role = "StoreKeeper",
                    CreatedAt = DateTime.Now
                });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "تم صرف  قطع الغيار",
                Message = "تم صرف  طلب قطع الغيار من قبل امين المستودع",
                ReceiverId = request.RequestedById,
                CreatedAt = DateTime.Now
            });

            _unitOfWork.notifications.Insert(new Notification
            {
                Title = "تم صرف  قطع الغيار",
                Message = "تم صرف  قطع الغيار",
                ReceiverId = request.ManagerId,
                CreatedAt = DateTime.Now
            });

            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "تم صرف القطع بنجاح.";
           
            return RedirectToAction("PendingDeliveries");
        }
  
    }//end Main method 


}
