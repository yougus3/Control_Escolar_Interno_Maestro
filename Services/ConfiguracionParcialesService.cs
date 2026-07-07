using System;
using System.Linq;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    public ConfiguracionParcialesService()
    {
    }

    public ConfiguracionParciales ObtenerConfiguracion(string claveMateria = "")
    {
        try
        {
            using var lite = new LiteDbService();
            var configs = lite.GetAllConfiguraciones().ToList();

            if (!configs.Any()) return new ConfiguracionParciales();

            if (string.IsNullOrWhiteSpace(claveMateria))
                return configs.FirstOrDefault().Value ?? new ConfiguracionParciales();

            var match = configs.FirstOrDefault(c => string.Equals(c.Key, claveMateria, StringComparison.OrdinalIgnoreCase));
            return match.Value ?? new ConfiguracionParciales();
        }
        catch
        {
            return new ConfiguracionParciales();
        }
    }

    public void GuardarConfiguracion(string claveMateria, ConfiguracionParciales cfg)
    {
        if (string.IsNullOrWhiteSpace(claveMateria) || cfg == null) return;
        try
        {
            using var lite = new LiteDbService();
            lite.SaveConfiguracion(claveMateria, cfg);
        }
        catch
        {
        }
    }
}