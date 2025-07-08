using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    public class SparePartsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public SparePartsController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet]

        public IActionResult RequestSpareParts(int deviceId)
        {

            var device = _unitOfWork.devices.FindById(deviceId);
            if (device == null) { return NotFound(); }
            var SparePart = _unitOfWork.sparePartRequests.FindAll("Items")
                            .Where(r => r.DeviceId == deviceId && r.Status == MaintenanceStatus.Pending.ToString())
                            .FirstOrDefault();
            if (SparePart != null)
            {
                var model = new SparePartRequestViewModel
                {
                    DeviceId = SparePart.DeviceId,
                    DeviceSerialNumber = device.SerialNumber,

                    Products = _unitOfWork.products.FindAll()
                             .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
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
            if (card == null || device == null) { return NotFound(); }
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

            return RedirectToAction("DeviceDetails","Device", new { id = model.DeviceId });
        }

        [HttpPost]

        public async Task<IActionResult> DeleteSparParts(DeviceDetailsViewModel model)
        {

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
            return RedirectToAction("DeviceDetails","Device", new { id = model.DeviceId });
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
                    return RedirectToAction("DeviceDetails", "Device", new { id = DeviceId });
                }
                else
                {
                    TempData["Massege"] = "❌ لم يتم العثور على القطعة.";
                    return RedirectToAction("DeviceDetails", "Device", new { id = DeviceId });
                }
            }

            await _unitOfWork.CompleteAsync();
            TempData["Massege"] = "تم حذف القطعة بنجاح";
            return RedirectToAction("DeviceDetails", "Device", new { id = DeviceId });
        }





    }
}
