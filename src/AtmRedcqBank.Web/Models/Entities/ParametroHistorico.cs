using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_parametros_historico")]
public class ParametroHistorico
{
    [Key]
    [Column("id_historico")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_parametro")]
    public int ParametroId { get; set; }

    [ForeignKey(nameof(ParametroId))]
    public Parametro? Parametro { get; set; }

    [Required]
    [MaxLength(60)]
    [Column("clave")]
    public string Clave { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("valor_anterior")]
    public string? ValorAnterior { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("valor_nuevo")]
    public string ValorNuevo { get; set; } = string.Empty;

    [Column("id_usuario_cambio")]
    public long? UsuarioCambioId { get; set; }

    [ForeignKey(nameof(UsuarioCambioId))]
    public PersonalBanco? UsuarioCambio { get; set; }

    [MaxLength(300)]
    [Column("motivo")]
    public string? Motivo { get; set; }

    [Required]
    [Column("fecha_cambio")]
    public DateTime FechaCambio { get; set; }
}