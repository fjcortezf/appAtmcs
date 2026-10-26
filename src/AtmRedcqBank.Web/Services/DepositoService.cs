using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class DepositoResult
{
    public bool Exito { get; set; }
    public string? MensajeError { get; set; }

    public string? NumeroOperacion { get; set; }
    public string? Simbolo { get; set; }
    public decimal MontoDepositado { get; set; }
    public decimal DeudasCobradas { get; set; }
    public decimal MontoAcreditado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public List<DeudaDetalle> DeudasDetalle { get; set; } = new();

    public class DeudaDetalle
    {
        public string TipoCargo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }
}

public class DepositoService
{
    private readonly AppDbContext _db;
    private readonly NumeroOperacionService _numeroOperacionService;
    private readonly ParametroService _parametroService;

    public DepositoService(AppDbContext db,
                           NumeroOperacionService numeroOperacionService,
                           ParametroService parametroService)
    {
        _db = db;
        _numeroOperacionService = numeroOperacionService;
        _parametroService = parametroService;
    }

    public async Task<DepositoResult> DepositarAsync(long cuentaId, long tarjetaId, decimal monto)
    {
        // 1) Validaciones básicas
        if (monto <= 0)
            return new DepositoResult { Exito = false, MensajeError = "El monto debe ser positivo." };

        if (monto % 20 != 0)
            return new DepositoResult { Exito = false, MensajeError = "El monto debe ser múltiplo de 20." };

        // 2) Horario
        if (!DentroDeHorarioAtencion())
            return new DepositoResult { Exito = false, MensajeError = "Fuera de horario de atención." };

        // 3) ATM
        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null || atm.Estado != EstadoATM.EN_SERVICIO)
            return new DepositoResult { Exito = false, MensajeError = "El cajero no está en servicio." };

        // 4) Cuenta
        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null || cuenta.TarjetaId != tarjetaId)
            return new DepositoResult { Exito = false, MensajeError = "Cuenta no válida." };

        if (cuenta.Estado != EstadoCuenta.ACTIVA)
            return new DepositoResult { Exito = false, MensajeError = "La cuenta no está activa." };

        var saldoAnterior = cuenta.Saldo;
        var numeroOperacion = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);

        // 5) Cobrar deudas pendientes por antigüedad
        var restante = monto;
        var deudasCobradas = 0m;
        var deudasDetalle = new List<DepositoResult.DeudaDetalle>();

        var pendientes = await _db.SaldosDeudores
            .Where(s => s.CuentaId == cuentaId && s.Estado == EstadoDeuda.PENDIENTE)
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

                deudasDetalle.Add(new DepositoResult.DeudaDetalle
                {
                    TipoCargo = deuda.TipoCargo.ToString(),
                    Monto = deuda.Monto
                });
            }
            else
            {
                break;
            }
        }

        var montoAcreditado = restante;

        // 6) Acreditar al saldo
        var saldoPosterior = saldoAnterior + montoAcreditado;
        cuenta.Saldo = saldoPosterior;
        // Depósito NO incrementa contador_operaciones_mes
        _db.Cuentas.Update(cuenta);

        // 7) Registrar COBRO_DEUDA (antes del depósito)
        var saldoIntermedio = saldoAnterior;
        foreach (var deuda in pendientes)
        {
            if (deuda.Estado != EstadoDeuda.APLICADO) continue;

            var saldoAntes = saldoIntermedio;
            var saldoDespues = saldoAntes - deuda.Monto;
            saldoIntermedio = saldoDespues;

            var tCobro = new Transaccion
            {
                CuentaId = cuenta.Id,
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

        // 8) Registrar DEPOSITO
        _db.Transacciones.Add(new Transaccion
        {
            CuentaId = cuenta.Id,
            AtmId = atm.Id,
            TipoOperacion = TipoOperacion.DEPOSITO,
            Monto = monto,
            MontoItf = 0,
            ComisionAplicada = 0,
            SaldoAnterior = saldoIntermedio,
            SaldoPosterior = saldoPosterior,
            NumeroOperacion = numeroOperacion,
            Fecha = DateTime.Now,
            Estado = "EXITOSA"
        });

        await _db.SaveChangesAsync();

        return new DepositoResult
        {
            Exito = true,
            NumeroOperacion = numeroOperacion,
            Simbolo = cuenta.Moneda == Moneda.PEN ? "S/" : "$",
            MontoDepositado = monto,
            DeudasCobradas = deudasCobradas,
            MontoAcreditado = montoAcreditado,
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            DeudasDetalle = deudasDetalle
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