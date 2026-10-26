using System.ComponentModel.DataAnnotations;

namespace AtmRedcqBank.Web.Models.Dtos;

public class ClienteCrearForm
{
    [Required(ErrorMessage = "Ingrese los nombres")]
    [MaxLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese los apellidos")]
    [MaxLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione el tipo de documento")]
    public string TipoDocumento { get; set; } = "DNI";

    [Required(ErrorMessage = "Ingrese el número de documento")]
    [MaxLength(20)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }
}