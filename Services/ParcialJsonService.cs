using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ParcialJsonService
{
    private readonly string _rutaParciales;

    public ParcialJsonService()
    {
        var carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        _rutaParciales = Path.Combine(carpeta, "parciales.json");
    }

    public Dictionary<string, MateriaParcial> CargarTodo()
    {
        if (!File.Exists(_rutaParciales)) return new Dictionary<string, MateriaParcial>();

        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var json = File.ReadAllText(_rutaParciales);
                return JsonSerializer.Deserialize<Dictionary<string, MateriaParcial>>(json) ?? new Dictionary<string, MateriaParcial>();
            }
            catch (IOException) when (attempt < maxAttempts - 1)
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(datos, options);
                File.WriteAllText(_rutaParciales, json);
                return;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
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
        var todo = CargarTodo();
        return todo.TryGetValue(claveMateria, out var materia) ? materia : new MateriaParcial();
    }

    public void GuardarMateria(string claveMateria, MateriaParcial materia)
    {
        var todo = CargarTodo();
        todo[claveMateria] = materia ?? new MateriaParcial();
        GuardarTodo(todo);
    }
}