using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Enums;
using WorkShop.Repository;
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
        public IActionResult TiketByDepartment(int? id)
        {
            if (id == null)
                return NotFound();

            var devices = _UnitOfWork.devices
                            .FindAll("Department")
                            .Where(d => d.DepartmentId == id)
                            .ToList();

            var ticketCount = devices
                            .GroupBy(d => d.Status)
                            .Select(g => new ReportViewModel
                            {
                                DepartmentId = id.Value,
                                TiketStatus = g.Key,
                                TiketNumber = g.Count()
                            }).ToList();

            var trendData = devices
                .GroupBy(d => new
                {
                    Region = string.IsNullOrEmpty(d.FromLocation) ? "Unknown" : d.FromLocation,
                 
                })
                .Select(g => new ReportRgionIssuesViewModel
                {
                    DepartmentId = id.Value,
                    TiketNumber = g.Count(),
                    TiketRegion = g.Key.Region

                }
              ).ToList();



    

            var model = new ReportLineDepartmentViewModel
            {
                TicketCounts = ticketCount,
                Datasets = trendData
            };

            return View(model);
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

        [HttpGet]
        public async Task<IActionResult> UserMintenanceHistory(string? id)
        {
            if (id == null)
                return NotFound();


            var devices = _UnitOfWork.devices
                            .FindAll("Department")
                            .Where(d => d.TechnicianId == id)
                            .ToList();


            var ticketCount = devices
                            .GroupBy(d => d.Status)
                            .Select(g => new ReportUserViewModel
                            {
                                userId = id,
                                TiketStatus = g.Key,
                                TiketNumber = g.Count()
                            }).ToList();

            return View(ticketCount);
        }
    }
}
