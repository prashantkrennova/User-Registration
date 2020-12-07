using Core.Interfaces;
using Core.Models;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserRegistration.UI.Controllers
{
    [Authorize(Roles ="Admin")]
    public class AdministrationController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdministrationServices _administrationServices;
        
        public AdministrationController(IAdministrationServices administrationServices, RoleManager<IdentityRole> roleManager,
                                                        UserManager<ApplicationUser> userManager
                                                        )
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _administrationServices = administrationServices;
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("ListUsers");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("ListUsers");
            
        }
    }

        [HttpGet]
        public async Task<IActionResult> ListUsers()
        {
            var users = await _userManager.Users.ToListAsync();
           return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUser
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName

            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> EditUser(EditUser editUser)
        {
            var user = await _userManager.FindByIdAsync(editUser.Id);
            if (user == null)
            {
                return NotFound();
            }

            else
            {
                user.Email = editUser.Email;
                user.UserName = editUser.UserName;
                user.FirstName = editUser.FirstName;
                user.LastName = editUser.LastName;
                

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("ListUsers");
                }

                foreach (var error in result.Errors) 
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(editUser);
            }
        }
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

   
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRole createRole)
        {
            if(ModelState.IsValid)
            {
                //We just need to specify a unique role name to create a new role
                IdentityRole identityRole = new IdentityRole
                {
                    Name = createRole.RoleName
                };
                //Saves the role in the underlying AspNetRoles table
                IdentityResult result = await _roleManager.CreateAsync(identityRole);
                

                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles", "Administration");
                }
                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(createRole);
        }

        [HttpGet]
        public async Task<IActionResult> ListRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if(role == null)
            {
                return NotFound();
            }

            var model = new EditRole
            {
                Id = role.Id,
                RoleName = role.Name
            };
            //Retrieve all the users
            foreach (var user in _userManager.Users.ToList())
            {
                //If the user is in this role, add the username to Users property of EditRoles.
                //This model object is then passed to the view for display
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    model.Users.Add(user.UserName);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRoles(EditRole  model)
        {
            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                return NotFound();
            }
            else
            {
                role.Name = model.RoleName;
               
                //Update the Role using UpdateAsync
                var result = await _roleManager.UpdateAsync(role);

                if(result.Succeeded)
                {
                    return RedirectToAction("ListRoles");
                }
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
  
        }

        [HttpGet]
        public async Task<IActionResult>EditUsersInRole(string roleId)
        {
            ViewBag.roleId = roleId;
            var role = await _roleManager.FindByIdAsync(roleId);
            if(role == null)
            {
                return NotFound();
            }

            var model = new List<UserRole>();
            foreach (var user in _userManager.Users.ToList())
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    UserName = user.UserName
                };

                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRole.IsSelected = true;
                }
                else
                {
                    userRole.IsSelected = false;
                }
                model.Add(userRole);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUsersInRole(List<UserRole> model, string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if(role==null)
            {
                return NotFound();
            }

            for (int i = 0; i<model.Count; i++)
            {
                var user = await _userManager.FindByIdAsync(model[i].UserId);
                IdentityResult result = null;
                if(model[i].IsSelected && !(await _userManager.IsInRoleAsync(user, role.Name)))
                {
                    result = await _userManager.AddToRoleAsync(user, role.Name);
                }
                else if(!model[i].IsSelected && await _userManager.IsInRoleAsync(user, role.Name))
                {
                    result = await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
                else
                {
                    continue;
                }

                if(result.Succeeded)
                {
                    if (i < (model.Count - 1))
                        continue;
                    else
                        return RedirectToAction("EditRoles", new { Id = roleId });
                }
            }
            return RedirectToAction("EditRoles", new { Id = roleId });
        }

    }
}
