using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

// Implementación ligera de persistencia basada en archivos JSON para sustituir a LiteDB
// Provee la API mínima que usan los servicios de la aplicación.
public class LiteDbService : IDisposable
{
    private readonly string _dataFolder;
    private readonly string _parcialesPath;
    private readonly string _gruposPath;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public LiteDbService()
    {
        _dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
        if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
        _parcialesPath = Path.Combine(_dataFolder, "parciales.json");
        _gruposPath = Path.Combine(_dataFolder, "grupo.json");
    }

    // Devuelve como tuplas para permitir deconstrucción: foreach (var (key,val) in lite.GetAllParciales())
    public IEnumerable<(string Key, MateriaParcial Value)> GetAllParciales()
    {
        var dict = LoadParcialesFile();
        foreach (var kv in dict)
            yield return (kv.Key, kv.Value);
    }

    public MateriaParcial? GetMateria(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave)) return null;
        var dict = LoadParcialesFile();
        if (dict.TryGetValue(clave, out var materia)) return materia;
        return null;
    }

    public void SaveMateria(string clave, MateriaParcial materia)
    {
        if (string.IsNullOrWhiteSpace(clave)) return;
        var dict = LoadParcialesFile();
        dict[clave] = materia ?? new MateriaParcial();
        SaveParcialesFile(dict);
    }

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
            // ignore and return empty map
        }
        return result;
    }

    public void SaveGrupos(Dictionary<string,string> grupos)
    {
        try
        {
            var text = JsonSerializer.Serialize(grupos ?? new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase), _jsonOptions);
            File.WriteAllText(_gruposPath, text);
        }
        catch
        {
        }
    }

    // Configuraciones (API mínima)
    public IEnumerable<(string Key, Models.ConfiguracionParciales Value)> GetAllConfiguraciones()
    {
        var result = new List<(string Key, Models.ConfiguracionParciales Value)>();
        var path = Path.Combine(_dataFolder, "configuraciones.json");
        if (!File.Exists(path)) return result;
        try
        {
            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text)) return result;
            var dict = JsonSerializer.Deserialize<Dictionary<string, Models.ConfiguracionParciales>>(text, _jsonOptions);
            if (dict == null) return result;
            foreach (var kv in dict)
                result.Add((kv.Key, kv.Value));
        }
        catch { }
        return result;
    }

    public void SaveConfiguracion(string clave, Models.ConfiguracionParciales cfg)
    {
        var path = Path.Combine(_dataFolder, "configuraciones.json");
        try
        {
            Dictionary<string, Models.ConfiguracionParciales> dict = new(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, Models.ConfiguracionParciales>>(text, _jsonOptions);
                    if (loaded != null) dict = loaded;
                }
            }
            dict[clave] = cfg ?? new Models.ConfiguracionParciales();
            var outText = JsonSerializer.Serialize(dict, _jsonOptions);
            File.WriteAllText(path, outText);
        }
        catch { }
    }

    private Dictionary<string, MateriaParcial> LoadParcialesFile()
    {
        try
        {
            if (!File.Exists(_parcialesPath)) return new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
            var text = File.ReadAllText(_parcialesPath);
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
            var dict = JsonSerializer.Deserialize<Dictionary<string, MateriaParcial>>(text, _jsonOptions);
            if (dict == null) return new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
            return new Dictionary<string, MateriaParcial>(dict, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveParcialesFile(Dictionary<string, MateriaParcial> dict)
    {
        try
        {
            var text = JsonSerializer.Serialize(dict, _jsonOptions);
            File.WriteAllText(_parcialesPath, text);
        }
        catch
        {
            // ignore write failures for now
        }
    }

    public void Dispose()
    {
        // nothing to dispose
    }
}
