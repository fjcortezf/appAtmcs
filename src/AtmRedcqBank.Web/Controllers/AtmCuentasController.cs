using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmCuentasController : Controller
{
    private readonly AppDbContext _db;

    public AtmCuentasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        if (!long.TryParse(tarjetaIdStr, out var tarjetaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuentas = await _db.Cuentas
            .Where(c => c.TarjetaId == tarjetaId && c.Estado == EstadoCuenta.ACTIVA)
            .ToListAsync();

        return View(cuentas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seleccionar(long cuentaId)
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        if (!long.TryParse(tarjetaIdStr, out var tarjetaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId && c.TarjetaId == tarjetaId);

        if (cuenta == null)
        {
            TempData["Error"] = "Cuenta no válida.";
            return RedirectToAction("Index");
        }

        HttpContext.Session.SetString("AtmCuentaId", cuentaId.ToString());
        return RedirectToAction("Index", "AtmMenu");
    }
}