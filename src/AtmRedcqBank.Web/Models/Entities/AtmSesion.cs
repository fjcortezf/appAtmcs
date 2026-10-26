using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_atm_sesiones")]
public class AtmSesion
{
    [Key]
    [Column("id_sesion")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_tarjeta")]
    public long TarjetaId { get; set; }

    [ForeignKey(nameof(TarjetaId))]
    public Tarjeta? Tarjeta { get; set; }

    [Required]
    [Column("id_atm")]
    public long AtmId { get; set; }

    [ForeignKey(nameof(AtmId))]
    public Atm? Atm { get; set; }

    [Required]
    [Column("fecha_inicio")]
    public DateTime FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    [Required]
    [Column("estado")]
    public EstadoSesionATM Estado { get; set; }

    [MaxLength(50)]
    [Column("motivo_cierre")]
    public string? MotivoCierre { get; set; }

    [MaxLength(45)]
    [Column("ip_origen")]
    public string? IpOrigen { get; set; }

    public void Cerrar(EstadoSesionATM nuevoEstado, string motivo)
    {
        FechaFin = DateTime.Now;
        Estado = nuevoEstado;
        MotivoCierre = motivo;
    }
}