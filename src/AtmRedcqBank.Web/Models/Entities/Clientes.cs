using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_clientes")]
public class Cliente
{
    [Key]
    [Column("id_cliente")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("tipo_documento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("numero_documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [MaxLength(150)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("telefono")]
    public string? Telefono { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("estado")]
    public string Estado { get; set; } = "ACTIVO";

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Tarjeta> Tarjetas { get; set; } = new List<Tarjeta>();
}