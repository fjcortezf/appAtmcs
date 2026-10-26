using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_cuentas")]
public class Cuenta
{
    [Key]
    [Column("id_cuenta")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_tarjeta")]
    public long TarjetaId { get; set; }

    [ForeignKey(nameof(TarjetaId))]
    public Tarjeta? Tarjeta { get; set; }

    [Required]
    [MaxLength(14)]
    [Column("numero_cuenta")]
    public string NumeroCuenta { get; set; } = string.Empty;

    [Required]
    [Column("moneda")]
    public Moneda Moneda { get; set; }

    [Required]
    [Column("tipo_cuenta")]
    public TipoCuenta TipoCuenta { get; set; }

    [Required]
    [Column("subtipo_cuenta")]
    public SubtipoCuenta SubtipoCuenta { get; set; }

    [Required]
    [Column("saldo", TypeName = "decimal(15,2)")]
    public decimal Saldo { get; set; }

    [Required]
    [Column("contador_operaciones_mes")]
    public int ContadorOperacionesMes { get; set; }

    [Required]
    [Column("estado")]
    public EstadoCuenta Estado { get; set; }

    [Required]
    [Column("version")]
    [ConcurrencyCheck]
    public int Version { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}