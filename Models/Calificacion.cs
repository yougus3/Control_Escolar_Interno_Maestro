using System;
using System.Collections.Generic;
using System.Linq;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class Calificación
{
    private readonly Dictionary<string, string> _valores =
        new(StringComparer.OrdinalIgnoreCase);

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";

            return _valores.TryGetValue(key, out var valor) ? valor : "";
        }
        set
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _valores[key.Trim()] = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
    }

    public IEnumerable<string> ObtenerClaves()
    {
        return _valores.Keys.ToList();
    }
}