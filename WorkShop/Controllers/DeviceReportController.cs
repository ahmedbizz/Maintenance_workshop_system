using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.ViewModel;
using static Azure.Core.HttpHeader;

namespace WorkShop.Controllers
{
    public class DeviceReportController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        public DeviceReportController(IUnitOfWork unitOfWork, UserManager<User> userManager, ILogService logService , INotificationService notificationService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Technion)]
        public async Task<IActionResult> SubmitReport(DeviceDetailsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            var device = _unitOfWork.devices
                .FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product")
                .FirstOrDefault(d => d.Id == model.DeviceId);
           if(device == null)
            {
                return NotFound();
            }
            
            var managers = await _userManager.GetUsersInRoleAsync(Roles.Engineer);
            var DepartmentManger = managers.Where(m => m.UserDepartments.Any( u => u.DepartmentId == device.DepartmentId)).ToList();

            var Officers = await _userManager.GetUsersInRoleAsync(Roles.Officer);
            var DepartmentOfficer = Officers.Where(o => o.UserDepartments.Any(u => u.DepartmentId == device.DepartmentId)).ToList();
            if (device == null) return NotFound();

            var card = _unitOfWork.maintenanceCards
                .FindAll()
                .FirstOrDefault(c => c.DeviceId == device.Id);

            if (card == null) return NotFound();

            // تقرير الفني
            card.TechnicianReport = string.IsNullOrWhiteSpace(model.TechnicianReport) ? " " : model.TechnicianReport;
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
                                ,
                                StoreId = item.StoreId
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
                card.SparePartsRequestedAt = DateTime.Now;
                await _unitOfWork.CompleteAsync();

                // سجل الحدث
                var LogTask = _logService.LogAsync(
                    device.Id,
                    "Spare Parts Request",
                    $"Spare Parts Requested By {new string(user.FullName.Take(10).ToArray())}",
                    MaintenanceStatus.AwaitingEngineer.ToString(),
                    card.TechnicianReport,
                    Roles.Technion,
                    user.Id);

                // إشعار للمهندس
                var NotifyEngineer = _notificationService.NotifyUsersAsync(
                      DepartmentManger,
                      "Spare Parts Request",
                      $"Spare parts request for device S/N: {device.SerialNumber}",
                      model.DeviceId
                      );

                await Task.WhenAll(LogTask, NotifyEngineer);



                await _unitOfWork.CompleteAsync();

            }


            // في حال تم الإصلاح بدون قطع
            if (model.IsRepaired && !hasParts)
            {
                var existingRequest = _unitOfWork.sparePartRequests
                    .FindAll("Items")
                    .FirstOrDefault(r => r.DeviceId == model.DeviceId && r.Status != MaintenanceStatus.Delivered.ToString());
                if (existingRequest != null && existingRequest.Status != MaintenanceStatus.Delivered.ToString())
                {
                    TempData["Massege"] = "You have requested spare parts that haven't been delivered yet. Cannot complete repair without parts.";
                    return RedirectToAction("DeviceDetails", "Device", new { id = model.DeviceId });
                }


                device.Status = MaintenanceStatus.Repaired.ToString();
                card.ClosedAt = DateTime.Now;
                card.Status = MaintenanceStatus.Closed.ToString();
                card.ClosedAt = DateTime.Now;
                var usedPartsString = "No need";

                // إيجاد طلب الصرف المرتبط بالجهاز (وليس مُسلّم بعد)
                var usedRequest = _unitOfWork.sparePartRequests
                                    .FindAll("Items.Product") // تحميل العناصر مع المنتجات
                                    .FirstOrDefault(r => r.DeviceId == model.DeviceId &&
                                                         r.Status == MaintenanceStatus.Delivered.ToString());

                if (usedRequest != null && usedRequest.Items != null && usedRequest.Items.Any())
                {
                    var partDescriptions = usedRequest.Items
                        .Where(i => i.Product != null)
                        .Select(i => $"{i.Product.Name} × {i.Quantity}")
                        .ToList();

                    usedPartsString = partDescriptions.Any() ? string.Join(", ", partDescriptions) : "No need";
                }



                var RepairReport = new RepairReport
                {
                    DeviceId = device.Id,
                    ProductId = device.ProductId,
                    ErrorKeyword = model.ErrorKeyword ?? device.SelectedErrorKeyword ?? "Not Specified",
                    ErrorDescription = model.TechnicianReport,
                    SuggestedFix = string.IsNullOrWhiteSpace(model.SuggestedFix) ? "Not Provided" : model.SuggestedFix,
                    UsedParts = usedPartsString,
                    TechnicianName = user.FullName,
                    RepairedAt = DateTime.Now,
                    IsSuccessful = true
                };
                await _unitOfWork.repairReports.AddAsync(RepairReport);
                await _unitOfWork.CompleteAsync();
                // سجل الحدث
                var LogTask = _logService.LogAsync(
                    device.Id,
                    "Repaired",
                    $"Device repaired by {new string(user.FullName.Take(10).ToArray())}",
                    MaintenanceStatus.Closed.ToString(),
                    card.TechnicianReport,
                    Roles.Technion,
                    user.Id);

                // Notefanction Engineer
                var NotifiyEngineer = _notificationService.NotifyUsersAsync(
                         DepartmentManger,
                        "Repaired",
                        $"Device repaired by {new string(user.FullName.Take(10).ToArray())}",
                         model.DeviceId
                      );
                // Notefanction Officer
                var NotifyOfficer = _notificationService.NotifyUsersAsync(
                         DepartmentOfficer,
                        "Repaired",
                        $"Device repaired by {new string(user.FullName.Take(10).ToArray())}",
                         model.DeviceId
                      );



                await Task.WhenAll(LogTask, NotifiyEngineer, NotifyOfficer);

            }

     
            return RedirectToAction("DeviceDetails", "Device", new { id = model.DeviceId });
        }

    }
}
