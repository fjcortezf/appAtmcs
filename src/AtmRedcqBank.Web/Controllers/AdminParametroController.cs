using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminParametroController : Controller
{
    private readonly AdminParametroService _service;
    private readonly AuditoriaService _auditoriaService;

    public AdminParametroController(AdminParametroService service, AuditoriaService auditoriaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var parametros = await _service.ListarAsync();
        return View(parametros);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, string valor, string? motivo)
    {
        var error = await _service.ActualizarAsync(id, valor, motivo, User.Identity?.Name ?? "");

        if (error != null)
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "PARAMETRO_ACTUALIZADO", "/AdminParametro/Editar", false, error, Request);
            TempData["Error"] = error;
        }
        else
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "PARAMETRO_ACTUALIZADO", "/AdminParametro/Editar", true, $"id={id} nuevo={valor}", Request);
            TempData["Mensaje"] = "Parámetro actualizado.";
        }

        return RedirectToAction("Index");
    }
}