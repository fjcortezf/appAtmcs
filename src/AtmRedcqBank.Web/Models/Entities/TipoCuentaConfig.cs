using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_tipos_cuenta_config")]
public class TipoCuentaConfig
{
    [Key]
    [Column("id_config")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("tipo_cuenta")]
    public TipoCuenta TipoCuenta { get; set; }

    [Required]
    [Column("subtipo_cuenta")]
    public SubtipoCuenta SubtipoCuenta { get; set; }

    [Required]
    [Column("comision_mantenimiento", TypeName = "decimal(15,2)")]
    public decimal ComisionMantenimiento { get; set; }

    [Required]
    [Column("tasa_interes_anual", TypeName = "decimal(5,4)")]
    public decimal TasaInteresAnual { get; set; }

    [Required]
    [Column("max_operaciones_gratuitas")]
    public int MaxOperacionesGratuitas { get; set; }

    [Required]
    [Column("costo_exceso_operacion", TypeName = "decimal(15,2)")]
    public decimal CostoExcesoOperacion { get; set; }

    [Required]
    [Column("aplica_itf")]
    public bool AplicaItf { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}