using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_solicitudes_tarjeta")]
public class SolicitudTarjeta
{
    [Key]
    [Column("id_solicitud")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_cliente")]
    public long ClienteId { get; set; }

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }

    [Column("id_tarjeta_anterior")]
    public long? TarjetaAnteriorId { get; set; }

    [ForeignKey(nameof(TarjetaAnteriorId))]
    public Tarjeta? TarjetaAnterior { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("motivo")]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    [Column("estado")]
    public EstadoSolicitud Estado { get; set; }

    [Required]
    [Column("fecha_solicitud")]
    public DateTime FechaSolicitud { get; set; }

    [Column("fecha_atencion")]
    public DateTime? FechaAtencion { get; set; }

    [Column("id_admin_atendio")]
    public long? AdminAtendioId { get; set; }

    [ForeignKey(nameof(AdminAtendioId))]
    public PersonalBanco? AdminAtendio { get; set; }

    public void Atender(PersonalBanco admin)
    {
        Estado = EstadoSolicitud.ATENDIDA;
        FechaAtencion = DateTime.Now;
        AdminAtendio = admin;
        AdminAtendioId = admin.Id;
    }

    public void Rechazar(PersonalBanco admin)
    {
        Estado = EstadoSolicitud.RECHAZADA;
        FechaAtencion = DateTime.Now;
        AdminAtendio = admin;
        AdminAtendioId = admin.Id;
    }
}