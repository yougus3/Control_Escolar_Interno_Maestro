using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private const string ClaveGlobalLegada = "__DEFAULT__";
    private readonly string _rutaConfig;

    public ConfiguracionParcialesService()
    {
        var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        _rutaConfig = Path.Combine(carpeta, "configuracion_parciales.json");
    }

    private static ConfiguracionParciales CrearConfiguracionPorDefecto()
    {
        return new ConfiguracionParciales
        {
            CapturaDirectaHabilitada = false,
            Parcial1Habilitado = true,
            Parcial2Habilitado = false,
            Parcial3Habilitado = false,
            SemestralHabilitado = false
        };
    }

    private Dictionary<string, ConfiguracionParciales> CargarTodoInterno()
    {
        if (!File.Exists(_rutaConfig)) return new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(_rutaConfig);
            var dict = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(json);
            return dict != null
                ? new Dictionary<string, ConfiguracionParciales>(dict, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void GuardarTodoInterno(Dictionary<string, ConfiguracionParciales> datos)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(datos, options);
            File.WriteAllText(_rutaConfig, json);
        }
        catch { }
    }

    public Dictionary<string, ConfiguracionParciales> CargarTodo()
    {
        return CargarTodoInterno();
    }

    public void GuardarTodo(Dictionary<string, ConfiguracionParciales> datos)
    {
        GuardarTodoInterno(datos ?? new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase));
    }

    public ConfiguracionParciales ObtenerConfiguracion()
    {
        return ObtenerConfiguracion(string.Empty);
    }

    public ConfiguracionParciales ObtenerConfiguracion(string claveMateria)
    {
        var datos = CargarTodoInterno();

        if (!string.IsNullOrWhiteSpace(claveMateria) &&
            datos.TryGetValue(claveMateria, out ConfiguracionParciales? configMateria) &&
            configMateria != null)
        {
            return configMateria;
        }

        if (datos.TryGetValue(ClaveGlobalLegada, out ConfiguracionParciales? configGlobal) &&
            configGlobal != null)
        {
            return configGlobal;
        }

        return CrearConfiguracionPorDefecto();
    }

    public void GuardarConfiguracion(ConfiguracionParciales configuracion)
    {
        GuardarConfiguracion(ClaveGlobalLegada, configuracion);
    }

    public void GuardarConfiguracion(string claveMateria, ConfiguracionParciales configuracion)
    {
        if (string.IsNullOrWhiteSpace(claveMateria))
        {
            claveMateria = ClaveGlobalLegada;
        }

        var datos = CargarTodoInterno();
        datos[claveMateria] = configuracion ?? CrearConfiguracionPorDefecto();
        GuardarTodoInterno(datos);
    }
}