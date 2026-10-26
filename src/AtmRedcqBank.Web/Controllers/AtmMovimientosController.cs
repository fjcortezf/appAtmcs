using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmMovimientosController : Controller
{
    private readonly AppDbContext _db;

    public AtmMovimientosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");
        if (!long.TryParse(cuentaIdStr, out var cuentaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas
            .FirstOrDefaultAsync(c => c.Id == cuentaId && c.Estado == EstadoCuenta.ACTIVA);

        if (cuenta == null)
            return RedirectToAction("Index", "AtmCuentas");

        var movimientos = await _db.Transacciones
            .Where(t => t.CuentaId == cuentaId)
            .OrderByDescending(t => t.Fecha)
            .Take(10)
            .ToListAsync();

        ViewBag.Cuenta = cuenta;
        return View(movimientos);
    }
}