using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

public class AtmTransferenciaController : Controller
{
    private readonly TransferenciaService _service;
    private readonly AuditoriaService _auditoria;
    private readonly AppDbContext _db;

    public AtmTransferenciaController(TransferenciaService service,
                                      AuditoriaService auditoria,
                                      AppDbContext db)
    {
        _service = service;
        _auditoria = auditoria;
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
        ViewBag.CuentasPropias = await _db.Cuentas
            .Where(c => c.TarjetaId == cuenta.TarjetaId && c.Id != cuentaId)
            .ToListAsync();

        return View(new TransferenciaRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transferir(TransferenciaRequest request)
    {
        var tarjetaIdStr = HttpContext.Session.GetString("AtmTarjetaId");
        var cuentaIdStr = HttpContext.Session.GetString("AtmCuentaId");

        if (!long.TryParse(tarjetaIdStr, out var tarjetaId) ||
            !long.TryParse(cuentaIdStr, out var cuentaId))
            return RedirectToAction("Login", "AtmAuth");

        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
        ViewBag.Cuenta = cuenta;
        ViewBag.CuentasPropias = await _db.Cuentas
            .Where(c => c.TarjetaId == cuenta!.TarjetaId && c.Id != cuentaId)
            .ToListAsync();

        if (!ModelState.IsValid)
            return View("Index", request);

        // Validar cuenta destino
        string cuentaDestino;
        if (request.Tipo == "PROPIA")
        {
            if (request.CuentaDestinoId == null)
            {
                ViewBag.Error = "Seleccione una cuenta destino.";
                return View("Index", request);
            }
            var destino = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == request.CuentaDestinoId);
            if (destino == null)
            {
                ViewBag.Error = "Cuenta destino no válida.";
                return View("Index", request);
            }
            cuentaDestino = destino.NumeroCuenta;
        }
        else if (request.Tipo == "TERCEROS")
        {
            if (string.IsNullOrWhiteSpace(request.CuentaDestino) || request.CuentaDestino.Length != 14)
            {
                ViewBag.Error = "La cuenta destino debe tener 14 dígitos.";
                return View("Index", request);
            }
            cuentaDestino = request.CuentaDestino;
        }
        else
        {
            ViewBag.Error = "Seleccione el tipo de transferencia.";
            return View("Index", request);
        }

        var result = await _service.TransferirAsync(cuentaId, tarjetaId, cuentaDestino, request.Monto);

        if (!result.Exito)
        {
            await _auditoria.RegistrarAsync(TipoActor.CLIENTE, tarjetaId, null,
                "TRANSFERENCIA", "/AtmTransferencia/Transferir", false,
                result.MensajeError + " destino=" + cuentaDestino, Request);

            ViewBag.Error = result.MensajeError;
            if (result.MaximoTransferible != null)
                ViewBag.MaximoTransferible = result.MaximoTransferible;
            return View("Index", request);
        }

        await _auditoria.RegistrarAsync(TipoActor.CLIENTE, tarjetaId, null,
            "TRANSFERENCIA", "/AtmTransferencia/Transferir", true,
            "op=" + result.NumeroOperacion + " destino=" + cuentaDestino, Request);

        TempData["TransferenciaResult"] = System.Text.Json.JsonSerializer.Serialize(result);
        return RedirectToAction("Comprobante");
    }

    [HttpGet]
    public IActionResult Comprobante()
    {
        var json = TempData["TransferenciaResult"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index", "AtmMenu");

        var result = System.Text.Json.JsonSerializer.Deserialize<TransferenciaResult>(json);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarComprobante()
    {
        TempData.Remove("TransferenciaResult");
        return RedirectToAction("Index", "AtmMenu");
    }
}