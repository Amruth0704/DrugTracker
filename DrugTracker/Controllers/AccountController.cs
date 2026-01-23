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
                var user = await _userRepository.GetUserByUsernameAsync(model.Username);

                if (user != null)
                {
                    // Check for Lockout
                    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
                    {
                        ModelState.AddModelError("", "Please try after 2 mins");
                        return View(model);
                    }

                    // Check Password (re-using logic from ValidateUserAsync or doing it here, 
                    // limiting changes to Controller if possible, but ValidateUserAsync logic is simple)
                    // Since ValidateUserAsync does password check, let's use it but we need to know if it failed due to password.
                    // We already fetched 'user', so let's verify password manually here or rely on Repository if we want to keep logic encapsulated.
                    // However, to reuse existing Repository logic without changing its signature significantly:
                    // We can call ValidateUserAsync. If null, it means password failed (since we know user exists).
                    
                    var validUser = await _userRepository.ValidateUserAsync(model.Username, model.Password);

                    if (validUser == null)
                    {
                        // Login Failed
                        user.AccessFailedCount++;
                        if (user.AccessFailedCount >= 3)
                        {
                            user.LockoutEnd = DateTime.Now.AddMinutes(2);
                            ModelState.AddModelError("", "Please try after 2 mins");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid username or password");
                        }
                        await _userRepository.UpdateUserAsync(user);
                        return View(model);
                    }

                    // Login Success
                    // Enforce Role Check if ExpectedRole is set
                    if (!string.IsNullOrEmpty(model.ExpectedRole) && 
                        !string.Equals(user.Role, model.ExpectedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", $"Invalid credentials for {model.ExpectedRole}. You are a {user.Role}.");
                        return View(model);
                    }

                    // Reset Lockout
                    user.AccessFailedCount = 0;
                    user.LockoutEnd = null;
                    await _userRepository.UpdateUserAsync(user);

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
