using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public class DataSyncService
    {
        private readonly string _dbPath;

        public DataSyncService()
        {
            var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            _dbPath = Path.Combine(carpeta, "data.db");
        }

        public IEnumerable<(string Key, MateriaParcial Value)> CargarParciales()
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<ParcialJsonService.StoredMateria>("parciales");
            foreach (var item in col.FindAll())
            {
                yield return (item.Id, item.Value ?? new MateriaParcial());
            }
        }

        public void GuardarParciales(IEnumerable<(string Key, MateriaParcial Value)> items)
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<ParcialJsonService.StoredMateria>("parciales");
            col.DeleteAll();
            foreach (var kv in items)
            {
                col.Upsert(new ParcialJsonService.StoredMateria { Id = kv.Key, Value = kv.Value ?? new MateriaParcial() });
            }
        }

        public IEnumerable<(string Key, ConfiguracionParciales Value)> CargarConfiguraciones()
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<ConfiguracionParcialesService.StoredConfig>("configuraciones");
            foreach (var item in col.FindAll())
            {
                yield return (item.Id, item.Value ?? new ConfiguracionParciales());
            }
        }

        public void GuardarConfiguraciones(IEnumerable<(string Key, ConfiguracionParciales Value)> items)
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<ConfiguracionParcialesService.StoredConfig>("configuraciones");
            col.DeleteAll();
            foreach (var kv in items)
            {
                col.Upsert(new ConfiguracionParcialesService.StoredConfig { Id = kv.Key, Value = kv.Value ?? new ConfiguracionParciales() });
            }
        }

        public Dictionary<string, string> CargarGrupos()
        {
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<CapParserService.GrupoEntry>("grupos");
            foreach (var g in col.FindAll())
            {
                if (!string.IsNullOrWhiteSpace(g.Matricula) && !mapa.ContainsKey(g.Matricula)) mapa[g.Matricula] = g.Grupo ?? string.Empty;
            }
            return mapa;
        }

        public void GuardarGrupos(Dictionary<string, string> grupos)
        {
            using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            var col = db.GetCollection<CapParserService.GrupoEntry>("grupos");
            col.DeleteAll();
            foreach (var kv in grupos)
            {
                col.Insert(new CapParserService.GrupoEntry { Matricula = kv.Key, Grupo = kv.Value });
            }
        }
    }
}
