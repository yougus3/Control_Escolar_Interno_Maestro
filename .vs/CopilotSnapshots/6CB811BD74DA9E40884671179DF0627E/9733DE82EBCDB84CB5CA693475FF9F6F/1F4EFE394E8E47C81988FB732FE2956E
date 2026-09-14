using System;
using System.Collections.Generic;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

// Este servicio mantiene la API previa pero persiste en LiteDB en lugar de JSON plano.
public class ParcialJsonService
{
    public ParcialJsonService()
    {
    }

    public Dictionary<string, MateriaParcial> CargarTodo()
    {
        var dict = new Dictionary<string, MateriaParcial>(StringComparer.OrdinalIgnoreCase);
        using var lite = new LiteDbService();
        foreach (var (key, val) in lite.GetAllParciales())
        {
            dict[key] = val ?? new MateriaParcial();
        }
        return dict;
    }

    public void GuardarTodo(Dictionary<string, MateriaParcial> datos)
    {
        using var lite = new LiteDbService();
        if (datos == null) return;
        foreach (var kv in datos)
        {
            lite.SaveMateria(kv.Key, kv.Value ?? new MateriaParcial());
        }
    }

    public MateriaParcial ObtenerMateria(string claveMateria)
    {
        using var lite = new LiteDbService();
        return lite.GetMateria(claveMateria) ?? new MateriaParcial();
    }

    public void GuardarMateria(string claveMateria, MateriaParcial materia)
    {
        using var lite = new LiteDbService();
        lite.SaveMateria(claveMateria, materia ?? new MateriaParcial());
    }
}
