using AtmRedcqBank.Web.Models.Entities;
using AtmRedcqBank.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AtmRedcqBank.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ============================================================
    // DbSets (18 tablas)
    // ============================================================
    public DbSet<PersonalBanco> PersonalBanco => Set<PersonalBanco>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<ParametroHistorico> ParametrosHistorico => Set<ParametroHistorico>();
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();
    public DbSet<TipoCuentaConfig> TiposCuentaConfig => Set<TipoCuentaConfig>();
    public DbSet<Tarjeta> Tarjetas => Set<Tarjeta>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Atm> Atms => Set<Atm>();
    public DbSet<AtmCash> AtmCash => Set<AtmCash>();
    public DbSet<AtmCashMovimiento> AtmCashMovimientos => Set<AtmCashMovimiento>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<SaldoDeudor> SaldosDeudores => Set<SaldoDeudor>();
    public DbSet<AtmSesion> AtmSesiones => Set<AtmSesion>();
    public DbSet<SolicitudTarjeta> SolicitudesTarjeta => Set<SolicitudTarjeta>();
    public DbSet<ContadorOperacion> ContadoresOperacion => Set<ContadorOperacion>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // Mapeo de enums como string (coincide con ENUM de MariaDB)
        // ============================================================
        var enumToStringConverter = new ValueConverter<Enum, string>(
            v => v.ToString(),
            v => (Enum)Enum.Parse(typeof(Enum), v));

        // Aplicar conversión a todos los enums
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType.IsEnum ||
                            (Nullable.GetUnderlyingType(p.PropertyType)?.IsEnum ?? false));

            foreach (var property in properties)
            {
                var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var converterType = typeof(EnumToStringConverter<>).MakeGenericType(enumType);
                var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(converter)
                    .HasColumnType("varchar(30)");
            }
        }

        // ============================================================
        // Configuraciones específicas
        // ============================================================

        // PersonalBanco: email único
        modelBuilder.Entity<PersonalBanco>()
            .HasIndex(p => p.Email)
            .IsUnique()
            .HasDatabaseName("UQ_personal_email");

        // Cliente: documento compuesto único + email único
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.TipoDocumento, c.NumeroDocumento })
            .IsUnique()
            .HasDatabaseName("UQ_cliente_documento");

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("UQ_cliente_email");

        // Parametro: clave única
        modelBuilder.Entity<Parametro>()
            .HasIndex(p => p.Clave)
            .IsUnique()
            .HasDatabaseName("UQ_parametro_clave");

        // TipoCambio: fecha única
        modelBuilder.Entity<TipoCambio>()
            .HasIndex(t => t.Fecha)
            .IsUnique()
            .HasDatabaseName("UQ_tipo_cambio_fecha");

        // TipoCuentaConfig: tipo + subtipo único
        modelBuilder.Entity<TipoCuentaConfig>()
            .HasIndex(t => new { t.TipoCuenta, t.SubtipoCuenta })
            .IsUnique()
            .HasDatabaseName("UQ_tipo_subtipo_config");

        // Tarjeta: numero_tarjeta único
        modelBuilder.Entity<Tarjeta>()
            .HasIndex(t => t.NumeroTarjeta)
            .IsUnique()
            .HasDatabaseName("UQ_tarjeta_numero");

        // Cuenta: numero_cuenta único
        modelBuilder.Entity<Cuenta>()
            .HasIndex(c => c.NumeroCuenta)
            .IsUnique()
            .HasDatabaseName("UQ_cuenta_numero");

        // Atm: codigo único
        modelBuilder.Entity<Atm>()
            .HasIndex(a => a.Codigo)
            .IsUnique()
            .HasDatabaseName("UQ_atm_codigo");

        // AtmCash: atm + moneda + denominacion único
        modelBuilder.Entity<AtmCash>()
            .HasIndex(ac => new { ac.AtmId, ac.Moneda, ac.Denominacion })
            .IsUnique()
            .HasDatabaseName("UQ_atm_denominacion");

        // Transaccion: numero_operacion único
        modelBuilder.Entity<Transaccion>()
            .HasIndex(t => t.NumeroOperacion)
            .IsUnique()
            .HasDatabaseName("UQ_transaccion_numero");

        // ContadorOperacion: atm + fecha único
        modelBuilder.Entity<ContadorOperacion>()
            .HasIndex(c => new { c.AtmId, c.Fecha })
            .IsUnique()
            .HasDatabaseName("UQ_contador_atm_fecha");

        // ============================================================
        // Precisión de decimales (por si acaso)
        // ============================================================
        modelBuilder.Entity<Cuenta>().Property(c => c.Saldo).HasPrecision(15, 2);
        modelBuilder.Entity<Transaccion>().Property(t => t.Monto).HasPrecision(15, 2);
        modelBuilder.Entity<Transaccion>().Property(t => t.MontoItf).HasPrecision(15, 2);
        modelBuilder.Entity<Transaccion>().Property(t => t.ComisionAplicada).HasPrecision(15, 2);
        modelBuilder.Entity<Transaccion>().Property(t => t.SaldoAnterior).HasPrecision(15, 2);
        modelBuilder.Entity<Transaccion>().Property(t => t.SaldoPosterior).HasPrecision(15, 2);
        modelBuilder.Entity<Transferencia>().Property(t => t.MontoOrigen).HasPrecision(15, 2);
        modelBuilder.Entity<Transferencia>().Property(t => t.MontoDestino).HasPrecision(15, 2);
        modelBuilder.Entity<SaldoDeudor>().Property(s => s.Monto).HasPrecision(15, 2);
        modelBuilder.Entity<TipoCambio>().Property(t => t.TasaCompra).HasPrecision(12, 6);
        modelBuilder.Entity<TipoCambio>().Property(t => t.TasaVenta).HasPrecision(12, 6);
        modelBuilder.Entity<TipoCuentaConfig>().Property(t => t.ComisionMantenimiento).HasPrecision(15, 2);
        modelBuilder.Entity<TipoCuentaConfig>().Property(t => t.TasaInteresAnual).HasPrecision(5, 4);
        modelBuilder.Entity<TipoCuentaConfig>().Property(t => t.CostoExcesoOperacion).HasPrecision(15, 2);
    }

    // ============================================================
    // Timestamps automáticos (equivalente a @CreationTimestamp)
    // ============================================================
    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
            var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");

            if (entry.State == EntityState.Added && createdAtProp != null && createdAtProp.CurrentValue == null)
                createdAtProp.CurrentValue = DateTime.Now;

            if (updatedAtProp != null)
                updatedAtProp.CurrentValue = DateTime.Now;
        }
    }
}