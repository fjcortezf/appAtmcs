using System.ComponentModel.DataAnnotations;

namespace AtmRedcqBank.Web.Models.Dtos;

public class DepositoRequest
{
    [Required(ErrorMessage = "Ingrese el monto")]
    [Range(20, 100000, ErrorMessage = "El monto debe estar entre 20 y 100000")]
    public decimal Monto { get; set; }
}