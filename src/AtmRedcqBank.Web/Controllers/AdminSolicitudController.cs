using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminSolicitudController : Controller
{
    private readonly AdminSolicitudService _service;
    private readonly AuditoriaService _auditoriaService;

    public AdminSolicitudController(AdminSolicitudService service, AuditoriaService auditoriaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var solicitudes = await _service.ListarAsync();
        return View(solicitudes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atender(long id)
    {
        var (solicitud, nuevoNumero, nuevoPin, cuentasReasociadas, error) = 
            await _service.AtenderAsync(id, User.Identity?.Name ?? "");

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Index");
        }

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "SOLICITUD_ATENDIDA", "/AdminSolicitud/Atender", true,
            $"nuevaTarjeta={nuevoNumero} reasociadas={cuentasReasociadas}", Request);

        TempData["TarjetaAtendidaNumero"] = nuevoNumero;
        TempData["TarjetaAtendidaPin"] = nuevoPin;
        TempData["CuentasReasociadas"] = cuentasReasociadas;

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(long id)
    {
        var error = await _service.RechazarAsync(id, User.Identity?.Name ?? "");

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "SOLICITUD_RECHAZADA", "/AdminSolicitud/Rechazar", true, null, Request);
            TempData["Mensaje"] = "Solicitud rechazada.";
        }

        return RedirectToAction("Index");
    }
}