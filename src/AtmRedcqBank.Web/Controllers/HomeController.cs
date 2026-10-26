using AtmRedcqBank.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        // Prueba temporal: contar registros
        ViewBag.TotalPersonal = _db.PersonalBanco.Count();
        ViewBag.TotalClientes = _db.Clientes.Count();
        ViewBag.TotalCuentas = _db.Cuentas.Count();
        ViewBag.TotalAtms = _db.Atms.Count();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}