using System;
using System.Linq;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ConfiguracionParcialesService
{
    private const string ClaveGlobal = "GLOBAL";

    public ConfiguracionParciales ObtenerConfiguracion(string claveMateria = "")
    {
        try
        {
            using var lite = new SqliteService();

            // Si no se especifica materia, se usa la clave "GLOBAL"
            string clave = string.IsNullOrWhiteSpace(claveMateria) ? ClaveGlobal : claveMateria;

            var configs = lite.GetAllConfiguraciones().ToList();

            // 1. Buscar la configuración específica solicitada
            var match = configs.FirstOrDefault(c => string.Equals(c.Key, clave, StringComparison.OrdinalIgnoreCase));
            if (match.Value != null) 
                return match.Value;

            // 2. Si se pidió una materia específica pero no tiene config propia, fallback a "GLOBAL"
            if (!clave.Equals(ClaveGlobal, StringComparison.OrdinalIgnoreCase))
            {
                var globalCfg = configs.FirstOrDefault(c => string.Equals(c.Key, ClaveGlobal, StringComparison.OrdinalIgnoreCase));
                if (globalCfg.Value != null) 
                    return globalCfg.Value;
            }

            // 3. Si no existe nada aún en la BD, retorna valores por defecto
            return new ConfiguracionParciales();
        }
        catch
        {
            return new ConfiguracionParciales();
        }
    }

    public void GuardarConfiguracion(string claveMateria, ConfiguracionParciales cfg)
    {
        if (cfg == null) return;
        try
        {
            // Si la clave viene vacía o nula, guardamos directamente como "GLOBAL" dentro de parciales.db
            string clave = string.IsNullOrWhiteSpace(claveMateria) ? ClaveGlobal : claveMateria;

            // Si la evaluación EXTRA está habilitada, no debe coexistir con otras evaluaciones.
            // Forzamos las demás evaluaciones a false cuando ExtraHabilitado == true.
            if (cfg.ExtraHabilitado)
            {
                cfg.Parcial1Habilitado = false;
                cfg.Parcial2Habilitado = false;
                cfg.Parcial3Habilitado = false;
                cfg.SemestralHabilitado = false;
                cfg.PreExtraordinarioHabilitado = false;
            }

            using var lite = new SqliteService();
            lite.SaveConfiguracion(clave, cfg);
        }
        catch
        {
        }
    }
}