using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_parametros")]
public class Parametro
{
    [Key]
    [Column("id_parametro")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    [Column("clave")]
    public string Clave { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("valor")]
    public string Valor { get; set; } = string.Empty;

    [Required]
    [Column("tipo_dato")]
    public TipoDatoParametro TipoDato { get; set; }

    [MaxLength(300)]
    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}