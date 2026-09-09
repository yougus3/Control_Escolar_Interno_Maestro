using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class CapParserService
{
    public CapParserService()
    {
    }

    // ============================================================
    // GRUPOS
    // ============================================================
    //
    // LiteDbService.GetGrupos() obtiene actualmente los grupos
    // desde configuracion.bin.
    //
    // CapParserService no necesita conocer cómo se descifra
    // configuracion.bin. Esa responsabilidad pertenece a
    // LiteDbService.
    //
    // ============================================================

    private Dictionary<string, string> CargarMapaGrupos()
    {
        try
        {
            using var lite = new SqliteService();

            return lite.GetGrupos();
        }
        catch
        {
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }
    }

    public Dictionary<string, string> ObtenerTodosGrupos()
    {
        return CargarMapaGrupos();
    }

    // ============================================================
    // CONSERVADO POR COMPATIBILIDAD
    // ============================================================
    //
    // No se elimina porque otras partes de CEIM pueden seguir
    // llamando a GuardarTodosGrupos().
    //
    // LiteDbService.SaveGrupos() ya NO escribe grupo.json.
    //
    // ============================================================

    public void GuardarTodosGrupos(
        Dictionary<string, string> grupos)
    {
        try
        {
            using var lite = new SqliteService();

            lite.SaveGrupos(
                grupos ??
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
        }
    }

    // ============================================================
    // NOMBRE VISUAL DEL ARCHIVO
    // ============================================================

    public string ObtenerNombreVisualArchivo(
        string filePath)
    {
        if (!File.Exists(filePath))
            return Path.GetFileNameWithoutExtension(filePath);

        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        var encodingCap =
            Encoding.GetEncoding("iso-8859-1");

        var lineas =
            File.ReadAllLines(
                filePath,
                encodingCap);

        string clave = string.Empty;
        string asignatura = string.Empty;
        string nombreProfesor = string.Empty;

        bool isExtra = false;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (l.StartsWith(
                    "CLAVEASIGNATURA=",
                    StringComparison.OrdinalIgnoreCase))
            {
                clave =
                    l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith(
                         "ASIGNATURA_STR=",
                         StringComparison.OrdinalIgnoreCase))
            {
                asignatura =
                    l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith(
                         "NombreProfesor=",
                         StringComparison.OrdinalIgnoreCase))
            {
                nombreProfesor =
                    l.Split('=', 2)[1].Trim();
            }
            else if (
                l.EndsWith(
                    "=EXTRA",
                    StringComparison.OrdinalIgnoreCase))
            {
                isExtra = true;
            }
        }

        string resultado =
            string.IsNullOrWhiteSpace(clave)
                ? asignatura
                : string.IsNullOrWhiteSpace(asignatura)
                    ? clave
                    : $"{clave} {asignatura}";

        // ========================================================
        // EXTRA
        // ========================================================

        // REGLA:
        // Si es EXTRA, mostramos obligatoriamente el Profesor
        // en el apartado de Grupo.
        if (isExtra)
        {
            if (string.IsNullOrWhiteSpace(nombreProfesor))
                nombreProfesor = "SIN REGISTRO";

            return
                $"{resultado} - Grupo: {nombreProfesor}";
        }

        // ========================================================
        // SIN INFORMACIÓN
        // ========================================================

        if (string.IsNullOrWhiteSpace(clave) &&
            string.IsNullOrWhiteSpace(asignatura))
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        // ========================================================
        // OBTENER GRUPO DESDE CONFIGURACION.BIN
        // ========================================================

        try
        {
            var mapa = CargarMapaGrupos();

            bool inAlumno = false;

            foreach (var linea in lineas)
            {
                var l = linea.Trim();

                if (l.StartsWith(
                        "[Alumno_",
                        StringComparison.OrdinalIgnoreCase) &&
                    l.EndsWith("]"))
                {
                    inAlumno = true;
                    continue;
                }

                if (!inAlumno)
                    continue;

                if (l.StartsWith(
                        "Matricula",
                        StringComparison.OrdinalIgnoreCase) &&
                    l.Contains('='))
                {
                    var partes =
                        l.Split('=', 2);

                    if (partes.Length == 2)
                    {
                        var matricula =
                            partes[1].Trim();

                        if (mapa.TryGetValue(
                                matricula,
                                out var grupo) &&
                            !string.IsNullOrWhiteSpace(grupo))
                        {
                            resultado =
                                $"{resultado} ({grupo})";
                        }

                        break;
                    }
                }
            }
        }
        catch
        {
        }

        return resultado;
    }

    // ============================================================
    // INFORMACIÓN PARA COMBO
    // ============================================================

    public (
        string NombreBase,
        bool IsExtra,
        string GrupoCap,
        string NombreProfesor)
        ObtenerInfoParaCombo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (
                Path.GetFileNameWithoutExtension(filePath),
                false,
                "S/G",
                "");
        }

        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        var encodingCap =
            Encoding.GetEncoding("iso-8859-1");

        var lineas =
            File.ReadAllLines(
                filePath,
                encodingCap);

        string clave = string.Empty;
        string asignatura = string.Empty;
        string grupoCap = "S/G";
        string nombreProfesor = string.Empty;

        bool isExtra = false;

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (l.StartsWith(
                    "CLAVEASIGNATURA=",
                    StringComparison.OrdinalIgnoreCase))
            {
                clave =
                    l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith(
                         "ASIGNATURA_STR=",
                         StringComparison.OrdinalIgnoreCase))
            {
                asignatura =
                    l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith(
                         "Grupo=",
                         StringComparison.OrdinalIgnoreCase))
            {
                grupoCap =
                    l.Split('=', 2)[1].Trim();
            }
            else if (l.StartsWith(
                         "NombreProfesor=",
                         StringComparison.OrdinalIgnoreCase))
            {
                nombreProfesor =
                    l.Split('=', 2)[1].Trim();
            }
            else if (
                l.EndsWith(
                    "=EXTRA",
                    StringComparison.OrdinalIgnoreCase))
            {
                isExtra = true;
            }
        }

        string nombreBase =
            string.IsNullOrWhiteSpace(clave)
                ? asignatura
                : string.IsNullOrWhiteSpace(asignatura)
                    ? clave
                    : $"{clave} {asignatura}";

        if (string.IsNullOrWhiteSpace(nombreBase))
        {
            nombreBase =
                Path.GetFileNameWithoutExtension(filePath);
        }

        return (
            nombreBase,
            isExtra,
            grupoCap,
            nombreProfesor);
    }

    // ============================================================
    // PROCESAR ARCHIVO COMPLETO
    // ============================================================

    public CapParseResult ProcesarArchivoCompleto(
        string filePath)
    {
        var resultado =
            new CapParseResult();

        if (!File.Exists(filePath))
            return resultado;

        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        var encodingCap =
            Encoding.GetEncoding("iso-8859-1");

        var lineas =
            File.ReadAllLines(
                filePath,
                encodingCap);

        // ========================================================
        // IMPORTANTE:
        // El mapa de grupos ahora proviene de configuracion.bin
        // a través de LiteDbService.GetGrupos().
        // ========================================================

        var mapaGrupos =
            CargarMapaGrupos();

        var mapaEvaluaciones =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        bool enSeccionEvaluaciones = false;

        // ========================================================
        // EVALUACIONES
        // ========================================================

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (string.IsNullOrWhiteSpace(l))
                continue;

            if (l.StartsWith(
                    "[",
                    StringComparison.OrdinalIgnoreCase) &&
                l.EndsWith("]"))
            {
                enSeccionEvaluaciones =
                    l.Equals(
                        "[Evaluaciones]",
                        StringComparison.OrdinalIgnoreCase);

                continue;
            }

            if (!enSeccionEvaluaciones)
                continue;

            if (l.StartsWith(
                    "ID_EVAL",
                    StringComparison.OrdinalIgnoreCase) &&
                l.Contains('=') &&
                l.Contains(
                    "_STR",
                    StringComparison.OrdinalIgnoreCase))
            {
                var partes =
                    l.Split('=', 2);

                if (partes.Length == 2)
                {
                    string claveCompleta =
                        partes[0].Trim();

                    string nombreColumnaReal =
                        partes[1].Trim();

                    string idEval =
                        claveCompleta
                            .Replace("ID_", "")
                            .Replace("_STR", "");

                    if (
                        idEval.Equals(
                            "RESFINAL",
                            StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals(
                            "RESFINAL",
                            StringComparison.OrdinalIgnoreCase) ||
                        idEval.Equals(
                            "PROMSEM",
                            StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals(
                            "PROMSEM",
                            StringComparison.OrdinalIgnoreCase) ||
                        idEval.Equals(
                            "RESULSEM",
                            StringComparison.OrdinalIgnoreCase) ||
                        nombreColumnaReal.Equals(
                            "RESULSEM",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    mapaEvaluaciones[idEval] =
                        nombreColumnaReal;

                    resultado
                        .EvaluacionIdPorNombre[
                            nombreColumnaReal] =
                        idEval;
                }
            }
        }

        // ========================================================
        // ORDEN DE EVALUACIONES
        // ========================================================

        if (mapaEvaluaciones.Count > 0)
        {
            var ordenPreferente =
                new[]
                {
                    "P1",
                    "P2",
                    "P3",
                    "SEM"
                };

            var yaAgregadas =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var o in ordenPreferente)
            {
                if (mapaEvaluaciones.Values.Any(
                        v => string.Equals(
                            v,
                            o,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    resultado
                        .EvaluacionesDisponibles
                        .Add(o);

                    yaAgregadas.Add(o);
                }
            }

            foreach (
                var nombre in
                mapaEvaluaciones.Values.Distinct(
                    StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    continue;

                if (yaAgregadas.Contains(nombre))
                    continue;

                if (
                    string.Equals(
                        nombre,
                        "RESFINAL",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        nombre,
                        "PROMSEM",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        nombre,
                        "RESULSEM",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                resultado
                    .EvaluacionesDisponibles
                    .Add(nombre);

                yaAgregadas.Add(nombre);
            }
        }
        else
        {
            resultado
                .EvaluacionesDisponibles
                .Add("P1");

            resultado
                .EvaluacionesDisponibles
                .Add("P2");

            resultado
                .EvaluacionesDisponibles
                .Add("P3");

            resultado
                .EvaluacionesDisponibles
                .Add("SEM");

            // Soporte inicial para
            // PREEXTRAORDINARIO.
            resultado
                .EvaluacionesDisponibles
                .Add("PREEXTRAORDINARIO");
        }

        // ========================================================
        // CONTROL DE DERECHO A EXTRAORDINARIO
        // ========================================================

        bool archivoTieneExtra =
            mapaEvaluaciones.Values.Any(
                v => string.Equals(
                    v,
                    "EXTRA",
                    StringComparison.OrdinalIgnoreCase));

        Alumno? alumnoActual = null;

        // ========================================================
        // ALUMNOS
        // ========================================================

        foreach (var linea in lineas)
        {
            string l = linea.Trim();

            if (string.IsNullOrWhiteSpace(l))
                continue;

            // ====================================================
            // NUEVO ALUMNO
            // ====================================================

            if (
                l.StartsWith(
                    "[Alumno_",
                    StringComparison.OrdinalIgnoreCase) &&
                l.EndsWith("]"))
            {
                alumnoActual =
                    new Alumno();

                // Si este CAP maneja extras,
                // por defecto asumimos que el alumno
                // NO tiene derecho hasta encontrar
                // explícitamente su evaluación EXTRA.

                if (archivoTieneExtra)
                {
                    alumnoActual
                        .TieneDerechoExtra = false;
                }

                resultado
                    .Alumnos
                    .Add(alumnoActual);

                continue;
            }

            if (alumnoActual == null ||
                !l.Contains('='))
            {
                continue;
            }

            var partes =
                l.Split('=', 2);

            if (partes.Length < 2)
                continue;

            string key =
                partes[0].Trim();

            string val =
                partes[1].Trim();

            // ====================================================
            // MATRÍCULA
            // ====================================================

            if (
                key.Equals(
                    "Matricula",
                    StringComparison.OrdinalIgnoreCase))
            {
                alumnoActual.Matricula =
                    val;

                // =================================================
                // EL GRUPO SE OBTIENE DE configuracion.bin
                // mediante el mapa cargado por LiteDbService.
                //
                // Ya no existe ninguna lectura de grupo.json aquí.
                // =================================================

                alumnoActual.Grupo =
                    mapaGrupos.TryGetValue(
                        val,
                        out string? g)
                        ? g
                        : "S/G";
            }

            // ====================================================
            // NOMBRE
            // ====================================================

            else if (
                key.Equals(
                    "Nombre",
                    StringComparison.OrdinalIgnoreCase))
            {
                alumnoActual.Nombre =
                    val;
            }

            // ====================================================
            // CALIFICACIONES
            // ====================================================

            else if (
                key.StartsWith(
                    "CALIFICACION_",
                    StringComparison.OrdinalIgnoreCase) &&
                key.EndsWith(
                    "_STR",
                    StringComparison.OrdinalIgnoreCase))
            {
                string idEval =
                    key
                        .Replace(
                            "CALIFICACION_",
                            "")
                        .Replace(
                            "_STR",
                            "");

                if (
                    mapaEvaluaciones.TryGetValue(
                        idEval,
                        out string? columnaDestino))
                {
                    alumnoActual
                        .Calificación[
                            columnaDestino] =
                        string.IsNullOrWhiteSpace(val)
                            ? ""
                            : val;

                    // =================================================
                    // EXTRA
                    // =================================================

                    if (
                        string.Equals(
                            columnaDestino,
                            "EXTRA",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        alumnoActual
                            .TieneDerechoExtra = true;
                    }
                }
            }
        }

        return resultado;
    }

    // ============================================================
    // PROCESAR ARCHIVO
    // ============================================================

    public List<Alumno> ProcesarArchivo(
        string filePath)
    {
        return
            ProcesarArchivoCompleto(
                filePath)
            .Alumnos;
    }
}