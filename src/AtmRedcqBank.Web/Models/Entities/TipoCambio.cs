using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_tipo_cambio")]
public class TipoCambio
{
    [Key]
    [Column("id_tipo_cambio")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Required]
    [Column("tasa_compra", TypeName = "decimal(12,6)")]
    public decimal TasaCompra { get; set; }

    [Required]
    [Column("tasa_venta", TypeName = "decimal(12,6)")]
    public decimal TasaVenta { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}