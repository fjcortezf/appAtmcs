using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmMenuController : Controller
{
    private readonly AppDbContext _db;

    public AtmMenuController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");

        if (!long.TryParse(tarjetaIdStr, out var tarjetaId) ||
            !long.TryParse(cuentaIdStr, out var cuentaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId && c.TarjetaId == tarjetaId && c.Estado == EstadoCuenta.ACTIVA);

        if (cuenta == null)
        {
            HttpContext.Session.Remove("AtmCuentaId");
            return RedirectToAction("Index", "AtmCuentas");
        }

        return View(cuenta);
    }
}