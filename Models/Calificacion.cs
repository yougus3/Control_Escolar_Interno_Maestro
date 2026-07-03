using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

public class Calificación : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _valores =
        new(StringComparer.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

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

            string k = key.Trim();
            string v = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

            // Evitar reasignaciones y notificaciones redundantes que pueden provocar reentradas
            if (_valores.TryGetValue(k, out var existing) && existing == v)
                return;

            _valores[k] = v;
            // Notify WPF bindings that the indexer item changed. The binding path uses indexer: Calificación[KEY]
            // WPF listens for PropertyChanged with name "Item[KEY]"
            OnPropertyChanged($"Item[{k}]");
        }
    }

    public IEnumerable<string> ObtenerClaves()
    {
        return _valores.Keys.ToList();
    }
}
