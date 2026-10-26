using System.Security.Cryptography;
using System.Text;
using AtmRedcqBank.Web.Config;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace AtmRedcqBank.Web.Security;

public class PinHasher
{
    private readonly AppSettings _settings;

    public PinHasher(IOptions<AppSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerarSalt()
    {
        var salt = new byte[_settings.Seguridad.Argon2.SaltLength];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToBase64String(salt);
    }

    public string Hashear(string pin, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var pinConPepper = pin + _settings.Seguridad.PinPepper;

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(pinConPepper))
        {
            Salt = salt,
            DegreeOfParallelism = _settings.Seguridad.Argon2.Parallelism,
            MemorySize = _settings.Seguridad.Argon2.MemoryKb,
            Iterations = _settings.Seguridad.Argon2.Iterations
        };

        var hash = argon2.GetBytes(_settings.Seguridad.Argon2.HashLength);
        return Convert.ToBase64String(hash);
    }

    public bool Verificar(string pin, string saltBase64, string hashEsperadoBase64)
    {
        var hashCalculado = Hashear(pin, saltBase64);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashCalculado),
            Encoding.UTF8.GetBytes(hashEsperadoBase64));
    }
}