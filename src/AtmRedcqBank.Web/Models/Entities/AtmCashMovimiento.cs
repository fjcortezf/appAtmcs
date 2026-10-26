using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_atm_cash_movimientos")]
public class AtmCashMovimiento
{
    [Key]
    [Column("id_movimiento")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_atm")]
    public long AtmId { get; set; }

    [ForeignKey(nameof(AtmId))]
    public Atm? Atm { get; set; }

    [Column("id_atm_cash")]
    public long? AtmCashId { get; set; }

    [ForeignKey(nameof(AtmCashId))]
    public AtmCash? AtmCash { get; set; }

    [Required]
    [Column("tipo_movimiento")]
    public TipoMovimientoATM TipoMovimiento { get; set; }

    [Required]
    [Column("moneda")]
    public Moneda Moneda { get; set; }

    [Required]
    [Column("denominacion")]
    public int Denominacion { get; set; }

    [Required]
    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("id_operador")]
    public long? OperadorId { get; set; }

    [ForeignKey(nameof(OperadorId))]
    public PersonalBanco? Operador { get; set; }

    [Column("id_transaccion")]
    public long? TransaccionId { get; set; }

    [ForeignKey(nameof(TransaccionId))]
    public Transaccion? Transaccion { get; set; }

    [Required]
    [Column("saldo_anterior")]
    public int SaldoAnterior { get; set; }

    [Required]
    [Column("saldo_posterior")]
    public int SaldoPosterior { get; set; }

    [Required]
    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [MaxLength(300)]
    [Column("observacion")]
    public string? Observacion { get; set; }
}