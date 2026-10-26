using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN,SISTEMAS")]
public class AdminSistemaController : Controller
{
    private readonly SistemaProcesoService _service;
    private readonly AuditoriaService _auditoriaService;

    public AdminSistemaController(SistemaProcesoService service, AuditoriaService auditoriaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.MantenimientoEjecutado = await _service.MantenimientoYaEjecutadoAsync();
        ViewBag.InteresesEjecutados = await _service.InteresesYaEjecutadosAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarMantenimiento()
    {
        var (exito, mensaje) = await _service.EjecutarMantenimientoAsync();

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "PROCESO_MANTENIMIENTO", "/AdminSistema/EjecutarMantenimiento", exito, mensaje, Request);

        if (exito) TempData["Mensaje"] = mensaje;
        else TempData["Error"] = mensaje;

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarIntereses()
    {
        var (exito, mensaje) = await _service.EjecutarInteresesAsync();

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "PROCESO_INTERESES", "/AdminSistema/EjecutarIntereses", exito, mensaje, Request);

        if (exito) TempData["Mensaje"] = mensaje;
        else TempData["Error"] = mensaje;

        return RedirectToAction("Index");
    }
}