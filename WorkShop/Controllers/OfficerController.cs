using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TextTemplating;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;

namespace WorkShop.Controllers
{
    public class OfficerController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        public OfficerController(IUnitOfWork unitOfWork, UserManager<User> userManager, ILogService logService, INotificationService notificationService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }


        //============================ Manager ========================
        [HttpGet]
        [Authorize(Roles = Roles.Officer)]
        public async Task<IActionResult> ReviewPartsRequestsByOfficer()
        {
            var user = await _userManager.GetUserAsync(User);
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            var devices = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => userDepartmentIds.Contains(d.DepartmentId) && d.MaintenanceCard.Status == "ApprovedByEngineer").ToList();
            return View(devices);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Officer)]
        public async Task<IActionResult> DetailsPartsRequestsByOfficer(int Id)
        {
            var request = _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard", "SparePartRequests.Items.Product").FirstOrDefault(r => r.Id == Id && r.MaintenanceCard.Status == "ApprovedByEngineer");
            if (request == null) { return NotFound(); }

            return View(request);
        }
        [Authorize(Roles = Roles.Officer)]
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
            var StoreAdmins = await _userManager.GetUsersInRoleAsync(Roles.StoreKeeper);
            var Storeuser = StoreAdmins.Where(s => s.UserDepartments.Any(u => u.DepartmentId == device.DepartmentId)).ToList();
            await _unitOfWork.CompleteAsync();
            // سجل الحدث
            var logTask = _logService.LogAsync(
                device.Id,
                "Spare Parts Approved",
                $"Spare Parts Approved By Cpt.{new string(Officer.FullName.Take(10).ToArray())}",
                MaintenanceStatus.ApprovedByOfficer.ToString(),
                card.TechnicianReport,
                Roles.Officer,
                Officer.Id);
            // إشعار الهندس
            var NotifyEngineer = _notificationService.NotifyUsersAsync(
                   request.ManagerId,
                  "Spare Parts Approved",
                  $"Spare parts approved by Cpt.{new string(Officer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
            // إشعار الفني
            var NotifyTechnitain = _notificationService.NotifyUsersAsync(
                  request.RequestedById,
                  "Spare Parts Approved",
                  $"Spare parts approved by Cpt.{new string(Officer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
            // إشعار المستودع
             var NotifyStoreUser = _notificationService.NotifyUsersAsync(
                  Storeuser,
                  "Spare Parts Disbursement",
                  $"Spare parts approved by Cpt.{new string(Officer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
     
            await Task.WhenAll(logTask,NotifyEngineer, NotifyStoreUser, NotifyTechnitain);

    
            return RedirectToAction("ReviewPartsRequestsByOfficer");

        }

        [Authorize(Roles = Roles.Officer)]
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
            await _unitOfWork.CompleteAsync();
            var Officer = await _userManager.GetUserAsync(User);
            // سجل الحدث
            var LogTask = _logService.LogAsync(
                device.Id,
                "Spare Parts rejected",
                $"Spare Parts rejected By Cpt.{new string(Officer.FullName.Take(10).ToArray())}",
                MaintenanceStatus.RejectedByOfficer.ToString(),
                card.TechnicianReport,
                Roles.Officer,
                Officer.Id);
            // إشعار الهندس
             var NotifyEngineer = _notificationService.NotifyUsersAsync(
                   request.ManagerId,
                  "Spare Parts rejected",
                  $"Spare parts rejected by Cpt.{new string(Officer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );

            // إشعار الفني
            var NotifyTechnaition = _notificationService.NotifyUsersAsync(
                  request.RequestedById,
                  "Spare Parts rejected",
                  $"Spare parts rejected by Cpt.{new string(Officer.FullName.Take(10).ToArray())} " +
                  $"for device S/N: {request.Device.SerialNumber}",
                   request.Device.Id
                  );
   
            await Task.WhenAll(LogTask,NotifyEngineer, NotifyTechnaition);
        
            return RedirectToAction("ReviewPartsRequestsByOfficer");
        }

    }
    
    }
