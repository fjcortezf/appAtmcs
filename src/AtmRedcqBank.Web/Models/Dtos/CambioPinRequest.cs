using System.ComponentModel.DataAnnotations;

namespace AtmRedcqBank.Web.Models.Dtos;

public class CambioPinRequest
{
    [Required(ErrorMessage = "Ingrese el PIN actual")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos")]
    public string PinActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el nuevo PIN")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos")]
    public string PinNuevo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme el nuevo PIN")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El PIN debe tener 4 dígitos")]
    public string PinConfirmacion { get; set; } = string.Empty;
}