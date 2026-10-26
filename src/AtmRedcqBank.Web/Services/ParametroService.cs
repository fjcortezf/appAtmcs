using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class ParametroService
{
    private readonly AppDbContext _db;

    public ParametroService(AppDbContext db)
    {
        _db = db;
    }

    public decimal GetDecimal(ParametroClave clave)
    {
        var valor = GetValor(clave.Clave());
        return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
    }

    public int GetInt(ParametroClave clave)
    {
        var valor = GetValor(clave.Clave());
        return int.Parse(valor);
    }

    public string GetString(ParametroClave clave)
    {
        return GetValor(clave.Clave());
    }

    public bool GetBool(ParametroClave clave)
    {
        var valor = GetValor(clave.Clave());
        return bool.Parse(valor);
    }

    private string GetValor(string clave)
    {
        var parametro = _db.Parametros.AsNoTracking().FirstOrDefault(p => p.Clave == clave);
        if (parametro == null)
            throw new InvalidOperationException($"Parámetro no encontrado: {clave}");
        return parametro.Valor;
    }
}