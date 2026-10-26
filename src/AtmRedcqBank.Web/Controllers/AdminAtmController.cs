using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN,OPERADOR_ATM")]
public class AdminAtmController : Controller
{
    private readonly AdminAtmService _service;
    private readonly AuditoriaService _auditoriaService;

    public AdminAtmController(AdminAtmService service, AuditoriaService auditoriaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var atm = await _service.ObtenerAtmAsync();
        if (atm == null) return NotFound();

        ViewBag.Estados = Enum.GetValues<EstadoATM>();
        ViewBag.Stock = await _service.ObtenerStockAsync(atm.Id);
        ViewBag.Movimientos = await _service.UltimosMovimientosAsync(atm.Id);

        return View(atm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(long atmId, EstadoATM estado)
    {
        var error = await _service.CambiarEstadoAsync(atmId, estado, User.Identity?.Name ?? "");

        if (error != null)
            TempData["Error"] = error;
        else
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "ATM_ESTADO", "/AdminAtm/CambiarEstado", true, $"nuevo={estado}", Request);
            TempData["Mensaje"] = "Estado del cajero actualizado.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperarStock(long atmId, IFormCollection form, bool sumar)
    {
        var cantidades = new Dictionary<long, int>();

        foreach (var key in form.Keys.Where(k => k.StartsWith("cant_")))
        {
            if (long.TryParse(key.Substring(5), out var id) && int.TryParse(form[key], out var cant))
            {
                if (cant != 0) cantidades[id] = cant;
            }
        }

        var error = await _service.OperarStockAsync(atmId, cantidades, sumar, User.Identity?.Name ?? "");

        if (error != null)
            TempData["Error"] = error;
        else
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                sumar ? "ATM_REPOSICION" : "ATM_RETIRO_OPERADOR", "/AdminAtm/OperarStock", true, null, Request);
            TempData["Mensaje"] = sumar ? "Efectivo repuesto." : "Efectivo retirado.";
        }

        return RedirectToAction("Index");
    }
}