using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtmRedcqBank.Web.Controllers;

[Authorize]
public class AdminHomeController : Controller
{
    public IActionResult Index()
    {
        // Verificamos los roles del usuario logueado
        ViewBag.EsAdmin = User.IsInRole("ADMIN");
        ViewBag.EsSistemas = User.IsInRole("SISTEMAS");
        ViewBag.EsOperador = User.IsInRole("OPERADOR_ATM");
        return View();
    }
}