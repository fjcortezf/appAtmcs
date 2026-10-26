using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class SistemaProcesoService
{
    private readonly AppDbContext _db;
    private readonly NumeroOperacionService _numeroOperacionService;
    private readonly ParametroService _parametroService;

    public SistemaProcesoService(AppDbContext db,
                                 NumeroOperacionService numeroOperacionService,
                                 ParametroService parametroService)
    {
        _db = db;
        _numeroOperacionService = numeroOperacionService;
        _parametroService = parametroService;
    }

    // =====================================================
    // Estado de los procesos
    // =====================================================
    public async Task<bool> MantenimientoYaEjecutadoAsync()
    {
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return await _db.Transacciones
            .AnyAsync(t => t.TipoOperacion == TipoOperacion.COBRO_MANTENIMIENTO
                        && t.Fecha >= inicioMes && t.Estado == "EXITOSA");
    }

    public async Task<bool> InteresesYaEjecutadosAsync()
    {
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return await _db.Transacciones
            .AnyAsync(t => t.TipoOperacion == TipoOperacion.ABONO_INTERES
                        && t.Fecha >= inicioMes && t.Estado == "EXITOSA");
    }

    // =====================================================
    // Mantenimiento
    // =====================================================
    public async Task<(bool exito, string mensaje)> EjecutarMantenimientoAsync()
    {
        if (await MantenimientoYaEjecutadoAsync())
            return (false, "El cobro de mantenimiento ya fue ejecutado este mes.");

        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null) return (false, "ATM no encontrado.");

        var cuentas = await _db.Cuentas
            .Where(c => c.Estado == EstadoCuenta.ACTIVA && c.SubtipoCuenta != SubtipoCuenta.FREE)
            .ToListAsync();

        int procesadas = 0;
        int conDeuda = 0;
        decimal totalCobrado = 0;

        foreach (var cuenta in cuentas)
        {
            var cfg = await _db.TiposCuentaConfig
                .FirstOrDefaultAsync(c => c.TipoCuenta == cuenta.TipoCuenta && c.SubtipoCuenta == cuenta.SubtipoCuenta);
            
            if (cfg == null || cfg.ComisionMantenimiento <= 0) continue;

            var monto = cfg.ComisionMantenimiento;

            if (cuenta.Saldo >= monto)
            {
                var saldoAntes = cuenta.Saldo;
                var saldoDespues = saldoAntes - monto;
                cuenta.Saldo = saldoDespues;
                _db.Cuentas.Update(cuenta);

                var numOp = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);

                _db.Transacciones.Add(new Transaccion
                {
                    CuentaId = cuenta.Id,
                    AtmId = atm.Id,
                    TipoOperacion = TipoOperacion.COBRO_MANTENIMIENTO,
                    Monto = monto,
                    MontoItf = 0,
                    ComisionAplicada = 0,
                    SaldoAnterior = saldoAntes,
                    SaldoPosterior = saldoDespues,
                    NumeroOperacion = numOp,
                    Fecha = DateTime.Now,
                    Estado = "EXITOSA"
                });

                totalCobrado += monto;
            }
            else
            {
                _db.SaldosDeudores.Add(new SaldoDeudor
                {
                    CuentaId = cuenta.Id,
                    TipoCargo = TipoCargo.MANTENIMIENTO,
                    Monto = monto,
                    Moneda = cuenta.Moneda,
                    Estado = EstadoDeuda.PENDIENTE,
                    FechaGeneracion = DateTime.Now,
                    Descripcion = "Mantenimiento mensual " + DateTime.Now.ToString("MM/yyyy")
                });
                conDeuda++;
            }
            procesadas++;
        }

        await _db.SaveChangesAsync();
        return (true, $"Mantenimiento ejecutado. Procesadas: {procesadas}, con deuda: {conDeuda}, cobrado: S/ {totalCobrado:N2}");
    }

    // =====================================================
    // Intereses
    // =====================================================
    public async Task<(bool exito, string mensaje)> EjecutarInteresesAsync()
    {
        if (await InteresesYaEjecutadosAsync())
            return (false, "El abono de intereses ya fue ejecutado este mes.");

        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null) return (false, "ATM no encontrado.");

        var cuentas = await _db.Cuentas
            .Where(c => c.Estado == EstadoCuenta.ACTIVA && c.SubtipoCuenta != SubtipoCuenta.FREE)
            .ToListAsync();

        int procesadas = 0;
        decimal totalAbonado = 0;

        foreach (var cuenta in cuentas)
        {
            var cfg = await _db.TiposCuentaConfig
                .FirstOrDefaultAsync(c => c.TipoCuenta == cuenta.TipoCuenta && c.SubtipoCuenta == cuenta.SubtipoCuenta);
            
            if (cfg == null || cfg.TasaInteresAnual <= 0) continue;
            if (cuenta.Saldo <= 0) continue;

            var interes = Math.Round(cuenta.Saldo * (cfg.TasaInteresAnual / 12), 2, MidpointRounding.AwayFromZero);
            if (interes <= 0) continue;

            var saldoAntes = cuenta.Saldo;
            var saldoDespues = saldoAntes + interes;
            cuenta.Saldo = saldoDespues;
            _db.Cuentas.Update(cuenta);

            var numOp = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);

            _db.Transacciones.Add(new Transaccion
            {
                CuentaId = cuenta.Id,
                AtmId = atm.Id,
                TipoOperacion = TipoOperacion.ABONO_INTERES,
                Monto = interes,
                MontoItf = 0,
                ComisionAplicada = 0,
                SaldoAnterior = saldoAntes,
                SaldoPosterior = saldoDespues,
                NumeroOperacion = numOp,
                TeaPorcentajeAplicado = cfg.TasaInteresAnual,
                Fecha = DateTime.Now,
                Estado = "EXITOSA"
            });

            totalAbonado += interes;
            procesadas++;
        }

        await _db.SaveChangesAsync();
        return (true, $"Intereses abonados. Procesadas: {procesadas}, total: S/ {totalAbonado:N2}");
    }
}