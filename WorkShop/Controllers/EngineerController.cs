using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    public class EngineerController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        public EngineerController(IUnitOfWork unitOfWork, UserManager<User> userManager, ILogService logService, INotificationService notificationService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }

        //===============================Engineer========================================


        [HttpGet]
        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> ReviewPartsRequests(string searchTerm, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var pageSize = 10;
            var query = string.IsNullOrEmpty(searchTerm) ?
                 _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => d.DepartmentId == user.DepartmentId && d.MaintenanceCard != null && d.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString()) :
                _unitOfWork.devices.SearchBycondition(d => d.SerialNumber.Contains(searchTerm) || d.Product.Name.Contains(searchTerm)
                , "Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => d.DepartmentId == user.DepartmentId && d.MaintenanceCard != null && d.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString());
            var totalDevice = query.Count();
            var devices = query.Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();


            var viewModel = new ReviewPartsRequestsViewModel
            {
                devices = devices,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalDevice / pageSize)
            };


            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Engineer)]
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

        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> ApproveParts(int Id)
        {
            var engineer = await _userManager.GetUserAsync(User);
            var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
            var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
            var device = _unitOfWork.devices.FindById(Id);
            if (request == null || device == null || card == null) return NotFound();


            request.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
            card.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
            card.ApprovedByEngineerAt = DateTime.Now;
            device.Status = MaintenanceStatus.AwaitingOfficer.ToString();

            // سجل الحدث
            await _logService.LogAsync(
                device.Id,
                "Spare Parts Approved",
                $"Spare Parts Approved By Eng.{engineer.FullName.Substring(0, 10)}",
                MaintenanceStatus.ApprovedByEngineer.ToString(),
                card.TechnicianReport,
                Roles.Engineer,
                engineer.Id);

            // إشعار الفني
            await _notificationService.NotifyUsersAsync(
                  request.RequestedById,
                  "Spare Parts Approved",
                  $"Spare parts approved by Eng.{new string(engineer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
            var officers = await _userManager.GetUsersInRoleAsync("Officer");
            var officer = officers
                .FirstOrDefault(u => u.DepartmentId == request.Device.DepartmentId);
            if (officer != null)
            {
                await _notificationService.NotifyUsersAsync(
                      officer.Id,
                      "Spare Parts Request",
                      $"Spare parts approved by Eng.{new string(engineer.FullName.Take(10).ToArray())} " +
                      $"for device S/N: {request.Device.SerialNumber}" +
                      $"wating Officer approval",
                      request.Device.Id
                      );
            }
            else
            {
                  await _notificationService.NotifyUsersAsync(
                      officer.Id,
                      "There is no officer in charge",
                      $"Please contact the responsible manager to solve the problem." +
                      $"for device S/N: {request.Device.SerialNumber}"
                     ,
                      request.Device.Id
                      );

          
                await _unitOfWork.CompleteAsync();
            }

            await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequests");


        }


        [Authorize(Roles = Roles.Engineer)]
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
   
            // سجل الحدث
            await _logService.LogAsync(
                device.Id,
                "Spare Parts rejected",
                $"Spare Parts rejected By Eng.{new string(engineer.FullName.Take(10).ToArray())}",
                MaintenanceStatus.RejectedByEngineer.ToString(),
                card.TechnicianReport,
                Roles.Engineer,
                engineer.Id);

            // إشعار الفني
            await _notificationService.NotifyUsersAsync(
                  request.RequestedById,
                  "Spare Parts rejected",
                  $"Spare parts rejected by Eng..{new string(engineer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );

            await _unitOfWork.CompleteAsync();

            return RedirectToAction("ReviewPartsRequests");
        }

    }
}
