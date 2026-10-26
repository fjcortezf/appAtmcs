namespace AtmRedcqBank.Web.Models.Enums;
public enum TipoCuenta
{
    AHORROS,
    CORRIENTE
}
public static class TipoCuentaExtensions
{
    public static string GetCodigo(this TipoCuenta t) => t switch
    {
        TipoCuenta.AHORROS => "22",
        TipoCuenta.CORRIENTE => "33",
        _ => throw new ArgumentException("Tipo de cuenta inválido")
    };
    public static string GetEtiqueta(this TipoCuenta t) => t switch
    {
        TipoCuenta.AHORROS => "Ahorros",
        TipoCuenta.CORRIENTE => "Corriente",
        _ => t.ToString()
    };
}