using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_saldos_deudores")]
public class SaldoDeudor
{
    [Key]
    [Column("id_deuda")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_cuenta")]
    public long CuentaId { get; set; }

    [ForeignKey(nameof(CuentaId))]
    public Cuenta? Cuenta { get; set; }

    [Required]
    [Column("tipo_cargo")]
    public TipoCargo TipoCargo { get; set; }

    [Required]
    [Column("monto", TypeName = "decimal(15,2)")]
    public decimal Monto { get; set; }

    [Required]
    [Column("moneda")]
    public Moneda Moneda { get; set; }

    [Required]
    [Column("estado")]
    public EstadoDeuda Estado { get; set; }

    [Required]
    [Column("fecha_generacion")]
    public DateTime FechaGeneracion { get; set; }

    [Column("fecha_aplicacion")]
    public DateTime? FechaAplicacion { get; set; }

    [Column("id_transaccion_aplicacion")]
    public long? TransaccionAplicacionId { get; set; }

    [ForeignKey(nameof(TransaccionAplicacionId))]
    public Transaccion? TransaccionAplicacion { get; set; }

    [MaxLength(300)]
    [Column("descripcion")]
    public string? Descripcion { get; set; }
}