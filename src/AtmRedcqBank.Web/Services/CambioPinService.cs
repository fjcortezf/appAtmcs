using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class CambioPinResult
{
    public bool Exito { get; set; }
    public string? MensajeError { get; set; }
    public string? NumeroOperacion { get; set; }

    public static CambioPinResult Error(string mensaje) => new() { Exito = false, MensajeError = mensaje };
    public static CambioPinResult Ok(string numOp) => new() { Exito = true, NumeroOperacion = numOp };
}

public class CambioPinService
{
    private readonly AppDbContext _db;
    private readonly PinHasher _pinHasher;
    private readonly NumeroOperacionService _numeroOperacionService;

    public CambioPinService(AppDbContext db,
                            PinHasher pinHasher,
                            NumeroOperacionService numeroOperacionService)
    {
        _db = db;
        _pinHasher = pinHasher;
        _numeroOperacionService = numeroOperacionService;
    }

    public async Task<CambioPinResult> CambiarAsync(long tarjetaId, long? cuentaId,
                                                    string pinActual, string pinNuevo, string pinConfirmacion)
    {
        if (pinNuevo != pinConfirmacion)
            return CambioPinResult.Error("El nuevo PIN y la confirmación no coinciden.");

        var tarjeta = await _db.Tarjetas.FirstOrDefaultAsync(t => t.Id == tarjetaId);
        if (tarjeta == null)
            return CambioPinResult.Error("Tarjeta no encontrada.");

        // Validar PIN actual
        var pinOk = _pinHasher.Verificar(pinActual, tarjeta.PinSalt, tarjeta.PinHash);
        if (!pinOk)
            return CambioPinResult.Error("El PIN actual es incorrecto.");

        // Validar que el nuevo PIN sea distinto
        var mismoPin = _pinHasher.Verificar(pinNuevo, tarjeta.PinSalt, tarjeta.PinHash);
        if (mismoPin)
            return CambioPinResult.Error("El nuevo PIN debe ser distinto al actual.");

        // Validar ATM
        var atm = await _db.Atms.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (atm == null || atm.Estado != EstadoATM.EN_SERVICIO)
            return CambioPinResult.Error("El cajero no está en servicio.");

        // Actualizar PIN
        var nuevoSalt = _pinHasher.GenerarSalt();
        var nuevoHash = _pinHasher.Hashear(pinNuevo, nuevoSalt);

        tarjeta.PinSalt = nuevoSalt;
        tarjeta.PinHash = nuevoHash;
        tarjeta.DebeCambiarPin = false;
        _db.Tarjetas.Update(tarjeta);

        // Registrar transacción si hay cuenta
        if (cuentaId != null)
        {
            var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
            if (cuenta != null)
            {
                var numOp = await _numeroOperacionService.GenerarAsync(atm.Id, atm.Codigo);
                
                _db.Transacciones.Add(new Transaccion
                {
                    CuentaId = cuenta.Id,
                    AtmId = atm.Id,
                    TipoOperacion = TipoOperacion.CAMBIO_PIN,
                    Monto = 0,
                    MontoItf = 0,
                    ComisionAplicada = 0,
                    SaldoAnterior = cuenta.Saldo,
                    SaldoPosterior = cuenta.Saldo,
                    NumeroOperacion = numOp,
                    Fecha = DateTime.Now,
                    Estado = "EXITOSA"
                });

                await _db.SaveChangesAsync();
                return CambioPinResult.Ok(numOp);
            }
        }

        await _db.SaveChangesAsync();
        return CambioPinResult.Ok("CAMBIO-PIN-" + tarjeta.NumeroTarjeta);
    }
}