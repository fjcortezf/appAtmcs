using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Models.Dtos;
using AtmRedcqBank.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtmRedcqBank.Web.Services;

public class AdminClienteService
{
    private readonly AppDbContext _db;

    public AdminClienteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Cliente>> ListarAsync()
    {
        return await _db.Clientes
            .OrderBy(c => c.Apellidos)
            .ThenBy(c => c.Nombres)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerAsync(long id)
    {
        return await _db.Clientes
            .Include(c => c.Tarjetas)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<string?> CrearAsync(ClienteCrearForm form)
    {
        // Validar documento único
        if (await _db.Clientes.AnyAsync(c => c.TipoDocumento == form.TipoDocumento && c.NumeroDocumento == form.NumeroDocumento))
            return "Ya existe un cliente con ese documento.";

        // Validar email único si se ingresó
        if (!string.IsNullOrWhiteSpace(form.Email))
        {
            if (await _db.Clientes.AnyAsync(c => c.Email == form.Email))
                return "Ya existe un cliente con ese email.";
        }

        var cliente = new Cliente
        {
            Nombres = form.Nombres,
            Apellidos = form.Apellidos,
            TipoDocumento = form.TipoDocumento,
            NumeroDocumento = form.NumeroDocumento,
            Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email,
            Telefono = form.Telefono,
            Estado = "ACTIVO"
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ActualizarAsync(long id, string nombres, string apellidos, string? email, string? telefono)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return "Cliente no encontrado.";

        // Validar email único si se está cambiando
        if (!string.IsNullOrWhiteSpace(email) && email != cliente.Email)
        {
            if (await _db.Clientes.AnyAsync(c => c.Email == email && c.Id != id))
                return "Ya existe otro cliente con ese email.";
        }

        cliente.Nombres = nombres;
        cliente.Apellidos = apellidos;
        cliente.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        cliente.Telefono = telefono;

        _db.Clientes.Update(cliente);
        await _db.SaveChangesAsync();
        return null;
    }
}