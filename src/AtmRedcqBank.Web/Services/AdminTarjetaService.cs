using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminTarjetaService
{
    private readonly AppDbContext _db;
    private readonly PinHasher _pinHasher;

    public AdminTarjetaService(AppDbContext db, PinHasher pinHasher)
    {
        _db = db;
        _pinHasher = pinHasher;
    }

    public async Task<(Tarjeta? tarjeta, string? pin, string? error)> CrearAsync(long clienteId)
    {
        var cliente = await _db.Clientes
            .Include(c => c.Tarjetas)
            .FirstOrDefaultAsync(c => c.Id == clienteId);

        if (cliente == null) return (null, null, "Cliente no encontrado.");

        if (cliente.Tarjetas.Count >= 2)
            return (null, null, "El cliente ya tiene el máximo de 2 tarjetas.");

        // Generar número único
        string numeroTarjeta;
        var random = new Random();
        do
        {
            numeroTarjeta = "3004" + string.Concat(Enumerable.Range(0, 12).Select(_ => random.Next(0, 10)));
        } while (await _db.Tarjetas.AnyAsync(t => t.NumeroTarjeta == numeroTarjeta));

        // Generar PIN aleatorio
        var pin = (1000 + random.Next(9000)).ToString();
        var salt = _pinHasher.GenerarSalt();
        var hash = _pinHasher.Hashear(pin, salt);

        var tarjeta = new Tarjeta
        {
            ClienteId = cliente.Id,
            NumeroTarjeta = numeroTarjeta,
            PinHash = hash,
            PinSalt = salt,
            FechaExpiracion = DateOnly.FromDateTime(DateTime.Now.AddYears(5)),
            Estado = EstadoTarjeta.ACTIVA,
            IntentosFallidos = 0,
            DebeCambiarPin = true // Obligar a cambiar en el primer login
        };

        _db.Tarjetas.Add(tarjeta);
        await _db.SaveChangesAsync();

        return (tarjeta, pin, null);
    }
}