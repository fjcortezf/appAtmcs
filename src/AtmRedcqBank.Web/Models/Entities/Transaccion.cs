using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_transacciones")]
public class Transaccion
{
    [Key]
    [Column("id_transaccion")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_cuenta")]
    public long CuentaId { get; set; }

    [ForeignKey(nameof(CuentaId))]
    public Cuenta? Cuenta { get; set; }

    [Column("id_atm")]
    public long? AtmId { get; set; }

    [ForeignKey(nameof(AtmId))]
    public Atm? Atm { get; set; }

    [Required]
    [Column("tipo_operacion")]
    public TipoOperacion TipoOperacion { get; set; }

    [Required]
    [Column("monto", TypeName = "decimal(15,2)")]
    public decimal Monto { get; set; }

    [Required]
    [Column("monto_itf", TypeName = "decimal(15,2)")]
    public decimal MontoItf { get; set; }

    [Required]
    [Column("comision_aplicada", TypeName = "decimal(15,2)")]
    public decimal ComisionAplicada { get; set; }

    [Required]
    [Column("saldo_anterior", TypeName = "decimal(15,2)")]
    public decimal SaldoAnterior { get; set; }

    [Required]
    [Column("saldo_posterior", TypeName = "decimal(15,2)")]
    public decimal SaldoPosterior { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("numero_operacion")]
    public string NumeroOperacion { get; set; } = string.Empty;

    [Column("itf_porcentaje_aplicado", TypeName = "decimal(5,4)")]
    public decimal? ItfPorcentajeAplicado { get; set; }

    [Column("comision_porcentaje_aplicado", TypeName = "decimal(5,4)")]
    public decimal? ComisionPorcentajeAplicado { get; set; }

    [Column("tea_porcentaje_aplicado", TypeName = "decimal(5,4)")]
    public decimal? TeaPorcentajeAplicado { get; set; }

    [Column("tipo_cambio_aplicado", TypeName = "decimal(12,6)")]
    public decimal? TipoCambioAplicado { get; set; }

    [Required]
    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("estado")]
    public string Estado { get; set; } = "EXITOSA";
}