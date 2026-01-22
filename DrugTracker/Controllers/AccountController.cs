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

                    var scheme = "ManufacturerScheme"; // Default fallback
                    if (user.Role.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase)) scheme = "ManufacturerScheme";
                    else if (user.Role.Equals("Distributor", StringComparison.OrdinalIgnoreCase)) scheme = "DistributorScheme";
                    else if (user.Role.Equals("Pharmacy", StringComparison.OrdinalIgnoreCase)) scheme = "PharmacyScheme";
                    else if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) scheme = "AdminScheme";

                    var identity = new ClaimsIdentity(claims, scheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(scheme, principal);

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
            // Sign out of all possible schemes
            await HttpContext.SignOutAsync("ManufacturerScheme");
            await HttpContext.SignOutAsync("DistributorScheme");
            await HttpContext.SignOutAsync("PharmacyScheme");
            await HttpContext.SignOutAsync("AdminScheme");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Just in case
            
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
