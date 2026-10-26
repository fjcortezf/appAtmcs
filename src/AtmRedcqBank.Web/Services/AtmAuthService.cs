using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public enum EstadoLoginAtm
{
    EXITO,
    TARJETA_NO_EXISTE,
    TARJETA_BLOQUEADA,
    TARJETA_VENCIDA,
    PIN_INCORRECTO,
    TARJETA_BLOQUEADA_AHORA
}

public class LoginAtmResult
{
    public EstadoLoginAtm Estado { get; set; }
    public long? TarjetaId { get; set; }
    public long? ClienteId { get; set; }
    public string? Mensaje { get; set; }
    public bool EsExito => Estado == EstadoLoginAtm.EXITO;
}

public class AtmAuthService
{
    private const int MaxIntentos = 3;
    private const string MensajeGenerico = "Tarjeta o PIN incorrectos.";

    private readonly AppDbContext _db;
    private readonly PinHasher _pinHasher;

    public AtmAuthService(AppDbContext db, PinHasher pinHasher)
    {
        _db = db;
        _pinHasher = pinHasher;
    }

    public async Task<LoginAtmResult> AutenticarAsync(LoginAtmForm form)
    {
        var tarjeta = await _db.Tarjetas
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.NumeroTarjeta == form.NumeroTarjeta);

        if (tarjeta == null)
            return new LoginAtmResult
            {
                Estado = EstadoLoginAtm.TARJETA_NO_EXISTE,
                Mensaje = MensajeGenerico
            };

        // Validar estado
        if (tarjeta.Estado == EstadoTarjeta.BLOQUEADA)
            return new LoginAtmResult
            {
                Estado = EstadoLoginAtm.TARJETA_BLOQUEADA,
                Mensaje = "Tarjeta bloqueada. Solicite renovación en el banco."
            };

        if (tarjeta.Estado == EstadoTarjeta.VENCIDA || tarjeta.EstaVencidaPorFecha())
        {
            if (tarjeta.Estado != EstadoTarjeta.VENCIDA)
            {
                tarjeta.Estado = EstadoTarjeta.VENCIDA;
                _db.Tarjetas.Update(tarjeta);
                await _db.SaveChangesAsync();
                await CrearSolicitudRenovacionAsync(tarjeta, "VENCIMIENTO");
            }

            return new LoginAtmResult
            {
                Estado = EstadoLoginAtm.TARJETA_VENCIDA,
                Mensaje = "Tarjeta vencida. Solicite renovación en el banco."
            };
        }

        // Validar PIN
        var pinOk = _pinHasher.Verificar(form.Pin, tarjeta.PinSalt, tarjeta.PinHash);

        if (!pinOk)
        {
            tarjeta.IntentosFallidos++;

            if (tarjeta.IntentosFallidos >= MaxIntentos)
            {
                tarjeta.Estado = EstadoTarjeta.BLOQUEADA;
                _db.Tarjetas.Update(tarjeta);
                await _db.SaveChangesAsync();
                await CrearSolicitudRenovacionAsync(tarjeta, "BLOQUEO");

                return new LoginAtmResult
                {
                    Estado = EstadoLoginAtm.TARJETA_BLOQUEADA_AHORA,
                    Mensaje = "Tarjeta bloqueada por seguridad. Solicite renovación en el banco."
                };
            }

            _db.Tarjetas.Update(tarjeta);
            await _db.SaveChangesAsync();

            var restantes = MaxIntentos - tarjeta.IntentosFallidos;
            return new LoginAtmResult
            {
                Estado = EstadoLoginAtm.PIN_INCORRECTO,
                Mensaje = MensajeGenerico + $" Intentos restantes: {restantes}"
            };
        }

        // Login OK
        tarjeta.IntentosFallidos = 0;
        _db.Tarjetas.Update(tarjeta);
        await _db.SaveChangesAsync();

        return new LoginAtmResult
        {
            Estado = EstadoLoginAtm.EXITO,
            TarjetaId = tarjeta.Id,
            ClienteId = tarjeta.ClienteId
        };
    }

    private async Task CrearSolicitudRenovacionAsync(Tarjeta tarjeta, string motivo)
    {
        var yaExiste = await _db.SolicitudesTarjeta
            .AnyAsync(s => s.Estado == EstadoSolicitud.PENDIENTE
                        && s.TarjetaAnteriorId == tarjeta.Id);

        if (yaExiste) return;

        _db.SolicitudesTarjeta.Add(new SolicitudTarjeta
        {
            ClienteId = tarjeta.ClienteId,
            TarjetaAnteriorId = tarjeta.Id,
            Motivo = motivo,
            Estado = EstadoSolicitud.PENDIENTE,
            FechaSolicitud = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    public async Task<long> CrearSesionAsync(long tarjetaId, string? ip)
    {
        var atm = await _db.Atms.FirstAsync();
        var sesion = new AtmSesion
        {
            TarjetaId = tarjetaId,
            AtmId = atm.Id,
            FechaInicio = DateTime.Now,
            Estado = EstadoSesionATM.ACTIVA,
            IpOrigen = ip
        };
        _db.AtmSesiones.Add(sesion);
        await _db.SaveChangesAsync();
        return sesion.Id;
    }

    public async Task CerrarSesionAsync(long sesionId, EstadoSesionATM estado, string motivo)
    {
        var sesion = await _db.AtmSesiones.FindAsync(sesionId);
        if (sesion != null)
        {
            sesion.FechaFin = DateTime.Now;
            sesion.Estado = estado;
            sesion.MotivoCierre = motivo;
            await _db.SaveChangesAsync();
        }
    }
}