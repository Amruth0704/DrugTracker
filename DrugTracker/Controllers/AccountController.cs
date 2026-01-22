using DrugTracker.Models.ViewModels;
using DrugTracker.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DrugTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login(string? expectedRole)
        {
            return View(new LoginViewModel { ExpectedRole = expectedRole });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.ValidateUserAsync(model.Username, model.Password);
                if (user != null)
                {
                    // Enforce Role Check if ExpectedRole is set
                    if (!string.IsNullOrEmpty(model.ExpectedRole) && 
                        !string.Equals(user.Role, model.ExpectedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", $"Invalid credentials for {model.ExpectedRole}. You are a {user.Role}.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("OrgId", user.OrgId.ToString()),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Redirect based on Role
                    switch (user.Role.ToUpper())
                    {
                        case "MANUFACTURER":
                            return RedirectToAction("Dashboard", "Manufacturer");
                        case "DISTRIBUTOR":
                            return RedirectToAction("Dashboard", "Distributor");
                        case "PHARMACY":
                            return RedirectToAction("Dashboard", "Pharmacy");
                        case "ADMIN":
                           // return RedirectToAction("Index", "Admin");
                           return RedirectToAction("Index", "Home");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Invalid username or password");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RedirectToDashboard()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    switch (role.ToUpper())
                    {
                        case "MANUFACTURER":
                            return RedirectToAction("Dashboard", "Manufacturer");
                        case "DISTRIBUTOR":
                            return RedirectToAction("Dashboard", "Distributor");
                        case "PHARMACY":
                            return RedirectToAction("Dashboard", "Pharmacy");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
