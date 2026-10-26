using System.ComponentModel.DataAnnotations;

namespace AtmRedcqBank.Web.Models.Dtos;

public class TransferenciaRequest
{
    [Required]
    public string Tipo { get; set; } = "PROPIA";

    public string? CuentaDestino { get; set; }

    [Required(ErrorMessage = "Ingrese el monto")]
    [Range(0.01, 100000, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Monto { get; set; }

    public long? CuentaDestinoId { get; set; }
}