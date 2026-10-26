namespace AtmRedcqBank.Web.Config;

public class AppSettings
{
    public AtmSettings Atm { get; set; } = new();
    public SeguridadSettings Seguridad { get; set; } = new();
    public PdfSettings Pdf { get; set; } = new();
    public AuditoriaSettings Auditoria { get; set; } = new();
}

public class AtmSettings
{
    public string Codigo { get; set; } = "ATM01";
    public int TimeoutSesionSegundos { get; set; } = 120;
    public int TimeoutAvisoSegundos { get; set; } = 10;
}

public class SeguridadSettings
{
    public string PinPepper { get; set; } = "";
    public string PasswordPepper { get; set; } = "";
    public Argon2Settings Argon2 { get; set; } = new();
    public BcryptSettings Bcrypt { get; set; } = new();
}

public class Argon2Settings
{
    public int SaltLength { get; set; } = 16;
    public int HashLength { get; set; } = 32;
    public int Parallelism { get; set; } = 1;
    public int MemoryKb { get; set; } = 65536;
    public int Iterations { get; set; } = 3;
}

public class BcryptSettings
{
    public int Strength { get; set; } = 12;
}

public class PdfSettings
{
    public string Emisor { get; set; } = "REDCQ BANK";
}

public class AuditoriaSettings
{
    public bool AuditarLecturas { get; set; } = true;
}