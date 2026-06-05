using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private const string ClaveGlobalLegada = "__DEFAULT__";

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
            File.WriteAllText(_rutaJson, "{}");
        }
    }

    private Dictionary<string, ConfiguracionParciales> CargarTodoInterno()
    {
        try
        {
            string json = File.ReadAllText(_rutaJson);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, ConfiguracionParciales>();
            }

            try
            {
                var datosDiccionario = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(json);
                if (datosDiccionario != null)
                {
                    return datosDiccionario;
                }
            }
            catch
            {
                // Intento siguiente abajo
            }

            try
            {
                var configLegada = JsonSerializer.Deserialize<ConfiguracionParciales>(json);
                if (configLegada != null)
                {
                    return new Dictionary<string, ConfiguracionParciales>
                    {
                        [ClaveGlobalLegada] = configLegada
                    };
                }
            }
            catch
            {
                // Se cae al return vacío
            }
        }
        catch
        {
        }

        return new Dictionary<string, ConfiguracionParciales>();
    }

    private void GuardarTodoInterno(Dictionary<string, ConfiguracionParciales> datos)
    {
        string json = JsonSerializer.Serialize(datos, _opciones);
        File.WriteAllText(_rutaJson, json);
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