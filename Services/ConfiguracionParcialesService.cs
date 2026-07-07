using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private string ConfigPath
    {
        get
        {
            var dir = Path.Combine(GlobalSettings.CurrentCapDirectory, "Data");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "configuraciones.json");
        }
    }

    public ConfiguracionParcialesService()
    {
    }

    public ConfiguracionParciales ObtenerConfiguracion(string claveMateria = "")
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new ConfiguracionParciales();
            var text = File.ReadAllText(ConfigPath);
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
            if (File.Exists(ConfigPath))
            {
                var text = File.ReadAllText(ConfigPath);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (loaded != null)
                        dict = loaded;
                }
            }

            dict[claveMateria] = cfg ?? new ConfiguracionParciales();
            var outText = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, outText);
        }
        catch
        {
        }
    }
}