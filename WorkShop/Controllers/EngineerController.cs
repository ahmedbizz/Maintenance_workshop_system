using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkShop.Enums;
using WorkShop.Models;
using WorkShop.Repository.Base;
using WorkShop.Services;
using WorkShop.Services.MainService;
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
            var user = await _userManager.Users
                            .Include(u => u.UserDepartments)
                            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
            var userDepartmentIds = user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            var pageSize = 10;
            var query = string.IsNullOrEmpty(searchTerm) ?
                 _unitOfWork.devices.FindAll("Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => userDepartmentIds.Contains(d.DepartmentId) && d.MaintenanceCard != null && d.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString()) :
                _unitOfWork.devices.SearchBycondition(d => d.SerialNumber.Contains(searchTerm) || d.Product.Name.Contains(searchTerm)
                , "Product", "Department", "Technician", "MaintenanceCard")
                .Where(d => userDepartmentIds.Contains(d.DepartmentId) && d.MaintenanceCard != null && d.MaintenanceCard.Status == MaintenanceStatus.NeedsParts.ToString());
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
            try
            {
                var engineer = await _userManager.Users
                    .Include(u => u.UserDepartments)
                    .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
                    
                var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
                var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
                var device = _unitOfWork.devices.FindById(Id);
                if (request == null || device == null || card == null)
                {
                    TempData["Error"] = "An error occurred while loading the page.";
                    return RedirectToAction("Index","Device");
                
                } 


                request.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
                request.ManagerId = engineer.Id;
                card.Status = MaintenanceStatus.ApprovedByEngineer.ToString();
                card.ApprovedByEngineerAt = DateTime.Now;
                card.EngineerId = engineer.Id;
                device.EngineerId = engineer.Id;
                device.Status = MaintenanceStatus.AwaitingOfficer.ToString();
                await _unitOfWork.CompleteAsync();
                // سجل الحدث
                var LogTask = _logService.LogAsync(
                    device.Id,
                    "Spare Parts Approved",
                    $"Spare Parts Approved By Eng.{new string(engineer.FullName.Take(10).ToArray())}",
                    MaintenanceStatus.ApprovedByEngineer.ToString(),
                    card.TechnicianReport,
                    Roles.Engineer,
                    engineer.Id);

                // إشعار الفني
                var NotifyTecnition = _notificationService.NotifyUsersAsync(
                      request.RequestedById,
                      "Spare Parts Approved",
                      $"Spare parts approved by Eng.{new string(engineer.FullName.Take(10).ToArray())} " +
                      $"for device S/N: {request.Device.SerialNumber}",
                       request.Device.Id
                      );
                var officersForRole = await _userManager.GetUsersInRoleAsync("Officer");
                var officers = _unitOfWork.users.FindAll("UserDepartments")
                    .Where(u => officersForRole.Any(Ou => Ou.Id == u.Id));
                var officer = officers
                    .FirstOrDefault(u => u.UserDepartments.Any(d => d.DepartmentId == request.Device.DepartmentId));

                if (officer != null)
                {
                    await _notificationService.NotifyUsersAsync(
                        officer.Id,
                        "Spare Parts Request",
                        $"Spare parts approved by Eng. {new string(engineer.FullName.Take(10).ToArray())}\n" +
                        $"For device S/N: {request.Device.SerialNumber}\n" +
                        $"Waiting for officer approval.",
                        request.Device.Id
                    );
                }

                await Task.WhenAll(LogTask, NotifyTecnition);

                TempData["Success"] = "Spare parts approved Successfully";
                return RedirectToAction("ReviewPartsRequests");
            }
            catch (Exception ex) {

                TempData["Error"] = "An error occurred while loading the page.";
                return RedirectToAction("Index","Device");

            }


        }


        [Authorize(Roles = Roles.Engineer)]
        public async Task<IActionResult> RejectParts(int Id)
        {
            try
            {
                var engineer = await _userManager.GetUserAsync(User);
                var request = _unitOfWork.sparePartRequests.FindAll("Items").FirstOrDefault(r => r.DeviceId == Id);
                var card = _unitOfWork.maintenanceCards.FindAll().FirstOrDefault(c => c.DeviceId == Id);
                var device = _unitOfWork.devices.FindById(Id);
                if (request == null || device == null || card == null) return NotFound();


                request.Status = MaintenanceStatus.RejectedByEngineer.ToString();
                request.ManagerId = engineer.Id;
                card.Status = MaintenanceStatus.RejectedByEngineer.ToString();
                card.EngineerId = engineer.Id;
                device.Status = MaintenanceStatus.AwaitingEngineer.ToString();
                device.EngineerId = engineer.Id;
                await _unitOfWork.CompleteAsync();
                // سجل الحدث
                var LogTask = _logService.LogAsync(
                    device.Id,
                    "Spare Parts rejected",
                    $"Spare Parts rejected By Eng.{new string(engineer.FullName.Take(10).ToArray())}",
                    MaintenanceStatus.RejectedByEngineer.ToString(),
                    card.TechnicianReport,
                    Roles.Engineer,
                    engineer.Id);

                // إشعار الفني
                var NotifyTecnition = _notificationService.NotifyUsersAsync(
                      request.RequestedById,
                      "Spare Parts rejected",
                      $"Spare parts rejected by Eng..{new string(engineer.FullName.Take(10).ToArray())} " +
                      $"for device S/N: {request.Device.SerialNumber}",
                       request.Device.Id
                      );
                await Task.WhenAll(LogTask, NotifyTecnition);


                return RedirectToAction("ReviewPartsRequests");
            } catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the page.";
                return RedirectToAction("ReviewPartsRequests");
            }

        }

    }
}
