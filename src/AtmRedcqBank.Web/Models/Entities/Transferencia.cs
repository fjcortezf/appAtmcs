using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_transferencias")]
public class Transferencia
{
    [Key]
    [Column("id_transferencia")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_transaccion_origen")]
    public long TransaccionOrigenId { get; set; }

    [ForeignKey(nameof(TransaccionOrigenId))]
    public Transaccion? TransaccionOrigen { get; set; }

    [Required]
    [Column("id_transaccion_destino")]
    public long TransaccionDestinoId { get; set; }

    [ForeignKey(nameof(TransaccionDestinoId))]
    public Transaccion? TransaccionDestino { get; set; }

    [Required]
    [MaxLength(14)]
    [Column("cuenta_origen")]
    public string CuentaOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(14)]
    [Column("cuenta_destino")]
    public string CuentaDestino { get; set; } = string.Empty;

    [Required]
    [Column("monto_origen", TypeName = "decimal(15,2)")]
    public decimal MontoOrigen { get; set; }

    [Required]
    [Column("monto_destino", TypeName = "decimal(15,2)")]
    public decimal MontoDestino { get; set; }

    [Column("tipo_cambio_aplicado", TypeName = "decimal(12,6)")]
    public decimal? TipoCambioAplicado { get; set; }
}