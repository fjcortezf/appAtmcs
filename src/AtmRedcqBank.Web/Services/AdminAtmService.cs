using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminAtmService
{
    private readonly AppDbContext _db;

    public AdminAtmService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Atm?> ObtenerAtmAsync()
    {
        return await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
    }

    public async Task<List<AtmCash>> ObtenerStockAsync(long atmId)
    {
        return await _db.AtmCash
            .Where(ac => ac.AtmId == atmId)
            .OrderBy(ac => ac.Moneda)
            .ThenByDescending(ac => ac.Denominacion)
            .ToListAsync();
    }

    public async Task<List<AtmCashMovimiento>> UltimosMovimientosAsync(long atmId)
    {
        return await _db.AtmCashMovimientos
            .Include(m => m.Operador)
            .Where(m => m.AtmId == atmId)
            .OrderByDescending(m => m.Fecha)
            .Take(20)
            .ToListAsync();
    }

    public async Task<string?> CambiarEstadoAsync(long atmId, EstadoATM nuevoEstado, string emailOperador)
    {
        var atm = await _db.Atms.FirstOrDefaultAsync(a => a.Id == atmId);
        if (atm == null) return "ATM no encontrado.";

        atm.Estado = nuevoEstado;
        _db.Atms.Update(atm);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> OperarStockAsync(long atmId, Dictionary<long, int> cantidades, 
                                                bool sumar, string emailOperador)
    {
        var atm = await _db.Atms.FirstOrDefaultAsync(a => a.Id == atmId);
        if (atm == null) return "ATM no encontrado.";

        var operador = await _db.PersonalBanco.FirstOrDefaultAsync(p => p.Email == emailOperador);

        foreach (var kv in cantidades)
        {
            var cash = await _db.AtmCash.FirstOrDefaultAsync(c => c.Id == kv.Key);
            if (cash == null || kv.Value == 0) continue;

            var saldoAntes = cash.Cantidad;
            var saldoDespues = sumar ? saldoAntes + kv.Value : saldoAntes - kv.Value;

            if (saldoDespues < 0)
                return $"No hay suficientes billetes de {cash.Moneda} {cash.Denominacion}.";

            cash.Cantidad = saldoDespues;
            _db.AtmCash.Update(cash);

            _db.AtmCashMovimientos.Add(new AtmCashMovimiento
            {
                AtmId = atm.Id,
                AtmCashId = cash.Id,
                TipoMovimiento = sumar ? TipoMovimientoATM.REPOSICION : TipoMovimientoATM.RETIRO_OPERADOR,
                Moneda = cash.Moneda,
                Denominacion = cash.Denominacion,
                Cantidad = sumar ? kv.Value : -kv.Value,
                SaldoAnterior = saldoAntes,
                SaldoPosterior = saldoDespues,
                OperadorId = operador?.Id,
                Fecha = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        return null;
    }
}