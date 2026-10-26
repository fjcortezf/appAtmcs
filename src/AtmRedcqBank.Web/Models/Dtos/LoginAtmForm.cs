using System.ComponentModel.DataAnnotations;

namespace AtmRedcqBank.Web.Models.Dtos;

public class LoginAtmForm
{
    [Required(ErrorMessage = "Ingrese el número de tarjeta")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "La tarjeta debe tener 16 dígitos")]
    public string NumeroTarjeta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el PIN")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos")]
    public string Pin { get; set; } = string.Empty;
}