using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminSolicitudService
{
    private readonly AppDbContext _db;
    private readonly PinHasher _pinHasher;

    public AdminSolicitudService(AppDbContext db, PinHasher pinHasher)
    {
        _db = db;
        _pinHasher = pinHasher;
    }

    public async Task<List<SolicitudTarjeta>> ListarAsync()
    {
        return await _db.SolicitudesTarjeta
            .Include(s => s.Cliente)
            .Include(s => s.TarjetaAnterior)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();
    }

    public async Task<long> ContarPendientesAsync()
    {
        return await _db.SolicitudesTarjeta.CountAsync(s => s.Estado == EstadoSolicitud.PENDIENTE);
    }

    public async Task<(SolicitudTarjeta? solicitud, string? nuevoNumero, string? nuevoPin, int cuentasReasociadas, string? error)> AtenderAsync(long solicitudId, string emailAdmin)
    {
        var solicitud = await _db.SolicitudesTarjeta
            .Include(s => s.Cliente)
            .Include(s => s.TarjetaAnterior)
            .FirstOrDefaultAsync(s => s.Id == solicitudId);

        if (solicitud == null) return (null, null, null, 0, "Solicitud no encontrada.");
        if (solicitud.Estado != EstadoSolicitud.PENDIENTE) return (null, null, null, 0, "La solicitud ya fue atendida.");

        var admin = await _db.PersonalBanco.FirstOrDefaultAsync(p => p.Email == emailAdmin);

        // 1. Crear nueva tarjeta
        var random = new Random();
        string numeroTarjeta;
        do
        {
            numeroTarjeta = "3004" + string.Concat(Enumerable.Range(0, 12).Select(_ => random.Next(0, 10)));
        } while (await _db.Tarjetas.AnyAsync(t => t.NumeroTarjeta == numeroTarjeta));

        var pin = (1000 + random.Next(9000)).ToString();
        var salt = _pinHasher.GenerarSalt();
        var hash = _pinHasher.Hashear(pin, salt);

        var nuevaTarjeta = new Tarjeta
        {
            ClienteId = solicitud.ClienteId,
            NumeroTarjeta = numeroTarjeta,
            PinHash = hash,
            PinSalt = salt,
            FechaExpiracion = DateOnly.FromDateTime(DateTime.Now.AddYears(5)),
            Estado = EstadoTarjeta.ACTIVA,
            IntentosFallidos = 0,
            DebeCambiarPin = true
        };
        _db.Tarjetas.Add(nuevaTarjeta);
        await _db.SaveChangesAsync();

        // 2. Reasociar cuentas
        int cuentasReasociadas = 0;
        if (solicitud.TarjetaAnteriorId != null)
        {
            var cuentasViejas = await _db.Cuentas
                .Where(c => c.TarjetaId == solicitud.TarjetaAnteriorId)
                .ToListAsync();

            foreach (var cuenta in cuentasViejas)
            {
                cuenta.TarjetaId = nuevaTarjeta.Id;
                _db.Cuentas.Update(cuenta);
                cuentasReasociadas++;
            }
        }

        // 3. Marcar solicitud como atendida
        solicitud.Estado = EstadoSolicitud.ATENDIDA;
        solicitud.FechaAtencion = DateTime.Now;
        solicitud.AdminAtendioId = admin?.Id;
        _db.SolicitudesTarjeta.Update(solicitud);

        await _db.SaveChangesAsync();

        return (solicitud, nuevaTarjeta.NumeroTarjeta, pin, cuentasReasociadas, null);
    }

    public async Task<string?> RechazarAsync(long solicitudId, string emailAdmin)
    {
        var solicitud = await _db.SolicitudesTarjeta.FirstOrDefaultAsync(s => s.Id == solicitudId);
        if (solicitud == null) return "Solicitud no encontrada.";
        if (solicitud.Estado != EstadoSolicitud.PENDIENTE) return "La solicitud ya fue procesada.";

        var admin = await _db.PersonalBanco.FirstOrDefaultAsync(p => p.Email == emailAdmin);

        solicitud.Estado = EstadoSolicitud.RECHAZADA;
        solicitud.FechaAtencion = DateTime.Now;
        solicitud.AdminAtendioId = admin?.Id;
        _db.SolicitudesTarjeta.Update(solicitud);

        await _db.SaveChangesAsync();
        return null;
    }
}