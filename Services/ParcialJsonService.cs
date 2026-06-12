using System;
using System.Collections.Generic;
using System.IO;
using LiteDB;
using System.Text.Json;
using System.Threading;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ParcialJsonService
{
    private readonly string _dbPath;

    public ParcialJsonService()
    {
        var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        _dbPath = Path.Combine(carpeta, "data.db");
        // LiteDB will create file on first use
        EnsureMigration();
    }

    private static bool _migrated = false;
    private static readonly object _migrateLock = new object();

    public static void EnsureMigration()
    {
        if (_migrated) return;
        lock (_migrateLock)
        {
            if (_migrated) return;
            try
            {
                var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                var dbPath = Path.Combine(carpeta, "data.db");
                var rutaParciales = Path.Combine(carpeta, "parciales.json");
                var rutaConfig = Path.Combine(carpeta, "configuracion_parciales.json");
                var rutaGrupo = Path.Combine(carpeta, "grupo.json");

                using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");

                // Parciales
                var colParciales = db.GetCollection<StoredMateria>("parciales");
                if (!colParciales.FindAll().Any() && File.Exists(rutaParciales))
                {
                    try
                    {
                        var json = File.ReadAllText(rutaParciales);
                        var datos = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, MateriaParcial>>(json);
                        if (datos != null)
                        {
                            foreach (var kv in datos)
                            {
                                colParciales.Upsert(new StoredMateria { Id = kv.Key, Value = kv.Value ?? new MateriaParcial() });
                            }
                        }
                    }
                    catch { }
                }

                // Configuraciones
                var colConfig = db.GetCollection<ConfiguracionParcialesService.StoredConfig>("configuraciones");
                if (!colConfig.FindAll().Any() && File.Exists(rutaConfig))
                {
                    try
                    {
                        var json = File.ReadAllText(rutaConfig);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            try
                            {
                                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models.ConfiguracionParciales>>(json);
                                if (dict != null)
                                {
                                    foreach (var kv in dict)
                                        colConfig.Upsert(new ConfiguracionParcialesService.StoredConfig { Id = kv.Key, Value = kv.Value ?? new ConfiguracionParciales() });
                                }
                            }
                            catch
                            {
                                try
                                {
                                    var single = System.Text.Json.JsonSerializer.Deserialize<Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models.ConfiguracionParciales>(json);
                                    if (single != null)
                                    {
                                        colConfig.Upsert(new ConfiguracionParcialesService.StoredConfig { Id = "__DEFAULT__", Value = single });
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }

                // Grupo
                var colGrupo = db.GetCollection<CapParserService.GrupoEntry>("grupos");
                if (!colGrupo.FindAll().Any() && File.Exists(rutaGrupo))
                {
                    try
                    {
                        var json = File.ReadAllText(rutaGrupo, System.Text.Encoding.UTF8);
                        var datos = System.Text.Json.JsonSerializer.Deserialize<List<List<string>>>(json);
                        if (datos != null)
                        {
                            foreach (var relacion in datos)
                            {
                                if (relacion.Count >= 2)
                                {
                                    string matricula = relacion[0].Trim();
                                    string grupo = relacion[1].Trim();
                                    colGrupo.Insert(new CapParserService.GrupoEntry { Matricula = matricula, Grupo = grupo });
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            _migrated = true;
        }
    }

    public Dictionary<string, MateriaParcial> CargarTodo()
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
                var col = db.GetCollection<StoredMateria>("parciales");
                var all = col.FindAll();
                var dict = new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in all)
                {
                    if (item != null && item.Id != null)
                        dict[item.Id] = item.Value ?? new MateriaParcial();
                }
                return dict;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch (LiteException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch
            {
                return new Dictionary<string, MateriaParcial>();
            }
        }

        return new Dictionary<string, MateriaParcial>();
    }

    public void GuardarTodo(Dictionary<string, MateriaParcial> datos)
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
                var col = db.GetCollection<StoredMateria>("parciales");
                // Replace all records with provided dictionary
                col.DeleteAll();
                foreach (var kv in datos)
                {
                    col.Upsert(new StoredMateria { Id = kv.Key, Value = kv.Value ?? new MateriaParcial() });
                }
                return;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch (LiteException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch
            {
                // swallow any other exceptions to avoid crashing UI
                return;
            }
        }
    }

    public MateriaParcial ObtenerMateria(string claveMateria)
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
                var col = db.GetCollection<StoredMateria>("parciales");
                var item = col.FindById(claveMateria);
                return item?.Value ?? new MateriaParcial();
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch (LiteException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch
            {
                return new MateriaParcial();
            }
        }

        return new MateriaParcial();
    }

    public void GuardarMateria(string claveMateria, MateriaParcial materia)
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
                var col = db.GetCollection<StoredMateria>("parciales");
                col.Upsert(new StoredMateria { Id = claveMateria, Value = materia ?? new MateriaParcial() });
                return;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch (LiteException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(100);
            }
            catch
            {
                // swallow to avoid crashing UI
                return;
            }
        }
    }

    public class StoredMateria
    {
        public string Id { get; set; } = string.Empty;
        public MateriaParcial Value { get; set; } = new MateriaParcial();
    }
}