using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN,SISTEMAS")]
public class AdminAuditoriaController : Controller
{
    private readonly AdminAuditoriaService _service;

    public AdminAuditoriaController(AdminAuditoriaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? desde,
        DateTime? hasta,
        string? tipoActor,
        string? operacion,
        string? resultado)
    {
        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;
        ViewBag.TipoActor = tipoActor;
        ViewBag.Operacion = operacion;
        ViewBag.Resultado = resultado;

        var auditorias = await _service.ListarAsync(desde, hasta, tipoActor, operacion, resultado);
        return View(auditorias);
    }
}