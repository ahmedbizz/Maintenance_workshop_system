using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NuGet.Packaging.Signing;
using System.Data;
using System.Text;
using WorkShop.Enums;
using WorkShop.Models;
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
        public async Task<IActionResult> DeviceHistory(string? SN,string? ProductName,string? DepartmentId)
        {
            try
            {
                if (string.IsNullOrEmpty(SN))
                    return NotFound("Serial number is required.");

                var history = _UnitOfWork.devices
                    .FindAll("Department", "Product", "MaintenanceCard", "Technician")
                    .Where(c => c.SerialNumber == SN && c.Department.Name == DepartmentId && c.Product.Name == ProductName)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();


                return View(history);
            }
            catch(Exception ex) {

                TempData["Error"] = "Error when loading Device History.";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public async Task<IActionResult> UserMintenanceHistory(string? id)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Error when loading User History.";
                return RedirectToAction("Index","User");
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ImportExcel(IFormFile file,string TableName)
        {
            try
            {
                if (file == null || file.Length == 0)
            {
                TempData["Error"] = "❌ Excel not found.";
                return RedirectToAction("Index", "Product");
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var strem = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(strem);

            var conf = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {

                    UseHeaderRow = true
                }


            };

            var result = reader.AsDataSet(conf);
            var table = result.Tables[0];


            // مثال على الاتصال بقاعدة البيانات
            var connectionString = "server=DESKTOP-E8AEC1J\\WORKSHOP;user Id=sa;password=P@ssw0rd;database=Workshop;TrustServerCertificate=True";

            using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            using var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = TableName
            };

            // تأكد أن أسماء الأعمدة متطابقة
            foreach (DataColumn col in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            try
            {
                bulkCopy.WriteToServer(table);
                TempData["Success"] = "Import Successfully";
                    return RedirectToAction("Index", "Product");
                }
            catch (Exception ex)
            {
                TempData["Error"] = "Error :" + ex.Message.ToString();
               return RedirectToAction("Index", "Product");
                }



            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ خطأ: " + ex.Message;
                return RedirectToAction("Index","Product");
            }
        
    }


        }
    }
