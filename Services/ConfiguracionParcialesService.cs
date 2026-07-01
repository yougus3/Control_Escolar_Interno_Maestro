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
            SemestralHabilitado = false,
            ExtraHabilitado = false
        };
    }

    private Dictionary<string, ConfiguracionParciales> CargarTodoInterno()
    {
        var resultado = new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var lite = new LiteDbService();
            foreach (var (key, val) in lite.GetAllConfiguraciones())
            {
                resultado[key] = val ?? CrearConfiguracionPorDefecto();
            }
        }
        catch
        {
            // si LiteDB falla, devolvemos la configuración por defecto vacía
        }

        return resultado;
    }

    private void GuardarTodoInterno(Dictionary<string, ConfiguracionParciales> datos)
    {
        try
        {
            using var lite = new LiteDbService();
            if (datos == null) return;
            foreach (var kv in datos)
            {
                lite.SaveConfiguracion(kv.Key, kv.Value ?? CrearConfiguracionPorDefecto());
            }
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