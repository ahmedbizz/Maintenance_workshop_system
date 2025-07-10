using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Enums;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    [Authorize(Roles = Roles.Admin+","+Roles.Engineer)]
    public class ReportController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        public ReportController(IUnitOfWork UnitOfWork) {
            _UnitOfWork = UnitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> TiketByDepartment(int? Id)
        {
            var devices = _UnitOfWork.devices
                            .FindAll("Department")
                            .Where(d => d.DepartmentId == Id).ToList();

            var TiketCount = devices
                            .GroupBy(d => d.Status)
                            .Select(T => new ReportViewModel
                            {
                                DepartmentId = Id,
                                TiketStatus = T.Key,
                                TiketNumber = T.Count(),
                            }).ToList();

            ViewBag.DepartmentId = Id;
            return View("TiketByDepartment", TiketCount);
        }

        [HttpGet]
        public async Task<IActionResult> DeviceHistory(string? SN)
        {
            if (string.IsNullOrEmpty(SN))
                return NotFound("Serial number is required.");

            var history = _UnitOfWork.maintenanceCards
                .FindAll("Device.Product", "Device.Technician")
                .Where(c => c.Device.SerialNumber == SN)
                .OrderByDescending(c => c.ReceivedAt)
                .ToList();


            return View(history);
        }
    }
}
