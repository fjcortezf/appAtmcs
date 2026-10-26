namespace AtmRedcqBank.Web.Models.Enums;

public enum TipoOperacion
{
    RETIRO,
    DEPOSITO,
    TRANSFERENCIA_ENVIADA,
    TRANSFERENCIA_RECIBIDA,
    CAMBIO_PIN,
    COBRO_MANTENIMIENTO,
    ABONO_INTERES,
    COBRO_DEUDA
}

public static class TipoOperacionExtensions
{
    public static bool AfectaSaldo(this TipoOperacion t) => t != TipoOperacion.CAMBIO_PIN;
    public static bool EsCredito(this TipoOperacion t) =>
        t == TipoOperacion.DEPOSITO ||
        t == TipoOperacion.TRANSFERENCIA_RECIBIDA ||
        t == TipoOperacion.ABONO_INTERES;
    public static bool EsDebito(this TipoOperacion t) =>
        t == TipoOperacion.RETIRO ||
        t == TipoOperacion.TRANSFERENCIA_ENVIADA ||
        t == TipoOperacion.COBRO_MANTENIMIENTO ||
        t == TipoOperacion.COBRO_DEUDA;
}