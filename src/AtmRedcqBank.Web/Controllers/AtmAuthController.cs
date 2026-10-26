using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmAuthController : Controller
{
    private readonly AtmAuthService _authService;
    private readonly AuditoriaService _auditoriaService;
    private readonly AppDbContext _db;

    public AtmAuthController(AtmAuthService authService,
                             AuditoriaService auditoriaService,
                             AppDbContext db)
    {
        _authService = authService;
        _auditoriaService = auditoriaService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        // Verificar estado del ATM
        var atm = await _db.Atms.FirstOrDefaultAsync();
        if (atm == null || atm.Estado != EstadoATM.EN_SERVICIO)
        {
            ViewBag.NoOperativo = true;
            ViewBag.EstadoAtm = atm?.Estado.ToString() ?? "Desconocido";
            return View("NoOperativo");
        }

        return View(new LoginAtmForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginAtmForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var result = await _authService.AutenticarAsync(form);

        if (!result.EsExito)
        {
            await _auditoriaService.RegistrarAsync(
                TipoActor.CLIENTE, null, form.NumeroTarjeta,
                "LOGIN_ATM", "/AtmAuth/Login", false,
                result.Mensaje, Request);

            ModelState.AddModelError(string.Empty, result.Mensaje ?? "Error.");
            return View(form);
        }

        // Crear sesión HTTP
        HttpContext.Session.SetString("AtmTarjetaId", result.TarjetaId!.Value.ToString());
        HttpContext.Session.SetString("AtmClienteId", result.ClienteId!.Value.ToString());
        HttpContext.Session.SetString("AtmUltimaActividad", DateTime.Now.ToString("O"));

        var sesionId = await _authService.CrearSesionAsync(
            result.TarjetaId!.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        HttpContext.Session.SetString("AtmSesionId", sesionId.ToString());

        await _auditoriaService.RegistrarAsync(
            TipoActor.CLIENTE, result.ClienteId, form.NumeroTarjeta,
            "LOGIN_ATM", "/AtmAuth/Login", true, null, Request);

        // Verificar si debe cambiar PIN
        var tarjeta = await _db.Tarjetas.FindAsync(result.TarjetaId);
        if (tarjeta?.DebeCambiarPin == true)
        {
            HttpContext.Session.SetString("AtmForzarCambioPin", "true");
            return RedirectToAction("Index", "AtmCambiarPin");
        }

        return RedirectToAction("Index", "AtmCuentas");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var sesionIdStr = HttpContext.Session.GetString("AtmSesionId");
        if (long.TryParse(sesionIdStr, out var sesionId))
            await _authService.CerrarSesionAsync(sesionId, EstadoSesionATM.CERRADA, "LOGOUT_MANUAL");

        HttpContext.Session.Clear();

        if (User.Identity?.IsAuthenticated == true)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Response.Cookies.Delete("RedcqBank.AdminAuth", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            });
        }

        return RedirectToAction("Index", "Home");
    }
}