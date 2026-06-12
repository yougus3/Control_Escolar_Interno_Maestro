using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private const string ClaveGlobalLegada = "__DEFAULT__";

    private readonly string _dbPath;

    public ConfiguracionParcialesService()
    {
        var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        _dbPath = Path.Combine(carpeta, "data.db");
        ParcialJsonService.EnsureMigration();
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
        try
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<StoredConfig>("configuraciones");
            var dict = new Dictionary<string, ConfiguracionParciales>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in col.FindAll())
            {
                if (item != null && item.Id != null) dict[item.Id] = item.Value ?? CrearConfiguracionPorDefecto();
            }
            return dict;
        }
        catch
        {
            return new Dictionary<string, ConfiguracionParciales>();
        }
    }

    private void GuardarTodoInterno(Dictionary<string, ConfiguracionParciales> datos)
    {
        using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
        var col = db.GetCollection<StoredConfig>("configuraciones");
        col.DeleteAll();
        foreach (var kv in datos)
        {
            col.Upsert(new StoredConfig { Id = kv.Key, Value = kv.Value ?? CrearConfiguracionPorDefecto() });
        }
    }

    public class StoredConfig
    {
        public string Id { get; set; } = string.Empty;
        public ConfiguracionParciales Value { get; set; } = new ConfiguracionParciales();
    }
    // Public API to load/save the entire collection
    public Dictionary<string, ConfiguracionParciales> CargarTodo()
    {
        return CargarTodoInterno();
    }

    public void GuardarTodo(Dictionary<string, ConfiguracionParciales> datos)
    {
        GuardarTodoInterno(datos ?? new Dictionary<string, ConfiguracionParciales>());
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