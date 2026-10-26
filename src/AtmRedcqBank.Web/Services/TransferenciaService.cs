using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class TransferenciaResult
{
    public bool Exito { get; set; }
    public string? MensajeError { get; set; }
    public decimal? MaximoTransferible { get; set; }

    public string? NumeroOperacion { get; set; }
    public string? CuentaOrigen { get; set; }
    public string? CuentaDestino { get; set; }
    public string? SimboloOrigen { get; set; }
    public string? SimboloDestino { get; set; }
    public decimal MontoOrigen { get; set; }
    public decimal MontoDestino { get; set; }
    public decimal? TipoCambioAplicado { get; set; }
    public decimal Itf { get; set; }
    public decimal Comision { get; set; }
    public decimal TotalDebitado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
}

public class TransferenciaService
{
    private readonly AppDbContext _db;
    private readonly CalculadoraFinanciera _calculadora;
    private readonly NumeroOperacionService _numeroOperacionService;
    private readonly ParametroService _parametroService;

    public TransferenciaService(AppDbContext db,
                                CalculadoraFinanciera calculadora,
                                NumeroOperacionService numeroOperacionService,
                                ParametroService parametroService)
    {
        _db = db;
        _calculadora = calculadora;
        _numeroOperacionService = numeroOperacionService;
        _parametroService = parametroService;
    }

    public async Task<TransferenciaResult> TransferirAsync(long cuentaOrigenId, long tarjetaId,
                                                           string cuentaDestino, decimal monto)
    {
        // 1) Validaciones básicas
        if (monto <= 0)
            return new TransferenciaResult { Exito = false, MensajeError = "El monto debe ser positivo." };

        if (string.IsNullOrWhiteSpace(cuentaDestino) || cuentaDestino.Length != 14)
            return new TransferenciaResult { Exito = false, MensajeError = "Cuenta destino inválida." };

        // 2) Horario
        if (!DentroDeHorarioAtencion())
            return new TransferenciaResult { Exito = false, MensajeError = "Fuera de horario de atención." };

        // 3) ATM
        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null || atm.Estado != EstadoATM.EN_SERVICIO)
            return new TransferenciaResult { Exito = false, MensajeError = "El cajero no está en servicio." };

        // 4) Buscar cuenta destino
        var destino = await _db.Cuentas.FirstOrDefaultAsync(c => c.NumeroCuenta == cuentaDestino);
        if (destino == null)
            return new TransferenciaResult { Exito = false, MensajeError = "La cuenta destino no existe." };

        if (destino.Estado != EstadoCuenta.ACTIVA)
            return new TransferenciaResult { Exito = false, MensajeError = "La cuenta destino no está activa." };

        // 5) Bloqueo en orden ascendente para evitar deadlocks
        var ids = new[] { cuentaOrigenId, destino.Id }.OrderBy(x => x).ToArray();
        
        var primera = await _db.Cuentas.FirstAsync(c => c.Id == ids[0]);
        var segunda = await _db.Cuentas.FirstAsync(c => c.Id == ids[1]);

        var origen = primera.Id == cuentaOrigenId ? primera : segunda;
        destino = primera.Id == destino.Id ? primera : segunda;

        // 6) Validaciones de origen
        if (origen.TarjetaId != tarjetaId)
            return new TransferenciaResult { Exito = false, MensajeError = "Cuenta de origen no válida." };

        if (origen.Estado != EstadoCuenta.ACTIVA)
            return new TransferenciaResult { Exito = false, MensajeError = "La cuenta de origen no está activa." };

        if (origen.Id == destino.Id)
            return new TransferenciaResult { Exito = false, MensajeError = "La cuenta de origen y destino no pueden ser la misma." };

        // 7) Tipo de cambio si monedas difieren
        decimal? tipoCambio = null;
        var mismaMoneda = origen.Moneda == destino.Moneda;
        if (!mismaMoneda)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var tc = await _db.TiposCambio.FirstOrDefaultAsync(t => t.Fecha == hoy);
            if (tc == null)
                throw new InvalidOperationException("No hay tipo de cambio registrado para hoy.");

            tipoCambio = origen.Moneda == Moneda.PEN && destino.Moneda == Moneda.USD
                ? tc.TasaVenta
                : tc.TasaCompra;
        }

        // 8) ITF + comisión
        var itf = await _calculadora.CalcularItfAsync(monto, origen.Moneda,
            origen.TipoCuenta, origen.SubtipoCuenta, origen.ContadorOperacionesMes);

        var comision = await _calculadora.CalcularComisionExcesoAsync(
            origen.TipoCuenta, origen.SubtipoCuenta, origen.ContadorOperacionesMes);

        var totalDebitado = _calculadora.Redondear(monto + itf + comision);

        // 9) Validar saldo
        if (origen.Saldo < totalDebitado)
        {
            var maximo = _calculadora.Redondear(origen.Saldo - itf - comision);
            if (maximo < 0) maximo = 0;
            return new TransferenciaResult
            {
                Exito = false,
                MensajeError = "Saldo insuficiente.",
                MaximoTransferible = maximo
            };
        }

        // 10) Monto destino (con conversión)
        decimal montoDestino = monto;
        if (!mismaMoneda)
        {
            montoDestino = origen.Moneda == Moneda.PEN && destino.Moneda == Moneda.USD
                ? Math.Round(monto / tipoCambio!.Value, 2, MidpointRounding.AwayFromZero)
                : Math.Round(monto * tipoCambio!.Value, 2, MidpointRounding.AwayFromZero);
        }

        // 11) Números de operación
        var numOpEnvio = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);
        var numOpRecibo = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);

        // 12) Debitar de origen
        var saldoAntesOrigen = origen.Saldo;
        var saldoDespuesOrigen = _calculadora.Redondear(saldoAntesOrigen - totalDebitado);
        origen.Saldo = saldoDespuesOrigen;
        origen.ContadorOperacionesMes += 1;
        _db.Cuentas.Update(origen);

        // 13) Registrar transacción ENVIADA
        var tEnvio = new Transaccion
        {
            CuentaId = origen.Id,
            AtmId = atm.Id,
            TipoOperacion = TipoOperacion.TRANSFERENCIA_ENVIADA,
            Monto = monto,
            MontoItf = itf,
            ComisionAplicada = comision,
            SaldoAnterior = saldoAntesOrigen,
            SaldoPosterior = saldoDespuesOrigen,
            NumeroOperacion = numOpEnvio,
            ItfPorcentajeAplicado = itf > 0 ? _parametroService.GetDecimal(ParametroClave.ITF_PORCENTAJE) : null,
            TipoCambioAplicado = tipoCambio,
            Fecha = DateTime.Now,
            Estado = "EXITOSA"
        };
        _db.Transacciones.Add(tEnvio);
        await _db.SaveChangesAsync();

        // 14) Acreditar en destino (con cobro de deudas)
        var saldoAntesDestino = destino.Saldo;
        var restante = montoDestino;
        var deudasCobradas = 0m;

        var pendientes = await _db.SaldosDeudores
            .Where(s => s.CuentaId == destino.Id && s.Estado == EstadoDeuda.PENDIENTE)
            .OrderBy(s => s.FechaGeneracion)
            .ToListAsync();

        foreach (var deuda in pendientes)
        {
            if (restante >= deuda.Monto)
            {
                restante -= deuda.Monto;
                deudasCobradas += deuda.Monto;
                deuda.Estado = EstadoDeuda.APLICADO;
                deuda.FechaAplicacion = DateTime.Now;
                _db.SaldosDeudores.Update(deuda);
            }
            else
            {
                break;
            }
        }

        var montoAcreditado = restante;
        var saldoDespuesDestino = _calculadora.Redondear(saldoAntesDestino + montoAcreditado);
        destino.Saldo = saldoDespuesDestino;
        _db.Cuentas.Update(destino);

        // 15) COBRO_DEUDA (antes del crédito)
        var saldoIntermedio = saldoAntesDestino;
        foreach (var deuda in pendientes)
        {
            if (deuda.Estado != EstadoDeuda.APLICADO) continue;

            var saldoAntes = saldoIntermedio;
            var saldoDespues = _calculadora.Redondear(saldoAntes - deuda.Monto);
            saldoIntermedio = saldoDespues;

            var tCobro = new Transaccion
            {
                CuentaId = destino.Id,
                AtmId = atm.Id,
                TipoOperacion = TipoOperacion.COBRO_DEUDA,
                Monto = deuda.Monto,
                MontoItf = 0,
                ComisionAplicada = 0,
                SaldoAnterior = saldoAntes,
                SaldoPosterior = saldoDespues,
                NumeroOperacion = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo),
                Fecha = DateTime.Now,
                Estado = "EXITOSA"
            };
            _db.Transacciones.Add(tCobro);
            await _db.SaveChangesAsync();

            deuda.TransaccionAplicacionId = tCobro.Id;
            _db.SaldosDeudores.Update(deuda);
        }

        // 16) TRANSFERENCIA_RECIBIDA
        var tRecibo = new Transaccion
        {
            CuentaId = destino.Id,
            AtmId = atm.Id,
            TipoOperacion = TipoOperacion.TRANSFERENCIA_RECIBIDA,
            Monto = montoDestino,
            MontoItf = 0,
            ComisionAplicada = 0,
            SaldoAnterior = saldoIntermedio,
            SaldoPosterior = saldoDespuesDestino,
            NumeroOperacion = numOpRecibo,
            TipoCambioAplicado = tipoCambio,
            Fecha = DateTime.Now,
            Estado = "EXITOSA"
        };
        _db.Transacciones.Add(tRecibo);
        await _db.SaveChangesAsync();

        // 17) Registrar transferencia
        _db.Transferencias.Add(new Transferencia
        {
            TransaccionOrigenId = tEnvio.Id,
            TransaccionDestinoId = tRecibo.Id,
            CuentaOrigen = origen.NumeroCuenta,
            CuentaDestino = destino.NumeroCuenta,
            MontoOrigen = monto,
            MontoDestino = montoDestino,
            TipoCambioAplicado = tipoCambio
        });

        await _db.SaveChangesAsync();

        return new TransferenciaResult
        {
            Exito = true,
            NumeroOperacion = numOpEnvio,
            CuentaOrigen = origen.NumeroCuenta,
            CuentaDestino = destino.NumeroCuenta,
            SimboloOrigen = origen.Moneda == Moneda.PEN ? "S/" : "$",
            SimboloDestino = destino.Moneda == Moneda.PEN ? "S/" : "$",
            MontoOrigen = monto,
            MontoDestino = montoDestino,
            TipoCambioAplicado = tipoCambio,
            Itf = itf,
            Comision = comision,
            TotalDebitado = totalDebitado,
            SaldoAnterior = saldoAntesOrigen,
            SaldoPosterior = saldoDespuesOrigen
        };
    }

    private bool DentroDeHorarioAtencion()
    {
        var ini = _parametroService.GetString(ParametroClave.HORARIO_ATENCION_INICIO);
        var fin = _parametroService.GetString(ParametroClave.HORARIO_ATENCION_FIN);
        var ahora = DateTime.Now.TimeOfDay;
        return ahora >= TimeSpan.Parse(ini) && ahora <= TimeSpan.Parse(fin);
    }
}