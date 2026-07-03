using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private readonly string _configPath;

    public ConfiguracionParcialesService()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "Data", "configuraciones.json");
    }

    public ConfiguracionParciales ObtenerConfiguracion(string claveMateria = "")
    {
        try
        {
            if (!File.Exists(_configPath)) return new ConfiguracionParciales();
            var text = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(text)) return new ConfiguracionParciales();
            var dict = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dict == null) return new ConfiguracionParciales();
            if (string.IsNullOrWhiteSpace(claveMateria)) return dict.Values.FirstOrDefault() ?? new ConfiguracionParciales();
            if (dict.TryGetValue(claveMateria, out var cfg)) return cfg;
            return new ConfiguracionParciales();
        }
        catch
        {
            return new ConfiguracionParciales();
        }
    }

    public void GuardarConfiguracion(string claveMateria, ConfiguracionParciales cfg)
    {
        try
        {
            Dictionary<string, ConfiguracionParciales> dict = new();
            if (File.Exists(_configPath))
            {
                var text = File.ReadAllText(_configPath);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (loaded != null)
                        dict = loaded;
                }
            }

            dict[claveMateria] = cfg ?? new ConfiguracionParciales();
            var outText = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, outText);
        }
        catch
        {
        }
    }
}