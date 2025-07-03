using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using WorkShop.Models;
using WorkShop.Repository;
using WorkShop.Repository.Base;
using WorkShop.ViewModel;

namespace WorkShop.Controllers
{
    public class GroupController : Controller
    {

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;


        public GroupController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }



        [HttpGet]

        public IActionResult Index()
        {
            var groups = _unitOfWork.groups.FindAll();
            var model = groups.Select(g => new CreateGroupViewModel
            {
                Id = g.Id,
                Name =g.Name,
                Description = g.Description
            });
            return View(model);
        }

        [HttpGet]

        public async Task<IActionResult> Create(int? Id)
        {
            var existingGroup = _unitOfWork.groups.FindById(Id);
                if (existingGroup != null)
                {
                var model = new CreateGroupViewModel
                {
                    Id = existingGroup.Id,
                    Name = existingGroup.Name,
                    Description = existingGroup.Description
                };
                    return View(model);
                }
            return View();
            
        }

        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel? model)
        {
         
            var existingGroup = _unitOfWork.groups.FindById(model.Id);
            if (!ModelState.IsValid)
            { return View(model); }

                if (existingGroup != null)
                {
                    // Update existing group
                    existingGroup.Name = model.Name;
                    existingGroup.Description = model.Description;
                    _unitOfWork.groups.Update(existingGroup);
              
                }
                else
                {
                    var Group = new Group
                    {
                        Name = model.Name,
                        Description = model.Description
                    };
                     await _unitOfWork.groups.AddAsync(Group);
                }
      
               
                await _unitOfWork.CompleteAsync();
                return RedirectToAction("Index");
       
        }
        [HttpPost]
        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? Id)
        {
            var group = _unitOfWork.groups.FindAll("UserGroups", "GroupRoles").
                SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                return NotFound();
            }
            if(group.GroupRoles.Any() || group.UserGroups.Any())
            {
                ModelState.AddModelError("", "Cannot delete group with existing roles or users.");
                return View("Index", _unitOfWork.groups.FindAll());
            }
            _unitOfWork.groups.Delete(group);
            await _unitOfWork.CompleteAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]

        public async Task<IActionResult> AssignRoles(int? Id)
        {
            

            if (Id == null)
            {
                return NotFound();
            }
            var group = _unitOfWork.groups.FindAll("GroupRoles").
                                           SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                return NotFound();
            }
            ViewBag.Rols = new SelectList(_roleManager.Roles, "Id", "Name");
            var assignRoles = _unitOfWork.groupRoles.FindAll().Where(g => g.GroupId == group.Id)
                .Select(gr => _roleManager.Roles.FirstOrDefault(r => r.Id == gr.RoleId)?.Name)
                .Where(roleName => roleName != null).ToList();
            var model = new AssignRolesToGroupViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                RoleNames = assignRoles! ,
            };
    
            return View(model);
        }

        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoles(int Id, AssignRolesToGroupViewModel model)
        {
            var group = _unitOfWork.groups.FindAll("GroupRoles").
                                SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                return NotFound();
            }
            if (group.GroupRoles.Any())
            {
                // Remove existing roles
                foreach (var groupRole in group.GroupRoles.ToList())
                {
                    _unitOfWork.groupRoles.Delete(groupRole);
                }
            }
            // Assign new roles
            foreach (var role in model.RoleNames)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
                var groupRole = new GroupRole { GroupId = Id, RoleId = (await _roleManager.FindByNameAsync(role)).Id };

                _unitOfWork.groupRoles.Insert(groupRole);
            }


            await _unitOfWork.CompleteAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]

        public async Task<IActionResult> AssignUsers(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            var group = _unitOfWork.groups.FindAll("UserGroups")
               .SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                return NotFound();
            }
            ViewBag.Users = new SelectList(_unitOfWork.users.FindAll(), "Id", "Name");
            var assignUser = _unitOfWork.userGroups.FindAll()
                .Where(g => g.GroupId == group.Id)
              .Select(r => r.User.UserName)
                .Where(Name => Name != null).ToList();

           
            var model = new AddUserToGroup
            {
                GroupId = group.Id,
                GroupName = group.Name,
                UserNames = assignUser!,

            };

            return View(model);
        }

        //[HttpPost]

        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AssignUsers(int Id, AssignRolesToGroupViewModel model)
        //{
        //    var group = _unitOfWork.groups.FindAll("GroupRoles").
        //                        SingleOrDefault(g => g.Id == Id);
        //    if (group == null)
        //    {
        //        return NotFound();
        //    }
        //    if (group.GroupRoles.Any())
        //    {
        //        // Remove existing roles
        //        foreach (var groupRole in group.GroupRoles.ToList())
        //        {
        //            _unitOfWork.groupRoles.Delete(groupRole);
        //        }
        //    }
        //    // Assign new roles
        //    foreach (var role in model.RoleNames)
        //    {
        //        if (!await _roleManager.RoleExistsAsync(role))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(role));
        //        }
        //        var groupRole = new GroupRole { GroupId = Id, RoleId = (await _roleManager.FindByNameAsync(role)).Id };

        //        _unitOfWork.groupRoles.Insert(groupRole);
        //    }


        //    await _unitOfWork.CompleteAsync();
        //    return RedirectToAction("Index");
        //}
 


}



}
