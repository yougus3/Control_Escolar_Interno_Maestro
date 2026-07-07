using System;
using System.Collections.Generic;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public class DataSyncService
    {
        public DataSyncService()
        {
        }

        public IEnumerable<(string Key, MateriaParcial Value)> CargarParciales()
        {
            using var lite = new LiteDbService();
            foreach (var kv in lite.GetAllParciales()) yield return kv;
        }

        public void GuardarParciales(IEnumerable<(string Key, MateriaParcial Value)> items)
        {
            using var lite = new LiteDbService();
            foreach (var kv in items) lite.SaveMateria(kv.Key, kv.Value ?? new MateriaParcial());
        }

        public IEnumerable<(string Key, ConfiguracionParciales Value)> CargarConfiguraciones()
        {
            using var lite = new LiteDbService();
            foreach (var kv in lite.GetAllConfiguraciones()) yield return kv;
        }

        public void GuardarConfiguraciones(IEnumerable<(string Key, ConfiguracionParciales Value)> items)
        {
            using var lite = new LiteDbService();
            foreach (var kv in items) lite.SaveConfiguracion(kv.Key, kv.Value ?? new ConfiguracionParciales());
        }

        public Dictionary<string, string> CargarGrupos()
        {
            using var lite = new LiteDbService();
            return lite.GetGrupos();
        }

        public void GuardarGrupos(Dictionary<string, string> grupos)
        {
            using var lite = new LiteDbService();
            lite.SaveGrupos(grupos ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }
    }
}