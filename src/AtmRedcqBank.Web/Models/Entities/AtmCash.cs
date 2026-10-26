using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AtmRedcqBank.Web.Models.Enums;

namespace AtmRedcqBank.Web.Models.Entities;

[Table("tb_atm_cash")]
public class AtmCash
{
    [Key]
    [Column("id_atm_cash")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [Column("id_atm")]
    public long AtmId { get; set; }

    [ForeignKey(nameof(AtmId))]
    public Atm? Atm { get; set; }

    [Required]
    [Column("moneda")]
    public Moneda Moneda { get; set; }

    [Required]
    [Column("denominacion")]
    public int Denominacion { get; set; }

    [Required]
    [Column("cantidad")]
    public int Cantidad { get; set; }
}