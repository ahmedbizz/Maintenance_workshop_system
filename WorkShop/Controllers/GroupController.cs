using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
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
        private readonly ILogger<GroupController> _logger;

        public GroupController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            ILogger<GroupController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
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
        public IActionResult Create(int? Id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Erorr Create");
                TempData["Massege"] = "Somthing Was Erorr!.";
                return RedirectToAction("Index");
            }

            
        }

        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel? model)
        {
            try
            {
                
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var existingGroup = _unitOfWork.groups.FindById(model.Id);
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
                TempData["Success"] = "New group created Successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erorr Create Post");
                TempData["Error"] = "Cannot create group somthing erorr.";
                return RedirectToAction("Index");
            }

       
        }
        [HttpPost]
        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? Id)
        {
            try
            {
                var group = _unitOfWork.groups.FindAll("UserGroups", "GroupRoles").
                SingleOrDefault(g => g.Id == Id);
                if (group == null)
                {
                    TempData["Error"] = "Cannot found group if existing .";
                    return RedirectToAction("Index");
                }
                if (group.GroupRoles.Any() || group.UserGroups.Any())
                {
                    TempData["Massege"] = "Cannot delete group with existing roles or users.";
                    return RedirectToAction("Index");
                }
                _unitOfWork.groups.Delete(group);
                await _unitOfWork.CompleteAsync();
                return RedirectToAction("Index");
            } catch (Exception ex) {
                _logger.LogError(ex, "Get Erorr Delete");
                TempData["Massege"] = $"{ex.Message}.";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]

        public async Task<IActionResult> AssignRoles(int? Id, string searchTerm, int page = 1)
        {
            

            if (Id == null)
            {
                TempData["Error"] = "Cannot found Id if existing .";
                return RedirectToAction("Index");
            }
            var group = _unitOfWork.groups.FindAll("GroupRoles").
                                           SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                TempData["Error"] = "Cannot found group if existing .";
                return RedirectToAction("Index");
            }

            int pageSize = 10;

            var query = string.IsNullOrEmpty(searchTerm) ?
                        _roleManager.Roles :
                        _roleManager.Roles.Where(r => r.Name.Contains(searchTerm));
            var totalitem = query.Count();

            var rols = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();



            ViewBag.Rols = new SelectList(rols, "Id", "Name");
            var assignRoles = _unitOfWork.groupRoles.FindAll()
                .Where(g => g.GroupId == group.Id)
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
            try
            {
                var group = _unitOfWork.groups.FindAll("GroupRoles").
                                    SingleOrDefault(g => g.Id == Id);
                if (group == null)
                {
                    TempData["Error"] = "Cannot found group if existing .";
                    return RedirectToAction("Index");
                }
                if (group.GroupRoles.Any())
                {
                    // Remove existing roles
                    foreach (var groupRole in group.GroupRoles.ToList())
                    {
                        _unitOfWork.groupRoles.Delete(groupRole);
                    }
                }


                var users = _unitOfWork.users.FindAll("UserGroups")
                    .Where(u => u.UserGroups.Any(ug => ug.GroupId == group.Id)).ToList();
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

                foreach (var user in users)
                {
                    await UpdateUserRolsFromGrooup(user);
                }

                await _unitOfWork.CompleteAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Erorr AssignRoles");
                TempData["DeleteError"] = $"An error occurred during the deletion process. {ex.Message}";
                return View("Index");
            }
        }

        [HttpGet]

        public async Task<IActionResult> AssignUsers(int? Id, string searchTerm, int page = 1)
        {

            if (Id == null)
            {
                TempData["Error"] = "Cannot found ID if existing .";
                return RedirectToAction("Index");
            }
            var group = _unitOfWork.groups.FindAll("UserGroups")
                        .SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                TempData["Error"] = "Cannot found group if existing .";
                return RedirectToAction("Index");
            }

            int pageSize = 10;
            var query = string.IsNullOrEmpty(searchTerm) ?
                _unitOfWork.users.FindAll() :
                _unitOfWork.users.SearchBycondition(u => u.Email.Contains(searchTerm));
            var totalitem = query.Count();

            var users = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

 
            ViewBag.Users = new SelectList(users, "Id", "Email");
            var assignUser = _unitOfWork.userGroups.FindAll("User")
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

        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUsers(int Id, AddUserToGroup model)
        {
            var group = _unitOfWork.groups.FindAll("UserGroups").
                                SingleOrDefault(g => g.Id == Id);
            if (group == null)
            {
                TempData["Error"] = "Cannot found group if existing .";
                return RedirectToAction("Index");
            }
            if (group.UserGroups.Any())
            {
                // Remove existing roles
                foreach (var userGroup in group.UserGroups.ToList())
                {
                    _unitOfWork.userGroups.Delete(userGroup);
                }
            }
            // Assign new User
 
            foreach (var userName in model.UserNames)
            {
                var user = await _userManager.FindByIdAsync(userName);
                if (user != null)
                {
                    _unitOfWork.userGroups.Insert(new UserGroup
                    {
                        GroupId = group.Id,
                        UserId = user.Id
                    });
                    await UpdateUserRolsFromGrooup(user);
                }
            }



            await _unitOfWork.CompleteAsync();
            return RedirectToAction("Index");
        }

        private async Task UpdateUserRolsFromGrooup(User user)
        {
            try
            {
                var grupIds = _unitOfWork.userGroups.FindAll()
                    .Where(g => g.UserId == user.Id)
                    .Select(g => g.GroupId)
                    .ToList();

                var RolsfromGroup = _unitOfWork.groupRoles.FindAll("Role")
                    .Where(r => grupIds.Contains(r.GroupId))
                    .Select(r => r.Role.Name)
                    .Distinct()
                    .ToList();

                var currentRols = await _userManager.GetRolesAsync(user);

                var toRemove = currentRols.Except(RolsfromGroup).ToList();

                // To add Roles 
                var toAdd = RolsfromGroup.Except(currentRols).ToList();

                await _userManager.RemoveFromRolesAsync(user, toRemove);
                await _userManager.AddToRolesAsync(user, toAdd);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Erorr AssignUsers");
                TempData["DeleteError"] = $"An error occurred during the deletion process. {ex.Message}";
            }
        }

    }



}
