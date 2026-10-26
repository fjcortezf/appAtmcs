using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_auditoria")]
public class Auditoria
{
    [Key]
    [Column("id_auditoria")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("tipo_actor")]
    public TipoActor TipoActor { get; set; }

    [Column("id_actor")]
    public long? IdActor { get; set; }

    [MaxLength(150)]
    [Column("email_actor")]
    public string? EmailActor { get; set; }

    [Required]
    [MaxLength(60)]
    [Column("operacion")]
    public string Operacion { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("recurso")]
    public string? Recurso { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("resultado")]
    public string Resultado { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("detalle")]
    public string? Detalle { get; set; }

    [MaxLength(45)]
    [Column("ip")]
    public string? Ip { get; set; }

    [MaxLength(300)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
}