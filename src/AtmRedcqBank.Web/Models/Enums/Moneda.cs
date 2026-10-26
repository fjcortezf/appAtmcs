namespace AtmRedcqBank.Web.Models.Enums;

public enum Moneda
{
    PEN,
    USD
}

public static class MonedaExtensions
{
    public static string GetCodigo(this Moneda m) => m switch
    {
        Moneda.PEN => "0",
        Moneda.USD => "1",
        _ => throw new ArgumentException("Moneda inválida")
    };

    public static string GetSimbolo(this Moneda m) => m switch
    {
        Moneda.PEN => "S/",
        Moneda.USD => "$",
        _ => m.ToString()
    };

    public static string GetEtiqueta(this Moneda m) => m switch
    {
        Moneda.PEN => "Soles",
        Moneda.USD => "Dólares",
        _ => m.ToString()
    };
}