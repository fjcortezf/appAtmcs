using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminAuditoriaService
{
    private readonly AppDbContext _db;

    public AdminAuditoriaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Auditoria>> ListarAsync(
        DateTime? desde,
        DateTime? hasta,
        string? tipoActor,
        string? operacion,
        string? resultado)
    {
        var query = _db.Auditorias.AsQueryable();

        if (desde.HasValue)
            query = query.Where(a => a.Timestamp >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(a => a.Timestamp <= hasta.Value.Date.AddDays(1).AddSeconds(-1));

        if (!string.IsNullOrWhiteSpace(tipoActor))
            query = query.Where(a => a.TipoActor.ToString() == tipoActor);

        if (!string.IsNullOrWhiteSpace(operacion))
            query = query.Where(a => a.Operacion.Contains(operacion));

        if (!string.IsNullOrWhiteSpace(resultado))
            query = query.Where(a => a.Resultado == resultado);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(200)
            .ToListAsync();
    }
}