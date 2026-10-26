using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class NumeroOperacionService
{
    private readonly AppDbContext _db;

    public NumeroOperacionService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Genera el siguiente número de operación del día para el ATM indicado.
    /// Debe ejecutarse dentro de una transacción abierta.
    /// </summary>
    public async Task<string> GenerarAsync(long atmId, string atmCodigo)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var contador = await _db.ContadoresOperacion
            .FirstOrDefaultAsync(c => c.AtmId == atmId && c.Fecha == hoy);

        if (contador == null)
        {
            contador = new ContadorOperacion
            {
                AtmId = atmId,
                Fecha = hoy,
                UltimoNumero = 0
            };
            _db.ContadoresOperacion.Add(contador);
        }

        contador.UltimoNumero += 1;
        await _db.SaveChangesAsync();

        return $"{hoy:yyyyMMdd}-{atmCodigo}-{contador.UltimoNumero:D6}";
    }
}