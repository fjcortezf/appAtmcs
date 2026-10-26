using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmDepositoController : Controller
{
    private readonly DepositoService _depositoService;
    private readonly AuditoriaService _auditoriaService;
    private readonly AppDbContext _db;

    public AtmDepositoController(DepositoService depositoService,
                                 AuditoriaService auditoriaService,
                                 AppDbContext db)
    {
        _depositoService = depositoService;
        _auditoriaService = auditoriaService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");
        if (!long.TryParse(cuentaIdStr, out var cuentaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null) return RedirectToAction("Index", "AtmCuentas");

        ViewBag.Cuenta = cuenta;
        ViewBag.MontosRapidos = new List<int> { 20, 50, 100, 200, 500 };
        return View(new DepositoRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Depositar(DepositoRequest request)
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");

        if (!long.TryParse(tarjetaIdStr, out var tarjetaId) ||
            !long.TryParse(cuentaIdStr, out var cuentaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
        ViewBag.Cuenta = cuenta;
        ViewBag.MontosRapidos = new List<int> { 20, 50, 100, 200, 500 };

        if (!ModelState.IsValid)
            return View("Index", request);

        var result = await _depositoService.DepositarAsync(cuentaId, tarjetaId, request.Monto);

        if (!result.Exito)
        {
            await _auditoriaService.RegistrarAsync(
                TipoActor.CLIENTE, tarjetaId, null, "DEPOSITO", "/AtmDeposito/Depositar",
                false, result.MensajeError + " monto=" + request.Monto, Request);

            ViewBag.Error = result.MensajeError;
            return View("Index", request);
        }

        await _auditoriaService.RegistrarAsync(
            TipoActor.CLIENTE, tarjetaId, null, "DEPOSITO", "/AtmDeposito/Depositar",
            true, "op=" + result.NumeroOperacion + " monto=" + result.MontoDepositado, Request);

        TempData["DepositoResult"] = System.Text.Json.JsonSerializer.Serialize(result);
        return RedirectToAction("Comprobante");
    }

    [HttpGet]
    public IActionResult Comprobante()
    {
        var json = TempData["DepositoResult"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index", "AtmMenu");

        var result = System.Text.Json.JsonSerializer.Deserialize<DepositoResult>(json);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarComprobante()
    {
        TempData.Remove("DepositoResult");
        return RedirectToAction("Index", "AtmMenu");
    }
}