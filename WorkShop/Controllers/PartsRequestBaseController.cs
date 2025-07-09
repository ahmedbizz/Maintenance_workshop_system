using Microsoft.AspNetCore.Mvc;

namespace WorkShop.Controllers
{
    public class PartsRequestBaseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
