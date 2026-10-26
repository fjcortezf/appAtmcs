namespace AtmRedcqBank.Web.Models.Enums;

public enum SubtipoCuenta
{
    NORMAL,
    FREE,
    SUELDO,
    NA
}

public static class SubtipoCuentaExtensions
{
    public static string GetCodigo(this SubtipoCuenta s) => s switch
    {
        SubtipoCuenta.NORMAL => "1",
        SubtipoCuenta.FREE => "2",
        SubtipoCuenta.SUELDO => "3",
        SubtipoCuenta.NA => "0",
        _ => throw new ArgumentException("Subtipo inválido")
    };

    public static string GetEtiqueta(this SubtipoCuenta s) => s switch
    {
        SubtipoCuenta.NORMAL => "Normal",
        SubtipoCuenta.FREE => "Free",
        SubtipoCuenta.SUELDO => "Sueldo",
        SubtipoCuenta.NA => "No aplica",
        _ => s.ToString()
    };
}