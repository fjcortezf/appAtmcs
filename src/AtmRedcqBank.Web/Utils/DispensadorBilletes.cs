namespace AtmRedcqBank.Web.Utils;

public class DispensadorBilletes
{
    public Dictionary<int, int> Calcular(int monto, Dictionary<int, int> stock)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto debe ser positivo");
        if (monto % 20 != 0)
            throw new ArgumentException("El monto debe ser múltiplo de 20");

        var denominaciones = stock.Keys.OrderByDescending(d => d).ToList();
        var resultado = new Dictionary<int, int>();

        var exito = Backtrack(monto, 0, denominaciones, stock, resultado);

        if (!exito)
            throw new InvalidOperationException(
                $"El ATM no puede dispensar S/ {monto} con las denominaciones disponibles.");

        return resultado;
    }

    private bool Backtrack(int restante, int idx, List<int> denominaciones,
                           Dictionary<int, int> stock, Dictionary<int, int> resultado)
    {
        if (restante == 0) return true;
        if (idx >= denominaciones.Count) return false;

        int denom = denominaciones[idx];
        int disponible = stock.GetValueOrDefault(denom, 0);
        int maxPosibles = Math.Min(disponible, restante / denom);

        for (int cant = maxPosibles; cant >= 0; cant--)
        {
            if (cant > 0)
                resultado[denom] = cant;
            else
                resultado.Remove(denom);

            int nuevoRestante = restante - (cant * denom);
            if (Backtrack(nuevoRestante, idx + 1, denominaciones, stock, resultado))
                return true;
        }

        resultado.Remove(denom);
        return false;
    }
}