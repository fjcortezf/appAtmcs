using AtmRedcqBank.Web.Config;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using AtmRedcqBank.Web.Security;
using Microsoft.Extensions.Options;

namespace AtmRedcqBank.Web.Data;

public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly PinHasher _pinHasher;
    private readonly AppSettings _settings;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext db,
                      PasswordHasher passwordHasher,
                      PinHasher pinHasher,
                      IOptions<AppSettings> settings,
                      ILogger<DataSeeder> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _pinHasher = pinHasher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedPersonalAsync();
        await SeedClienteDemoAsync();
        await _db.SaveChangesAsync();
        _logger.LogInformation("DataSeeder finalizado.");
    }

    // =====================================================
    // Personal del banco
    // =====================================================
    private async Task SeedPersonalAsync()
    {
        await CrearPersonalSiNoExisteAsync("Admin", "Banco",
            "admin@banco.com", "Fcf160575@na", Rol.ADMIN);

        await CrearPersonalSiNoExisteAsync("Sistemas", "Banco",
            "sistemas@banco.com", "Sis160575@na", Rol.SISTEMAS);

        await CrearPersonalSiNoExisteAsync("Operador", "ATM",
            "operador_atm@banco.com", "Ope160575@na", Rol.OPERADOR_ATM);
    }

    private async Task CrearPersonalSiNoExisteAsync(string nombres, string apellidos,
                                                     string email, string password, Rol rol)
    {
        if (_db.PersonalBanco.Any(p => p.Email == email)) return;

        var personal = new PersonalBanco
        {
            Nombres = nombres,
            Apellidos = apellidos,
            Email = email,
            PasswordHash = _passwordHasher.Hashear(password),
            Rol = rol,
            Estado = "ACTIVO"
        };
        _db.PersonalBanco.Add(personal);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Personal creado: {Email} ({Rol})", email, rol);
    }

    // =====================================================
    // Cliente demo
    // =====================================================
    private async Task SeedClienteDemoAsync()
    {
        const string tipoDoc = "DNI";
        const string numDoc = "12345678";

        if (_db.Clientes.Any(c => c.TipoDocumento == tipoDoc && c.NumeroDocumento == numDoc))
            return;

        // Crear cliente
        var cliente = new Cliente
        {
            Nombres = "Juan Carlos",
            Apellidos = "Pérez Gómez",
            TipoDocumento = tipoDoc,
            NumeroDocumento = numDoc,
            Email = "juan.perez@example.com",
            Telefono = "999888777",
            Estado = "ACTIVO"
        };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        // Crear tarjeta con PIN 5215
        var numeroTarjeta = GenerarNumeroTarjeta();
        var salt = _pinHasher.GenerarSalt();
        var pinHash = _pinHasher.Hashear("5215", salt);

        var tarjeta = new Tarjeta
        {
            ClienteId = cliente.Id,
            NumeroTarjeta = numeroTarjeta,
            PinHash = pinHash,
            PinSalt = salt,
            FechaExpiracion = DateOnly.FromDateTime(DateTime.Now.AddYears(5)),
            Estado = EstadoTarjeta.ACTIVA,
            IntentosFallidos = 0,
            DebeCambiarPin = false
        };
        _db.Tarjetas.Add(tarjeta);
        await _db.SaveChangesAsync();

        // Cuenta PEN Ahorros Normal
        _db.Cuentas.Add(new Cuenta
        {
            TarjetaId = tarjeta.Id,
            NumeroCuenta = GenerarNumeroCuenta(Moneda.PEN, TipoCuenta.AHORROS, SubtipoCuenta.NORMAL),
            Moneda = Moneda.PEN,
            TipoCuenta = TipoCuenta.AHORROS,
            SubtipoCuenta = SubtipoCuenta.NORMAL,
            Saldo = 1000.00m,
            ContadorOperacionesMes = 0,
            Estado = EstadoCuenta.ACTIVA,
            Version = 0
        });

        // Cuenta USD Ahorros Free
        _db.Cuentas.Add(new Cuenta
        {
            TarjetaId = tarjeta.Id,
            NumeroCuenta = GenerarNumeroCuenta(Moneda.USD, TipoCuenta.AHORROS, SubtipoCuenta.FREE),
            Moneda = Moneda.USD,
            TipoCuenta = TipoCuenta.AHORROS,
            SubtipoCuenta = SubtipoCuenta.FREE,
            Saldo = 500.00m,
            ContadorOperacionesMes = 0,
            Estado = EstadoCuenta.ACTIVA,
            Version = 0
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Cliente demo creado: {Nombres} {Apellidos} | Tarjeta: {Tarjeta} | PIN: 5215",
            cliente.Nombres, cliente.Apellidos, numeroTarjeta);
    }

    // =====================================================
    // Helpers
    // =====================================================
    private static string GenerarNumeroTarjeta()
    {
        var random = new Random();
        var digitos = string.Concat(Enumerable.Range(0, 12)
            .Select(_ => random.Next(0, 10)));
        return "3004" + digitos;
    }

    private static string GenerarNumeroCuenta(Moneda moneda, TipoCuenta tipo, SubtipoCuenta subtipo)
    {
        var random = new Random();
        var correlativo = string.Concat(Enumerable.Range(0, 6)
            .Select(_ => random.Next(0, 10)));

        var codigoMoneda = moneda == Moneda.PEN ? "0" : "1";
        var codigoTipo = tipo == TipoCuenta.AHORROS ? "22" : "33";
        var codigoSubtipo = subtipo switch
        {
            SubtipoCuenta.NORMAL => "1",
            SubtipoCuenta.FREE => "2",
            SubtipoCuenta.SUELDO => "3",
            _ => "0"
        };

        return $"1112{codigoMoneda}{codigoTipo}{codigoSubtipo}{correlativo}";
    }
}