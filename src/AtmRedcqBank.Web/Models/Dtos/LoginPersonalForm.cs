using System.ComponentModel.DataAnnotations;
namespace AtmRedcqBank.Web.Models.Dtos;
public class LoginPersonalForm
{
    [Required(ErrorMessage = "Ingrese el email")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la contraseña")]
    public string Password { get; set; } = string.Empty;
}