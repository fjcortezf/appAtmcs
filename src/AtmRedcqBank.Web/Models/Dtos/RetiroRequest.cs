using System.ComponentModel.DataAnnotations;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Dtos;

public class RetiroRequest
{
    [Required(ErrorMessage = "Ingrese el monto")]
    [Range(20, 1000, ErrorMessage = "El monto debe estar entre 20 y 1000")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Seleccione la moneda del retiro")]
    public Moneda MonedaRetiro { get; set; } = Moneda.PEN;
}