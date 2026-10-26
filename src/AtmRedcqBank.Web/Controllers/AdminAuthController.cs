using System.Security.Claims;
using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Security;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AdminAuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly AuditoriaService _auditoriaService;

    public AdminAuthController(AppDbContext db,
                               PasswordHasher passwordHasher,
                               AuditoriaService auditoriaService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // Si ya está logueado, redirigir al panel
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "AdminHome");

        return View(new LoginPersonalForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginPersonalForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var personal = await _db.PersonalBanco
            .FirstOrDefaultAsync(p => p.Email == form.Email && p.Estado == "ACTIVO");

        if (personal == null)
        {
            await _auditoriaService.RegistrarAsync(
                Models.Enums.TipoActor.PERSONAL, null, form.Email,
                "LOGIN_PERSONAL", "/Admin/Login", false,
                "Usuario no encontrado", Request);

            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(form);
        }

        var passwordOk = _passwordHasher.Verificar(form.Password, personal.PasswordHash);
        if (!passwordOk)
        {
            await _auditoriaService.RegistrarAsync(
                Models.Enums.TipoActor.PERSONAL, personal.Id, personal.Email,
                "LOGIN_PERSONAL", "/Admin/Login", false,
                "Contraseña incorrecta", Request);

            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(form);
        }

        // Crear cookie de autenticación
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, personal.Id.ToString()),
            new Claim(ClaimTypes.Name, personal.Email),
            new Claim(ClaimTypes.Email, personal.Email),
            new Claim(ClaimTypes.Role, personal.Rol.ToString()),
            new Claim("Nombres", personal.Nombres + " " + personal.Apellidos)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        await _auditoriaService.RegistrarAsync(
            Models.Enums.TipoActor.PERSONAL, personal.Id, personal.Email,
            "LOGIN_PERSONAL", "/Admin/Login", true, null, Request);

        return RedirectToAction("Index", "AdminHome");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        Response.Cookies.Delete("RedcqBank.AdminAuth", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax
        });

        return RedirectToAction("Index", "Home");
    }
}