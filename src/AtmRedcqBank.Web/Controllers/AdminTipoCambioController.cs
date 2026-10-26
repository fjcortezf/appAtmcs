using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminTipoCambioController : Controller
{
    private readonly AppDbContext _db;
    private readonly TipoCambioApiService _apiService;
    private readonly AuditoriaService _auditoriaService;

    public AdminTipoCambioController(AppDbContext db,
                                     TipoCambioApiService apiService,
                                     AuditoriaService auditoriaService)
    {
        _db = db;
        _apiService = apiService;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        ViewBag.TipoCambioHoy = await _db.TiposCambio.FirstOrDefaultAsync(t => t.Fecha == hoy);
        
        var historico = await _db.TiposCambio
            .OrderByDescending(t => t.Fecha)
            .Take(30)
            .ToListAsync();
            
        return View(historico);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CargarBcrp()
    {
        try
        {
            var cantidad = await _apiService.CargarTipoCambiosFaltantesAsync();
            
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "TIPO_CAMBIO_CARGA_BCRP", "/AdminTipoCambio/CargarBcrp", true,
                $"Días cargados: {cantidad}", Request);

            if (cantidad == 0)
                TempData["Mensaje"] = "No hay días nuevos por cargar. Ya está al día.";
            else
                TempData["Mensaje"] = $"Se cargaron {cantidad} día(s) de tipo de cambio desde el BCRP.";
        }
        catch (Exception ex)
        {
            await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
                "TIPO_CAMBIO_CARGA_BCRP", "/AdminTipoCambio/CargarBcrp", false,
                ex.Message, Request);

            TempData["Error"] = "Error al conectar con el BCRP: " + ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarManual(decimal tasaCompra, decimal tasaVenta)
    {
        if (tasaCompra <= 0 || tasaVenta <= 0)
        {
            TempData["Error"] = "Las tasas deben ser mayores a 0.";
            return RedirectToAction("Index");
        }
        if (tasaCompra >= tasaVenta)
        {
            TempData["Error"] = "La tasa de compra debe ser menor que la tasa de venta.";
            return RedirectToAction("Index");
        }

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var existente = await _db.TiposCambio.FirstOrDefaultAsync(t => t.Fecha == hoy);

        if (existente != null)
        {
            existente.TasaCompra = tasaCompra;
            existente.TasaVenta = tasaVenta;
            _db.TiposCambio.Update(existente);
        }
        else
        {
            _db.TiposCambio.Add(new TipoCambio
            {
                Fecha = hoy,
                TasaCompra = tasaCompra,
                TasaVenta = tasaVenta
            });
        }
        await _db.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(TipoActor.PERSONAL, null, User.Identity?.Name,
            "TIPO_CAMBIO_REGISTRADO", "/AdminTipoCambio/RegistrarManual", true,
            $"compra={tasaCompra} venta={tasaVenta}", Request);

        TempData["Mensaje"] = "Tipo de cambio actualizado correctamente.";
        return RedirectToAction("Index");
    }
}