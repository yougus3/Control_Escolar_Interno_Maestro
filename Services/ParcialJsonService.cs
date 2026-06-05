using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ParcialJsonService
{
    private readonly string _rutaJson;

    public ParcialJsonService()
    {
        _rutaJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "parciales.json");
        CrearArchivoSiNoExiste();
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

    public Dictionary<string, MateriaParcial> CargarTodo()
    {
        try
        {
            string json = File.ReadAllText(_rutaJson);
            var datos = JsonSerializer.Deserialize<Dictionary<string, MateriaParcial>>(json);
            return datos ?? new Dictionary<string, MateriaParcial>();
        }
        catch
        {
            return new Dictionary<string, MateriaParcial>();
        }
    }

    public void GuardarTodo(Dictionary<string, MateriaParcial> datos)
    {
        var opciones = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(datos, opciones);
        File.WriteAllText(_rutaJson, json);
    }

    public MateriaParcial ObtenerMateria(string claveMateria)
    {
        var datos = CargarTodo();

        if (datos.TryGetValue(claveMateria, out MateriaParcial? materia))
        {
            return materia;
        }

        return new MateriaParcial();
    }

    public void GuardarMateria(string claveMateria, MateriaParcial materia)
    {
        var datos = CargarTodo();
        datos[claveMateria] = materia;
        GuardarTodo(datos);
    }
}