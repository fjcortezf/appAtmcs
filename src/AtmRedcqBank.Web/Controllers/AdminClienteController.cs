using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminClienteController : Controller
{
    private readonly AdminClienteService _service;
    private readonly AuditoriaService _auditoriaService;
    private readonly AdminTarjetaService _tarjetaService;

    public AdminClienteController(AdminClienteService service,
                                  AuditoriaService auditoriaService,
                                  AdminTarjetaService tarjetaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
        _tarjetaService = tarjetaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var clientes = await _service.ListarAsync();
        return View(clientes);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new ClienteCrearForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ClienteCrearForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var error = await _service.CrearAsync(form);

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            return View(form);
        }

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "CLIENTE_CREADO", "/AdminCliente/Crear", true,
            $"doc={form.TipoDocumento} {form.NumeroDocumento}", Request);

        TempData["Mensaje"] = "Cliente creado correctamente.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(long id)
    {
        var cliente = await _service.ObtenerAsync(id);
        if (cliente == null) return RedirectToAction("Index");
        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTarjeta(long clienteId)
    {
        var (tarjeta, pin, error) = await _tarjetaService.CrearAsync(clienteId);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Detalle", new { id = clienteId });
        }

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "TARJETA_CREADA", "/AdminCliente/CrearTarjeta", true,
            $"numero={tarjeta!.NumeroTarjeta}", Request);

        TempData["TarjetaCreadaNumero"] = tarjeta.NumeroTarjeta;
        TempData["TarjetaCreadaPin"] = pin;

        return RedirectToAction("Detalle", new { id = clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(long id, string nombres, string apellidos, string? email, string? telefono)
    {
        var error = await _service.ActualizarAsync(id, nombres, apellidos, email, telefono);

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "CLIENTE_ACTUALIZADO", "/AdminCliente/Editar", true,
                $"id={id}", Request);
            TempData["Mensaje"] = "Datos del cliente actualizados.";
        }

        return RedirectToAction("Detalle", new { id });
    }
}