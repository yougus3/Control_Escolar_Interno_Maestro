using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class LiteDbService : IDisposable
{
    private readonly string _rutaDb;
    private readonly LiteDatabase _db;

    public LiteDbService()
    {
        var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        _rutaDb = Path.Combine(carpeta, "appdata.db");
        _db = new LiteDatabase($"Filename={_rutaDb};Connection=shared");
    }

    // Grupos
    public Dictionary<string, string> GetGrupos()
    {
        var col = _db.GetCollection<GrupoDoc>("grupos");
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in col.FindAll())
        {
            if (!string.IsNullOrWhiteSpace(g.Matricula) && !string.IsNullOrWhiteSpace(g.Grupo))
                dict[g.Matricula] = g.Grupo;
        }
        return dict;
    }

    public void SaveGrupos(Dictionary<string, string> grupos)
    {
        var col = _db.GetCollection<GrupoDoc>("grupos");
        col.DeleteAll();
        if (grupos == null) return;
        foreach (var kv in grupos)
        {
            col.Insert(new GrupoDoc { Matricula = kv.Key, Grupo = kv.Value });
        }
    }

    // Configuraciones
    public ConfiguracionParciales GetConfiguracion(string claveMateria)
    {
        var col = _db.GetCollection<ConfiguracionDoc>("configuraciones");
        var doc = col.FindById(claveMateria ?? "__DEFAULT__");
        return doc != null ? doc.Configuracion : null;
    }

    public IEnumerable<(string Key, ConfiguracionParciales Value)> GetAllConfiguraciones()
    {
        var col = _db.GetCollection<ConfiguracionDoc>("configuraciones");
        foreach (var d in col.FindAll())
        {
            yield return (d.Id, d.Configuracion);
        }
    }

    public void SaveConfiguracion(string claveMateria, ConfiguracionParciales config)
    {
        var col = _db.GetCollection<ConfiguracionDoc>("configuraciones");
        if (string.IsNullOrWhiteSpace(claveMateria)) claveMateria = "__DEFAULT__";
        var doc = new ConfiguracionDoc { Id = claveMateria, Configuracion = config ?? new ConfiguracionParciales() };
        col.Upsert(doc);
    }

    // Parciales
    public MateriaParcial GetMateria(string claveMateria)
    {
        var col = _db.GetCollection<ParcialDoc>("parciales");
        var doc = col.FindById(claveMateria);
        return doc != null ? doc.Materia : null;
    }

    public IEnumerable<(string Key, MateriaParcial Value)> GetAllParciales()
    {
        var col = _db.GetCollection<ParcialDoc>("parciales");
        foreach (var d in col.FindAll())
        {
            yield return (d.Id, d.Materia);
        }
    }

    public void SaveMateria(string claveMateria, MateriaParcial materia)
    {
        var col = _db.GetCollection<ParcialDoc>("parciales");
        if (string.IsNullOrWhiteSpace(claveMateria)) return;
        var doc = new ParcialDoc { Id = claveMateria, Materia = materia ?? new MateriaParcial() };
        col.Upsert(doc);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    private class GrupoDoc
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
    }

    private class ConfiguracionDoc
    {
        [BsonId]
        public string Id { get; set; } = "__DEFAULT__";
        public ConfiguracionParciales Configuracion { get; set; } = new ConfiguracionParciales();
    }

    private class ParcialDoc
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public MateriaParcial Materia { get; set; } = new MateriaParcial();
    }
}
