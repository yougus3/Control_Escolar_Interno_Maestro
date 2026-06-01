using System;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private readonly string _rutaJson;
    private static readonly JsonSerializerOptions _opciones = new()
    {
        WriteIndented = true
    };

    public ConfiguracionParcialesService()
    {
        _rutaJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "configuracion_parciales.json");
        CrearArchivoSiNoExiste();
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

    private void CrearArchivoSiNoExiste()
    {
        string? carpeta = Path.GetDirectoryName(_rutaJson);

        if (!string.IsNullOrWhiteSpace(carpeta) && !Directory.Exists(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        if (!File.Exists(_rutaJson))
        {
            GuardarConfiguracion(CrearConfiguracionPorDefecto());
        }
    }

    public ConfiguracionParciales ObtenerConfiguracion()
    {
        try
        {
            if (!File.Exists(_rutaJson))
            {
                var defecto = CrearConfiguracionPorDefecto();
                GuardarConfiguracion(defecto);
                return defecto;
            }

            string json = File.ReadAllText(_rutaJson);
            var config = JsonSerializer.Deserialize<ConfiguracionParciales>(json);

            if (config == null)
            {
                var defecto = CrearConfiguracionPorDefecto();
                GuardarConfiguracion(defecto);
                return defecto;
            }

            return config;
        }
        catch
        {
            var defecto = CrearConfiguracionPorDefecto();
            GuardarConfiguracion(defecto);
            return defecto;
        }
    }

    public void GuardarConfiguracion(ConfiguracionParciales configuracion)
    {
        string json = JsonSerializer.Serialize(configuracion, _opciones);
        File.WriteAllText(_rutaJson, json);
    }
}