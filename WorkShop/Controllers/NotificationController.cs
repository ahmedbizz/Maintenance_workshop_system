using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkShop.Models;
using WorkShop.Repository.Base;

namespace WorkShop.Controllers
{
    public class NotificationController : Controller
    {
        public NotificationController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var notifications = await _unitOfWork.notifications.GetUnreadForUserAsync(currentUser.Id);
            return View(notifications);
        }

        // For Delete Notifcation 
        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int id,string curentUrl)
        {
            var notification = _unitOfWork.notifications.FindById(id);
            if (notification == null)
                return NotFound();

            _unitOfWork.notifications.Delete(notification.Id);
            await _unitOfWork.CompleteAsync();
            var currentUser = await _userManager.GetUserAsync(User);
            var notifications = _unitOfWork.notifications.FindAll()
                                    .Where(n => n.ReceiverId == currentUser.Id)
                                    .ToList();

            if (!string.IsNullOrEmpty(curentUrl))
            {
                return Redirect(curentUrl);
            }


            return RedirectToAction("Index","Home");
        }



    }
}
