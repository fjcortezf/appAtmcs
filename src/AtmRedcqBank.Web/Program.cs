using AtmRedcqBank.Web.Data;
using AtmRedcqBank.Web.Security;
using AtmRedcqBank.Web.Services;
using AtmRedcqBank.Web.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Configuración (appsettings + User Secrets)
// ============================================================
builder.Services.Configure<AtmRedcqBank.Web.Config.AppSettings>(
    builder.Configuration);

builder.Services.AddScoped<PinHasher>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<ParametroService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<AtmAuthService>();
builder.Services.AddScoped<NumeroOperacionService>();
builder.Services.AddScoped<CalculadoraFinanciera>();
builder.Services.AddScoped<DispensadorBilletes>();
builder.Services.AddScoped<RetiroService>();
builder.Services.AddScoped<DepositoService>();
builder.Services.AddScoped<TransferenciaService>();
builder.Services.AddScoped<CambioPinService>();
builder.Services.AddScoped<AdminClienteService>();
builder.Services.AddScoped<AdminTarjetaService>();
builder.Services.AddHttpClient<TipoCambioApiService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<AdminParametroService>();
builder.Services.AddScoped<AdminSolicitudService>();
builder.Services.AddScoped<AdminAuditoriaService>();
builder.Services.AddScoped<SistemaProcesoService>();
builder.Services.AddScoped<AdminAtmService>();
builder.Services.AddScoped<DataSeeder>();

// ============================================================
// Base de datos (EF Core + MariaDB)
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string no configurado.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ============================================================
// MVC
// ============================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
   options.IdleTimeout = TimeSpan.FromMinutes(30);
   options.Cookie.HttpOnly = true;
   options.Cookie.IsEssential = true;
   options.Cookie.Name = "RedcqBank.AtmSession";
});
builder.Services.AddControllersWithViews();

// ============================================================
// Autenticación por cookie (personal del banco)
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/AdminAuth/Login";
        options.LogoutPath = "/AdminAuth/Logout";
        options.AccessDeniedPath = "/AdminAuth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "RedcqBank.AdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ejecutar seeder al arrancar
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();