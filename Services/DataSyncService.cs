using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public class DataSyncService
    {
        private readonly string _carpeta;

        public DataSyncService()
        {
            _carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_carpeta)) Directory.CreateDirectory(_carpeta);
        }

        public IEnumerable<(string Key, MateriaParcial Value)> CargarParciales()
        {
            var ruta = Path.Combine(_carpeta, "parciales.json");
            if (!File.Exists(ruta)) yield break;
            var dict = JsonSerializer.Deserialize<Dictionary<string, MateriaParcial>>(File.ReadAllText(ruta));
            if (dict != null) foreach (var kv in dict) yield return (kv.Key, kv.Value);
        }

        public void GuardarParciales(IEnumerable<(string Key, MateriaParcial Value)> items)
        {
            var ruta = Path.Combine(_carpeta, "parciales.json");
            var dict = new Dictionary<string, MateriaParcial>();
            foreach (var kv in items) dict[kv.Key] = kv.Value;
            File.WriteAllText(ruta, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }

        public IEnumerable<(string Key, ConfiguracionParciales Value)> CargarConfiguraciones()
        {
            var ruta = Path.Combine(_carpeta, "configuracion_parciales.json");
            if (!File.Exists(ruta)) yield break;
            var dict = JsonSerializer.Deserialize<Dictionary<string, ConfiguracionParciales>>(File.ReadAllText(ruta));
            if (dict != null) foreach (var kv in dict) yield return (kv.Key, kv.Value);
        }

        public void GuardarConfiguraciones(IEnumerable<(string Key, ConfiguracionParciales Value)> items)
        {
            var ruta = Path.Combine(_carpeta, "configuracion_parciales.json");
            var dict = new Dictionary<string, ConfiguracionParciales>();
            foreach (var kv in items) dict[kv.Key] = kv.Value;
            File.WriteAllText(ruta, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }

        public Dictionary<string, string> CargarGrupos()
        {
            var ruta = Path.Combine(_carpeta, "grupo.json");
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(ruta)) return mapa;
            try {
                var datos = JsonSerializer.Deserialize<List<List<string>>>(File.ReadAllText(ruta));
                if (datos != null) {
                    foreach (var rel in datos) {
                        if (rel.Count >= 2) mapa[rel[0].Trim()] = rel[1].Trim();
                    }
                }
            } catch { }
            return mapa;
        }

        public void GuardarGrupos(Dictionary<string, string> grupos)
        {
            var ruta = Path.Combine(_carpeta, "grupo.json");
            var lista = new List<List<string>>();
            foreach (var kv in grupos) lista.Add(new List<string> { kv.Key, kv.Value });
            File.WriteAllText(ruta, JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}