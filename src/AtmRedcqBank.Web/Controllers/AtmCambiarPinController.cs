using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmCambiarPinController : Controller
{
    private readonly CambioPinService _cambioPinService;
    private readonly AuditoriaService _auditoriaService;
    private readonly AppDbContext _db;

    public AtmCambiarPinController(CambioPinService cambioPinService,
                                   AuditoriaService auditoriaService,
                                   AppDbContext db)
    {
        _cambioPinService = cambioPinService;
        _auditoriaService = auditoriaService;
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        if (!long.TryParse(tarjetaIdStr, out _))
            return RedirectToAction("Login", "AtmAuth");

        var forzado = HttpContext.Session.GetString("AtmForzarCambioPin") == "true";
        
        // Si no es forzado, requiere cuenta seleccionada
        if (!forzado && string.IsNullOrEmpty(HttpContext.Session.GetString("AtmCuentaId")))
            return RedirectToAction("Index", "AtmCuentas");

        ViewBag.Forzado = forzado;
        return View(new CambioPinRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cambiar(CambioPinRequest request)
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");
        
        if (!long.TryParse(tarjetaIdStr, out var tarjetaId))
            return RedirectToAction("Login", "AtmAuth");

        var forzado = HttpContext.Session.GetString("AtmForzarCambioPin") == "true";
        ViewBag.Forzado = forzado;

        if (!forzado && !long.TryParse(cuentaIdStr, out _))
            return RedirectToAction("Index", "AtmCuentas");

        if (!ModelState.IsValid)
            return View("Index", request);

        long? cuentaId = long.TryParse(cuentaIdStr, out var cid) ? cid : null;

        var result = await _cambioPinService.CambiarAsync(
            tarjetaId, cuentaId, request.PinActual, request.PinNuevo, request.PinConfirmacion);

        if (!result.Exito)
        {
            await _auditoriaService.RegistrarAsync(TipoActor.CLIENTE, tarjetaId, null,
                "CAMBIO_PIN", "/AtmCambiarPin/Cambiar", false, result.MensajeError, Request);

            ModelState.AddModelError(string.Empty, result.MensajeError ?? "Error.");
            return View("Index", request);
        }

        await _auditoriaService.RegistrarAsync(TipoActor.CLIENTE, tarjetaId, null,
            "CAMBIO_PIN", "/AtmCambiarPin/Cambiar", true, "op=" + result.NumeroOperacion, Request);

        // Limpiar flag de forzado
        HttpContext.Session.Remove("AtmForzarCambioPin");

        TempData["CambioPinResult"] = result.NumeroOperacion;
        return RedirectToAction("Comprobante");
    }

    [HttpGet]
    public IActionResult Comprobante()
    {
        var numOp = TempData["CambioPinResult"] as string;
        if (string.IsNullOrEmpty(numOp))
            return RedirectToAction("Index", "AtmMenu");

        ViewBag.NumeroOperacion = numOp;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Continuar()
    {
        TempData.Remove("CambioPinResult");
        return RedirectToAction("Index", "AtmCuentas");
    }
}