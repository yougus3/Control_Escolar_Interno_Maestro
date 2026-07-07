using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LiteDB; 
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class LiteDbService : IDisposable
{
    private readonly string _dataFolder;
    private readonly string _dbPath;
    private readonly string _gruposPath;
    private readonly LiteDatabase _db;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public LiteDbService()
    {
        // Usa la ruta dinámica de la USB / Carpeta de CAPs
        _dataFolder = Path.Combine(GlobalSettings.CurrentCapDirectory, "Data");
        if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
        
        // Configuraciones y Parciales van encriptados en el DB LITE
        _dbPath = Path.Combine(_dataFolder, "parciales.db");
        _db = new LiteDatabase(_dbPath);

        // Grupos se queda libre en JSON
        _gruposPath = Path.Combine(_dataFolder, "grupo.json");
    }

    // --- MÉTODOS CON LITEDB (Parciales y Configuración) ---

    public IEnumerable<(string Key, MateriaParcial Value)> GetAllParciales()
    {
        var col = _db.GetCollection<MateriaParcialRecord>("Parciales");
        foreach (var doc in col.FindAll())
        {
            yield return (doc.Id, doc.Data);
        }
    }

    public MateriaParcial? GetMateria(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave)) return null;
        var col = _db.GetCollection<MateriaParcialRecord>("Parciales");
        var record = col.FindById(clave);
        return record?.Data;
    }

    public void SaveMateria(string clave, MateriaParcial materia)
    {
        if (string.IsNullOrWhiteSpace(clave) || materia == null) return;
        var col = _db.GetCollection<MateriaParcialRecord>("Parciales");
        
        var record = new MateriaParcialRecord { Id = clave, Data = materia };
        col.Upsert(record);
    }

    public IEnumerable<(string Key, ConfiguracionParciales Value)> GetAllConfiguraciones()
    {
        var col = _db.GetCollection<ConfiguracionRecord>("Configuraciones");
        foreach (var doc in col.FindAll())
        {
            yield return (doc.Id, doc.Data);
        }
    }

    public void SaveConfiguracion(string clave, ConfiguracionParciales cfg)
    {
        if (string.IsNullOrWhiteSpace(clave) || cfg == null) return;
        var col = _db.GetCollection<ConfiguracionRecord>("Configuraciones");
        
        var record = new ConfiguracionRecord { Id = clave, Data = cfg };
        col.Upsert(record);
    }

    // --- MÉTODOS CON JSON (Solo para Grupos) ---

    public Dictionary<string, string> GetGrupos()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(_gruposPath))
                return result;

            using var fs = File.OpenRead(_gruposPath);
            using var doc = JsonDocument.Parse(fs);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                {
                    var mat = item[0].GetString() ?? string.Empty;
                    var grp = item[1].GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(mat) && !string.IsNullOrWhiteSpace(grp))
                        result[mat.Trim()] = grp.Trim();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public void SaveGrupos(Dictionary<string, string> grupos)
    {
        try
        {
            var text = System.Text.Json.JsonSerializer.Serialize(grupos ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), _jsonOptions);
            File.WriteAllText(_gruposPath, text);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    // --- Clases Privadas para estructurar los documentos en LiteDB ---
    
    private class MateriaParcialRecord
    {
        [BsonId] 
        public string Id { get; set; } = string.Empty;
        public MateriaParcial Data { get; set; } = new();
    }

    private class ConfiguracionRecord
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public ConfiguracionParciales Data { get; set; } = new();
    }
}