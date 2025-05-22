using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace SchoolManager.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                return View(model);
            }

            var (success, message, user) = await _authService.LoginAsync(model.Email, model.Password);

            Console.WriteLine($"[Login] Intento de login para {model.Email} - Éxito: {success}");

            if (!success)
            {
                TempData["Error"] = message;
                return View(model);
            }

            TempData["Success"] = "¡Bienvenido " + user.Name + "!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            TempData["Success"] = "Sesión cerrada correctamente";
            return RedirectToAction("Login");
        }
    }
} 