using System;
using System.Collections.Generic;
using System.Linq;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class Calificación
{
    private readonly Dictionary<string, string> _valores = new(StringComparer.OrdinalIgnoreCase);

    public string this[string columna]
    {
        get => _valores.TryGetValue(columna, out var valor) ? valor : "-";
        set => _valores[columna] = value;
    }

    public List<string> ObtenerClaves()
    {
        return _valores.Keys.ToList();
    }

    public void Limpiar()
    {
        _valores.Clear();
    }
}