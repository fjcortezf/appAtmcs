using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_atms")]
public class Atm
{
    [Key]
    [Column("id_atm")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("ubicacion")]
    public string? Ubicacion { get; set; }

    [Required]
    [Column("estado")]
    public EstadoATM Estado { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}