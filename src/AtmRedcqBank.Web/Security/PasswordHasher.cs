using System.Security.Cryptography;
using System.Text;
using AtmRedcqBank.Web.Config;
using Microsoft.Extensions.Options;

namespace AtmRedcqBank.Web.Security;

public class PasswordHasher
{
    private readonly AppSettings _settings;

    public PasswordHasher(IOptions<AppSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Hashear(string password)
    {
        var preHasheado = PreHashearConPepper(password);
        return BCrypt.Net.BCrypt.HashPassword(preHasheado, _settings.Seguridad.Bcrypt.Strength);
    }

    public bool Verificar(string password, string hashEsperado)
    {
        var preHasheado = PreHashearConPepper(password);
        return BCrypt.Net.BCrypt.Verify(preHasheado, hashEsperado);
    }

    private string PreHashearConPepper(string password)
    {
        var combinado = password + _settings.Seguridad.PasswordPepper;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combinado));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}