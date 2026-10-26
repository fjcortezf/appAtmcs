using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace AtmRedcqBank.Web.Services;

public class AuditoriaService
{
    private readonly AppDbContext _db;

    public AuditoriaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(
        TipoActor tipoActor,
        long? idActor,
        string? emailActor,
        string operacion,
        string? recurso,
        bool exito,
        string? detalle,
        HttpRequest? request)
    {
        var auditoria = new Auditoria
        {
            TipoActor = tipoActor,
            IdActor = idActor,
            EmailActor = emailActor,
            Operacion = operacion,
            Recurso = recurso,
            Resultado = exito ? "EXITO" : "FALLO",
            Detalle = detalle,
            Ip = ExtraerIp(request),
            UserAgent = Recortar(request?.Headers["User-Agent"].ToString(), 300),
            Timestamp = DateTime.Now
        };

        _db.Auditorias.Add(auditoria);
        await _db.SaveChangesAsync();
    }

    private static string? ExtraerIp(HttpRequest? request)
    {
        if (request == null) return null;
        var xff = request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xff))
            return xff.Split(',')[0].Trim();
        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? Recortar(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return s.Length <= max ? s : s[..max];
    }
}