using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_contadores_operacion")]
public class ContadorOperacion
{
    [Key]
    [Column("id_contador")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("id_atm")]
    public long AtmId { get; set; }

    [ForeignKey(nameof(AtmId))]
    public Atm? Atm { get; set; }

    [Required]
    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Required]
    [Column("ultimo_numero")]
    public int UltimoNumero { get; set; }

    public int Siguiente()
    {
        UltimoNumero += 1;
        return UltimoNumero;
    }
}