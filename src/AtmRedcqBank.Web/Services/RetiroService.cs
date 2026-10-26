using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Utils;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class RetiroResult
{
    public bool Exito { get; set; }
    public string? MensajeError { get; set; }
    public decimal? MaximoRetirable { get; set; }

    // Datos de éxito
    public string? NumeroOperacion { get; set; }
    public string? MonedaRetiroSimbolo { get; set; }
    public string? MonedaCuentaSimbolo { get; set; }
    public decimal MontoRetirado { get; set; }
    public decimal MontoDebitado { get; set; }
    public decimal? TipoCambioAplicado { get; set; }
    public decimal Itf { get; set; }
    public decimal Comision { get; set; }
    public decimal TotalDebitado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public Dictionary<int, int>? Desglose { get; set; }
}

public class RetiroService
{
    private readonly AppDbContext _db;
    private readonly CalculadoraFinanciera _calculadora;
    private readonly DispensadorBilletes _dispensador;
    private readonly NumeroOperacionService _numeroOperacionService;
    private readonly ParametroService _parametroService;

    public RetiroService(AppDbContext db,
                         CalculadoraFinanciera calculadora,
                         DispensadorBilletes dispensador,
                         NumeroOperacionService numeroOperacionService,
                         ParametroService parametroService)
    {
        _db = db;
        _calculadora = calculadora;
        _dispensador = dispensador;
        _numeroOperacionService = numeroOperacionService;
        _parametroService = parametroService;
    }

    public async Task<RetiroResult> RetirarAsync(long cuentaId, long tarjetaId,
                                                 decimal monto, Moneda monedaRetiro)
    {
        // 1) Validaciones básicas
        if (monto <= 0)
            return new RetiroResult { Exito = false, MensajeError = "El monto debe ser positivo." };

        if (monto % 20 != 0)
            return new RetiroResult { Exito = false, MensajeError = "El monto debe ser múltiplo de 20." };

        var limiteOperacion = _parametroService.GetDecimal(ParametroClave.LIMITE_RETIRO_OPERACION);

        // 2) Horario
        if (!DentroDeHorarioAtencion())
            return new RetiroResult { Exito = false, MensajeError = "Fuera de horario de atención." };

        // 3) ATM
        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null || atm.Estado != EstadoATM.EN_SERVICIO)
            return new RetiroResult { Exito = false, MensajeError = "El cajero no está en servicio." };

        // 4) Cuenta
        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null || cuenta.TarjetaId != tarjetaId)
            return new RetiroResult { Exito = false, MensajeError = "Cuenta no válida." };

        if (cuenta.Estado != EstadoCuenta.ACTIVA)
            return new RetiroResult { Exito = false, MensajeError = "La cuenta no está activa." };

        var monedaCuenta = cuenta.Moneda;
        var mismaMoneda = monedaCuenta == monedaRetiro;

        // 5) Tipo de cambio
        decimal? tipoCambioAplicado = null;
        if (!mismaMoneda)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var tc = await _db.TiposCambio.FirstOrDefaultAsync(t => t.Fecha == hoy);
            if (tc == null)
                throw new InvalidOperationException("No hay tipo de cambio registrado para hoy.");

            if (monedaCuenta == Moneda.PEN && monedaRetiro == Moneda.USD)
                tipoCambioAplicado = tc.TasaVenta;
            else if (monedaCuenta == Moneda.USD && monedaRetiro == Moneda.PEN)
                tipoCambioAplicado = tc.TasaCompra;
            else
                return new RetiroResult { Exito = false, MensajeError = "Conversión no soportada." };
        }

        // 6) Monto a debitar en moneda de la cuenta
        decimal montoDebitar;
        if (mismaMoneda)
            montoDebitar = monto;
        else if (monedaCuenta == Moneda.PEN && monedaRetiro == Moneda.USD)
            montoDebitar = _calculadora.Redondear(monto * tipoCambioAplicado!.Value);
        else
            montoDebitar = Math.Round(monto / tipoCambioAplicado!.Value, 2, MidpointRounding.AwayFromZero);

        // 7) Límite por operación
        var limiteEnRetiro = mismaMoneda ? limiteOperacion
            : (monedaCuenta == Moneda.PEN
                ? limiteOperacion / tipoCambioAplicado!.Value
                : limiteOperacion * tipoCambioAplicado!.Value);

        if (monto > limiteEnRetiro)
            return new RetiroResult { Exito = false, MensajeError = "El monto supera el límite por operación." };

        // 8) Límite diario + máximo retirable
        var inicioDia = DateTime.Today;
        var finDia = inicioDia.AddDays(1);
        var retiradoHoy = await _db.Transacciones
            .Where(t => t.Cuenta!.TarjetaId == tarjetaId
                     && t.TipoOperacion == TipoOperacion.RETIRO
                     && t.Fecha >= inicioDia && t.Fecha < finDia
                     && t.Estado == "EXITOSA")
            .SumAsync(t => (decimal?)t.Monto) ?? 0;

        var limiteDiario = _parametroService.GetDecimal(ParametroClave.LIMITE_RETIRO_DIARIO);
        var stockTotal = await CalcularStockTotalAtmAsync(atm.Id, monedaRetiro);

        var maximoRetirable = await _calculadora.CalcularMaximoRetirableConConversionAsync(
            cuenta.Saldo, monedaCuenta, monedaRetiro, tipoCambioAplicado,
            cuenta.TipoCuenta, cuenta.SubtipoCuenta, cuenta.ContadorOperacionesMes,
            limiteOperacion, limiteDiario, retiradoHoy, stockTotal);

        if (retiradoHoy + montoDebitar > limiteDiario)
            return new RetiroResult
            {
                Exito = false,
                MensajeError = "Superó el límite diario de retiro.",
                MaximoRetirable = maximoRetirable
            };

        // 9) ITF + comisión
        var itf = await _calculadora.CalcularItfAsync(montoDebitar, monedaCuenta,
            cuenta.TipoCuenta, cuenta.SubtipoCuenta, cuenta.ContadorOperacionesMes);

        var comision = await _calculadora.CalcularComisionExcesoAsync(
            cuenta.TipoCuenta, cuenta.SubtipoCuenta, cuenta.ContadorOperacionesMes);

        var totalDebitado = _calculadora.Redondear(montoDebitar + itf + comision);

        if (cuenta.Saldo < totalDebitado)
            return new RetiroResult
            {
                Exito = false,
                MensajeError = "Saldo insuficiente.",
                MaximoRetirable = maximoRetirable
            };

        // 10) Dispensado
        var montoInt = (int)monto;
        var stockAtm = await _db.AtmCash
            .Where(ac => ac.AtmId == atm.Id && ac.Moneda == monedaRetiro)
            .ToListAsync();

        var stockMap = stockAtm.ToDictionary(ac => ac.Denominacion, ac => ac.Cantidad);

        Dictionary<int, int> desglose;
        try
        {
            desglose = _dispensador.Calcular(montoInt, stockMap);
        }
        catch (InvalidOperationException)
        {
            var maxPorStock = stockAtm.Sum(ac => (long)ac.Denominacion * ac.Cantidad);
            var maxFinal = Math.Min(maximoRetirable, Math.Floor(maxPorStock / 20m) * 20);
            return new RetiroResult
            {
                Exito = false,
                MensajeError = "El cajero no puede dispensar ese monto.",
                MaximoRetirable = maxFinal
            };
        }

        // 11) Número de operación
        var numeroOperacion = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);

        // 12) Actualizar cuenta
        var saldoAnterior = cuenta.Saldo;
        var saldoPosterior = _calculadora.Redondear(saldoAnterior - totalDebitado);
        cuenta.Saldo = saldoPosterior;
        cuenta.ContadorOperacionesMes += 1;
        _db.Cuentas.Update(cuenta);

        // 13) Actualizar stock + movimientos
        foreach (var cash in stockAtm)
        {
            if (!desglose.TryGetValue(cash.Denominacion, out var cant) || cant == 0) continue;

            var saldoAntes = cash.Cantidad;
            var saldoDespues = saldoAntes - cant;
            cash.Cantidad = saldoDespues;
            _db.AtmCash.Update(cash);

            _db.AtmCashMovimientos.Add(new AtmCashMovimiento
            {
                AtmId = atm.Id,
                AtmCashId = cash.Id,
                TipoMovimiento = TipoMovimientoATM.DISPENSADO,
                Moneda = cash.Moneda,
                Denominacion = cash.Denominacion,
                Cantidad = -cant,
                SaldoAnterior = saldoAntes,
                SaldoPosterior = saldoDespues,
                Fecha = DateTime.Now
            });
        }

        // 14) Registrar transacción
        _db.Transacciones.Add(new Transaccion
        {
            CuentaId = cuenta.Id,
            AtmId = atm.Id,
            TipoOperacion = TipoOperacion.RETIRO,
            Monto = montoDebitar,
            MontoItf = itf,
            ComisionAplicada = comision,
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            NumeroOperacion = numeroOperacion,
            ItfPorcentajeAplicado = itf > 0
                ? _parametroService.GetDecimal(ParametroClave.ITF_PORCENTAJE)
                : null,
            TipoCambioAplicado = tipoCambioAplicado,
            Fecha = DateTime.Now,
            Estado = "EXITOSA"
        });

        await _db.SaveChangesAsync();

        // 15) Resultado
        return new RetiroResult
        {
            Exito = true,
            NumeroOperacion = numeroOperacion,
            MonedaRetiroSimbolo = monedaRetiro == Moneda.PEN ? "S/" : "$",
            MonedaCuentaSimbolo = monedaCuenta == Moneda.PEN ? "S/" : "$",
            MontoRetirado = monto,
            MontoDebitado = montoDebitar,
            TipoCambioAplicado = tipoCambioAplicado,
            Itf = itf,
            Comision = comision,
            TotalDebitado = totalDebitado,
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            Desglose = desglose
        };
    }

    private bool DentroDeHorarioAtencion()
    {
        var ini = _parametroService.GetString(ParametroClave.HORARIO_ATENCION_INICIO);
        var fin = _parametroService.GetString(ParametroClave.HORARIO_ATENCION_FIN);
        var ahora = DateTime.Now.TimeOfDay;
        var inicio = TimeSpan.Parse(ini);
        var finHora = TimeSpan.Parse(fin);
        return ahora >= inicio && ahora <= finHora;
    }

    private async Task<decimal> CalcularStockTotalAtmAsync(long atmId, Moneda moneda)
    {
        var stock = await _db.AtmCash
            .Where(ac => ac.AtmId == atmId && ac.Moneda == moneda)
            .ToListAsync();

        var total = stock.Sum(ac => (long)ac.Denominacion * ac.Cantidad);
        return Math.Floor(total / 20m) * 20;
    }
}