using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class CalculadoraFinanciera
{
    private readonly AppDbContext _db;
    private readonly ParametroService _parametroService;

    public CalculadoraFinanciera(AppDbContext db, ParametroService parametroService)
    {
        _db = db;
        _parametroService = parametroService;
    }

    public async Task<decimal> CalcularItfAsync(decimal monto, Moneda moneda,
                                                TipoCuenta tipo, SubtipoCuenta subtipo,
                                                int contadorOperacionesMes)
    {
        var cfg = await _db.TiposCuentaConfig
            .FirstOrDefaultAsync(c => c.TipoCuenta == tipo && c.SubtipoCuenta == subtipo);

        if (cfg == null)
            throw new InvalidOperationException("Configuración de cuenta no encontrada");

        if (!cfg.AplicaItf)
            return 0.00m;

        var umbral = moneda == Moneda.PEN
            ? _parametroService.GetDecimal(ParametroClave.ITF_UMBRAL_PEN)
            : _parametroService.GetDecimal(ParametroClave.ITF_UMBRAL_USD);

        var maxSinItf = _parametroService.GetInt(ParametroClave.MAX_OPERACIONES_SIN_ITF);

        var dentroDeGratuitas = contadorOperacionesMes < maxSinItf;
        var dentroDeUmbral = monto <= umbral;

        if (dentroDeGratuitas && dentroDeUmbral)
            return 0.00m;

        var porcentaje = _parametroService.GetDecimal(ParametroClave.ITF_PORCENTAJE);
        return Redondear(monto * porcentaje);
    }

    public async Task<decimal> CalcularComisionExcesoAsync(TipoCuenta tipo, SubtipoCuenta subtipo,
                                                           int contadorOperacionesMes)
    {
        var cfg = await _db.TiposCuentaConfig
            .FirstOrDefaultAsync(c => c.TipoCuenta == tipo && c.SubtipoCuenta == subtipo);

        if (cfg == null)
            throw new InvalidOperationException("Configuración de cuenta no encontrada");

        if (contadorOperacionesMes < cfg.MaxOperacionesGratuitas)
            return 0.00m;

        return cfg.CostoExcesoOperacion;
    }

    public decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    // ============================================================
    // Cálculo del máximo retirable
    // ============================================================
    public async Task<decimal> CalcularMaximoRetirableConConversionAsync(
        decimal saldo,
        Moneda monedaCuenta,
        Moneda monedaRetiro,
        decimal? tipoCambio,
        TipoCuenta tipo,
        SubtipoCuenta subtipo,
        int contadorOperacionesMes,
        decimal limiteOperacion,
        decimal limiteDiario,
        decimal retiradoHoy,
        decimal stockAtm)
    {
        var mismaMoneda = monedaCuenta == monedaRetiro || tipoCambio == null;

        var itfEstimado = await CalcularItfAsync(saldo, monedaCuenta, tipo, subtipo, contadorOperacionesMes);
        var comisionEstimada = await CalcularComisionExcesoAsync(tipo, subtipo, contadorOperacionesMes);

        var maxPorSaldoCuenta = saldo - itfEstimado - comisionEstimada;
        if (maxPorSaldoCuenta < 0) maxPorSaldoCuenta = 0;

        var restanteDia = limiteDiario - retiradoHoy;
        if (restanteDia < 0) restanteDia = 0;

        decimal maxPorSaldoRetiro, maxPorOperacionRetiro, maxPorDiaRetiro;

        if (mismaMoneda)
        {
            maxPorSaldoRetiro = maxPorSaldoCuenta;
            maxPorOperacionRetiro = limiteOperacion;
            maxPorDiaRetiro = restanteDia;
        }
        else if (monedaCuenta == Moneda.PEN && monedaRetiro == Moneda.USD)
        {
            // Límites están en PEN, dividir por tasa venta
            maxPorSaldoRetiro = maxPorSaldoCuenta / tipoCambio!.Value;
            maxPorOperacionRetiro = limiteOperacion / tipoCambio.Value;
            maxPorDiaRetiro = restanteDia / tipoCambio.Value;
        }
        else
        {
            // Cuenta USD → retiro PEN, multiplicar por tasa compra
            maxPorSaldoRetiro = maxPorSaldoCuenta * tipoCambio!.Value;
            maxPorOperacionRetiro = limiteOperacion * tipoCambio.Value;
            maxPorDiaRetiro = restanteDia * tipoCambio.Value;
        }

        var maxPorStockRetiro = stockAtm;

        var maximo = Math.Min(
            Math.Min(maxPorSaldoRetiro, maxPorOperacionRetiro),
            Math.Min(maxPorDiaRetiro, maxPorStockRetiro));

        // Redondear hacia abajo a múltiplo de 20
        var multiplos = Math.Floor(maximo / 20);
        return Redondear(multiplos * 20);
    }
}