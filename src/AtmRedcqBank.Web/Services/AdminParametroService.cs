using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminParametroService
{
    private readonly AppDbContext _db;

    public AdminParametroService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Parametro>> ListarAsync()
    {
        return await _db.Parametros.OrderBy(p => p.Clave).ToListAsync();
    }

    public async Task<Parametro?> ObtenerAsync(int id)
    {
        return await _db.Parametros.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<string?> ActualizarAsync(int id, string nuevoValor, string? motivo, string emailAdmin)
    {
        var p = await _db.Parametros.FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return "Parámetro no encontrado.";

        // Validación según tipo
        var error = ValidarValor(p.TipoDato, nuevoValor);
        if (error != null) return error;

        var valorAnterior = p.Valor;
        if (valorAnterior == nuevoValor) return null; // sin cambios

        var admin = await _db.PersonalBanco.FirstOrDefaultAsync(pb => pb.Email == emailAdmin);

        _db.ParametrosHistorico.Add(new ParametroHistorico
        {
            ParametroId = p.Id,
            Clave = p.Clave,
            ValorAnterior = valorAnterior,
            ValorNuevo = nuevoValor,
            UsuarioCambioId = admin?.Id,
            Motivo = motivo,
            FechaCambio = DateTime.Now
        });

        p.Valor = nuevoValor;
        _db.Parametros.Update(p);
        await _db.SaveChangesAsync();

        return null;
    }

    private string? ValidarValor(TipoDatoParametro tipo, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "El valor no puede estar vacío.";

        switch (tipo)
        {
            case TipoDatoParametro.INT:
                if (!int.TryParse(valor, out _)) return "El valor debe ser un entero.";
                break;
            case TipoDatoParametro.DECIMAL:
                if (!decimal.TryParse(valor, out _)) return "El valor debe ser un número decimal.";
                break;
            case TipoDatoParametro.BOOLEAN:
                if (!bool.TryParse(valor, out _)) return "El valor debe ser true o false.";
                break;
            case TipoDatoParametro.STRING:
                break;
        }
        return null;
    }
}