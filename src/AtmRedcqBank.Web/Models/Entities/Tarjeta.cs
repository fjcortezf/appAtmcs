using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_tarjetas")]
public class Tarjeta
{
    [Key]
    [Column("id_tarjeta")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_cliente")]
    public long ClienteId { get; set; }

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }

    [Required]
    [MaxLength(16)]
    [Column("numero_tarjeta")]
    public string NumeroTarjeta { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("pin_hash")]
    public string PinHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("pin_salt")]
    public string PinSalt { get; set; } = string.Empty;

    [Required]
    [Column("fecha_expiracion")]
    public DateOnly FechaExpiracion { get; set; }

    [Required]
    [Column("estado")]
    public EstadoTarjeta Estado { get; set; }

    [Required]
    [Column("intentos_fallidos")]
    public int IntentosFallidos { get; set; }

    [Required]
    [Column("debe_cambiar_pin")]
    public bool DebeCambiarPin { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public bool EstaVencidaPorFecha() => FechaExpiracion < DateOnly.FromDateTime(DateTime.Now);

    public bool PuedeIniciarSesion() =>
        Estado == EstadoTarjeta.ACTIVA && !EstaVencidaPorFecha();
}