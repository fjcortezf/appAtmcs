using System.Globalization;
using System.Text.Json;
using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class TipoCambioApiService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;

    // Códigos oficiales del BCRP
    private const string SerieCompra = "PD04637PD";
    private const string SerieVenta = "PD04638PD";

    public TipoCambioApiService(HttpClient httpClient, AppDbContext db)
    {
        _httpClient = httpClient;
        _db = db;
    }

    /// <summary>
    /// Consulta el BCRP desde el último día registrado hasta hoy y guarda los días faltantes.
    /// Devuelve la cantidad de días nuevos insertados.
    /// </summary>
    public async Task<int> CargarTipoCambiosFaltantesAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var hace30Dias = hoy.AddDays(-30);

        // 1. Obtener fechas ya registradas en los últimos 30 días
        var fechasRegistradas = await _db.TiposCambio
            .Where(t => t.Fecha >= hace30Dias && t.Fecha <= hoy)
            .Select(t => t.Fecha)
            .ToListAsync();

        // 2. Pedir al BCRP todo el rango (el BCRP ignora fechas sin datos)
        var url = $"https://estadisticas.bcrp.gob.pe/estadisticas/series/api/{SerieCompra}-{SerieVenta}/json/{hace30Dias:yyyy-MM-dd}/{hoy:yyyy-MM-dd}/ing";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"BCRP respondió {(int)response.StatusCode}: {response.ReasonPhrase}");

        var json = await response.Content.ReadAsStringAsync();

        if (!json.TrimStart().StartsWith("{"))
            throw new InvalidOperationException($"Respuesta inesperada del BCRP: {json.Substring(0, Math.Min(200, json.Length))}");

        var data = JsonSerializer.Deserialize<BcrpResponse>(json);
        if (data == null || data.Periods.Count == 0)
            return 0;

        var nuevosRegistros = 0;

        foreach (var periodo in data.Periods)
        {
            if (!TryParseFechaBcrp(periodo.Name, out var fecha))
                continue;

            if (periodo.Values.Count < 2) continue;

            var valorCompraStr = periodo.Values[0];
            var valorVentaStr = periodo.Values[1];

            if (valorCompraStr == "n.d." || valorVentaStr == "n.d.")
                continue;

            if (!decimal.TryParse(valorCompraStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var compra))
                continue;
            if (!decimal.TryParse(valorVentaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var venta))
                continue;

            // Si ya existe, saltar
            if (fechasRegistradas.Contains(fecha)) continue;

            _db.TiposCambio.Add(new TipoCambio
            {
                Fecha = fecha,
                TasaCompra = compra,
                TasaVenta = venta
            });
            nuevosRegistros++;
        }

        if (nuevosRegistros > 0)
            await _db.SaveChangesAsync();

        return nuevosRegistros;
    }
    private static bool TryParseFechaBcrp(string input, out DateOnly fecha)
    {
        fecha = default;
        // Formatos posibles: "20.Oct.26", "20.Oct.2026"
        var partes = input.Split('.');
        if (partes.Length != 3) return false;

        if (!int.TryParse(partes[0], out var dia)) return false;

        var mes = partes[1] switch
        {
            "Ene" or "Jan" => 1,
            "Feb" => 2,
            "Mar" => 3,
            "Abr" or "Apr" => 4,
            "May" => 5,
            "Jun" => 6,
            "Jul" => 7,
            "Ago" or "Aug" => 8,
            "Set" or "Sep" => 9,
            "Oct" => 10,
            "Nov" => 11,
            "Dic" or "Dec" => 12,
            _ => 0
        };
        if (mes == 0) return false;

        if (!int.TryParse(partes[2], out var anioCorto)) return false;
        var anio = anioCorto < 100 ? 2000 + anioCorto : anioCorto;

        try
        {
            fecha = new DateOnly(anio, mes, dia);
            return true;
        }
        catch
        {
            return false;
        }
    }
}