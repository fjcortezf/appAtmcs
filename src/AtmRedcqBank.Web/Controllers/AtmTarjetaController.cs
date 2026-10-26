using AtmRedcqBank.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmTarjetaController : Controller
{
    private readonly AppDbContext _db;

    public AtmTarjetaController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        if (!long.TryParse(tarjetaIdStr, out var tarjetaId))
            return RedirectToAction("Login", "AtmAuth");

        var tarjeta = await _db.Tarjetas
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.Id == tarjetaId);

        if (tarjeta == null)
            return RedirectToAction("Login", "AtmAuth");

        return View(tarjeta);
    }
}