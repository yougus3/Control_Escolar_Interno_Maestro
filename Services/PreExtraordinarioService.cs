using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public sealed class PreOperacionResultado
{
    public bool Exito { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class PreEstado
{
    public bool TienePRE { get; init; }
    public int? Calificacion { get; init; }
    public string Estado { get; init; } = string.Empty;

    public bool Aprobado =>
        string.Equals(
            Estado,
            "PA",
            StringComparison.OrdinalIgnoreCase);

    public bool Reprobado =>
        string.Equals(
            Estado,
            "PR",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class PreAlumnoData
{
    public string Matricula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;

    public string P1 { get; set; } = string.Empty;
    public string P2 { get; set; } = string.Empty;
    public string P3 { get; set; } = string.Empty;
    public string Promedio { get; set; } = string.Empty;

    public string Pre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class PreExtraordinarioService
{
    private const string PreHizo = "__PRE_HIZO__";
    private const string PreCalificacion = "__PRE_CALIFICACION__";
    private const string PreEstado = "__PRE_ESTADO__";

    private const string PreSemOriginal = "__PRE_SEM_ORIGINAL__";
    private const string PreSemOriginalValido = "__PRE_SEM_ORIGINAL_VALIDO__";

    private readonly ParcialJsonService _parcialJsonService;

    public PreExtraordinarioService()
    {
        _parcialJsonService = new ParcialJsonService();
    }

    // =========================================================================
    // ALUMNOS DE PRE
    // =========================================================================

    public List<PreAlumnoData> ObtenerAlumnosParaPre(
        string claveMateriaBase,
        IEnumerable<Alumno> alumnos)
    {
        var lista = new List<PreAlumnoData>();

        if (string.IsNullOrWhiteSpace(claveMateriaBase))
            return lista;

        foreach (var alumno in alumnos ?? Enumerable.Empty<Alumno>())
        {
            if (alumno == null)
                continue;

            string matricula =
                alumno.Matricula?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(matricula))
                continue;

            var estado =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    matricula);

            bool p1Valido =
                TryObtenerDouble(
                    alumno.Calificación["P1"],
                    out _);

            bool p2Valido =
                TryObtenerDouble(
                    alumno.Calificación["P2"],
                    out _);

            bool p3Valido =
                TryObtenerDouble(
                    alumno.Calificación["P3"],
                    out _);

            bool semValido =
                TryObtenerDouble(
                    alumno.Calificación["SEM"],
                    out double sem);

            bool tieneCalificacionesBase =
                p1Valido &&
                p2Valido &&
                p3Valido &&
                semValido;

            bool elegiblePorSem =
                tieneCalificacionesBase &&
                sem >= 0.0 &&
                sem <= 5.99;

            /*
             * Si ya tiene PRE registrado, debe seguir apareciendo
             * aunque después de PA su SEM sea 6.
             */
            if (!elegiblePorSem && !estado.TienePRE)
                continue;

            lista.Add(
                new PreAlumnoData
                {
                    Matricula = matricula,
                    Nombre = alumno.Nombre ?? string.Empty,
                    Grupo = alumno.Grupo ?? string.Empty,

                    P1 = NormalizarVisual(
                        alumno.Calificación["P1"]),

                    P2 = NormalizarVisual(
                        alumno.Calificación["P2"]),

                    P3 = NormalizarVisual(
                        alumno.Calificación["P3"]),

                    Promedio = CalcularPromedio(
                        alumno.Calificación["P1"],
                        alumno.Calificación["P2"],
                        alumno.Calificación["P3"]),

                    Pre = estado.Calificacion.HasValue
                        ? estado.Calificacion.Value.ToString(
                            CultureInfo.InvariantCulture)
                        : string.Empty,

                    Estado = estado.Estado
                });
        }

        return lista;
    }

    // =========================================================================
    // ESTADO PRE
    // =========================================================================

    public PreEstado ObtenerEstadoPre(
        string claveMateriaBase,
        string matricula)
    {
        if (string.IsNullOrWhiteSpace(claveMateriaBase) ||
            string.IsNullOrWhiteSpace(matricula))
        {
            return new PreEstado();
        }

        try
        {
            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_PRE");

            if (materiaPre?.Calificaciones == null)
                return new PreEstado();

            if (!materiaPre.Calificaciones.TryGetValue(
                    matricula.Trim(),
                    out var registro) ||
                registro == null)
            {
                return new PreEstado();
            }

            bool hizoPre =
                registro.TryGetValue(
                    PreHizo,
                    out double hizo) &&
                hizo > 0;

            if (!hizoPre)
                return new PreEstado();

            int? calificacion = null;

            if (registro.TryGetValue(
                    PreCalificacion,
                    out double valorPre))
            {
                calificacion =
                    (int)Math.Round(
                        valorPre,
                        MidpointRounding.AwayFromZero);
            }

            string estado = string.Empty;

            if (registro.TryGetValue(
                    PreEstado,
                    out double estadoPre))
            {
                estado =
                    estadoPre >= 1.0
                        ? "PA"
                        : "PR";
            }

            return new PreEstado
            {
                TienePRE = true,
                Calificacion = calificacion,
                Estado = estado
            };
        }
        catch
        {
            return new PreEstado();
        }
    }

    // =========================================================================
    // GUARDAR PRE
    // =========================================================================

    public PreOperacionResultado GuardarResultadoPre(
        string claveMateriaBase,
        string matricula,
        int resultado,
        IEnumerable<Alumno> alumnos)
    {
        if (string.IsNullOrWhiteSpace(claveMateriaBase))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje = "No se pudo determinar la materia."
            };
        }

        if (string.IsNullOrWhiteSpace(matricula))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje = "No se pudo determinar la matrícula."
            };
        }

        if (resultado < 0 || resultado > 6)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "El PREEXTRAORDINARIO sólo acepta enteros de 0 a 6."
            };
        }

        var alumno =
            alumnos?.FirstOrDefault(
                a => a != null &&
                     string.Equals(
                         a.Matricula,
                         matricula,
                         StringComparison.OrdinalIgnoreCase));

        if (alumno == null)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje = "No se encontró el alumno."
            };
        }

        try
        {
            string claveAlumno =
                matricula.Trim();

            // -------------------------------------------------------------
            // SI YA HABÍA PRE, PRIMERO RESTAURAR EL ESTADO ANTERIOR
            // -------------------------------------------------------------

            var estadoAnterior =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    claveAlumno);

            if (estadoAnterior.TienePRE)
            {
                var restauracion =
                    RestaurarOriginalesDePre(
                        claveMateriaBase,
                        claveAlumno,
                        alumno,
                        eliminarRegistroPre: true);

                if (!restauracion.Exito)
                    return restauracion;
            }

            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_PRE");

            materiaPre.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            // -------------------------------------------------------------
            // PRE REPROBADO: 0 A 5
            //
            // NO toca P2
            // NO toca P3
            // NO toca SEM
            //
            // Sólo registra:
            // PRE = true
            // PRE_CALIFICACION = valor
            // PRE_ESTADO = PR
            // -------------------------------------------------------------

            if (resultado < 6)
            {
                GuardarSemOriginalEnRegistroPre(
                    materiaPre,
                    claveAlumno,
                    alumno.Calificación["SEM"]);

                RegistrarEstadoPre(
                    materiaPre,
                    claveAlumno,
                    resultado,
                    aprobado: false);

                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_PRE",
                    materiaPre);

                return new PreOperacionResultado
                {
                    Exito = true,
                    Mensaje =
                        "PRE registrado como PR. P2, P3 y SEM permanecen sin cambios."
                };
            }

            // -------------------------------------------------------------
            // PRE APROBADO = 6
            // -------------------------------------------------------------

            if (!TryObtenerDouble(
                    alumno.Calificación["P1"],
                    out double p1))
            {
                return new PreOperacionResultado
                {
                    Exito = false,
                    Mensaje =
                        "El alumno no tiene un P1 válido."
                };
            }

            var m1 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P1");

            var m2 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P2");

            var m3 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P3");

            if (m2 == null || m3 == null)
            {
                return new PreOperacionResultado
                {
                    Exito = false,
                    Mensaje =
                        "No se pudieron cargar P2 y P3 desde LiteDB."
                };
            }

            m2.Actividades ??=
                new List<ActividadParcial>();

            m3.Actividades ??=
                new List<ActividadParcial>();

            m2.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            m3.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            // -------------------------------------------------------------
            // RESPALDAR P2 Y P3 ANTES DE TOCARLOS
            // -------------------------------------------------------------

            m2.PreOriginals ??=
                new Dictionary<string, PreOriginalData>(
                    StringComparer.OrdinalIgnoreCase);

            m3.PreOriginals ??=
                new Dictionary<string, PreOriginalData>(
                    StringComparer.OrdinalIgnoreCase);

            if (!m2.PreOriginals.ContainsKey(claveAlumno))
            {
                m2.PreOriginals[claveAlumno] =
                    new PreOriginalData
                    {
                        CapturasOriginal =
                            ObtenerCapturasOriginales(
                                m2,
                                claveAlumno)
                    };
            }

            if (!m3.PreOriginals.ContainsKey(claveAlumno))
            {
                m3.PreOriginals[claveAlumno] =
                    new PreOriginalData
                    {
                        CapturasOriginal =
                            ObtenerCapturasOriginales(
                                m3,
                                claveAlumno)
                    };
            }

            // -------------------------------------------------------------
            // RESPALDAR SEM ORIGINAL
            // -------------------------------------------------------------

            GuardarSemOriginalEnRegistroPre(
                materiaPre,
                claveAlumno,
                alumno.Calificación["SEM"]);

            // -------------------------------------------------------------
            // PRE APROBADO: simplificar comportamiento
            // -------------------------------------------------------------
            // En lugar de ajustar actividades para alcanzar una calificación
            // objetivo, se fijan las tres calificaciones parciales a 6.0 y el
            // semestral a 6. Las demás reglas (respaldo y registro de PRE)
            // se mantienen.

            // Ajustar las capturas internas de P2 y P3 para reflejar 6.0
            // Guardamos 60% del puntaje máximo en cada actividad activa
            // de forma que el cálculo desde actividades dé 6.0.
            foreach (var materia in new[] { m1, m2, m3 })
            {
                materia.Calificaciones ??=
                    new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

                if (!materia.Calificaciones.TryGetValue(claveAlumno, out var captura) || captura == null)
                {
                    captura = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    materia.Calificaciones[claveAlumno] = captura;
                }

                var actividadesActivas = materia.Actividades?
                    .Where(a => a != null && a.Activa && a.PuntajeMaximo > 0)
                    .Take(4)
                    .ToList() ?? new List<ActividadParcial>();

                foreach (var actividad in actividadesActivas)
                {
                    string nombre = actividad.Nombre?.Trim() ?? string.Empty;
                    double valor = actividad.PuntajeMaximo * 0.6; // 60% del máximo -> contribuye a 6.0
                    captura[nombre] = valor;
                }

                // Marcar PRE en la captura
                MarcarPreEnCaptura(materia, claveAlumno);

                // Guardar materia en LiteDB
                _parcialJsonService.GuardarMateria($"{claveMateriaBase}_{(materia == m2 ? "P2" : "P3")}", materia);
            }

            // -------------------------------------------------------------
            // ACTUALIZAR MAINVIEWMODEL: fijar P1,P2,P3 a 6.0 y SEM a 6
            // -------------------------------------------------------------

            if (alumno != null)
            {
                alumno.Calificación["P1"] = "6.0";
                alumno.Calificación["P2"] = "6.0";
                alumno.Calificación["P3"] = "6.0";
                alumno.Calificación["SEM"] = "6";
            }

            // Registrar PRE aprobado
            RegistrarEstadoPre(
                materiaPre,
                claveAlumno,
                6,
                aprobado: true);

            // Guardar registro PRE en LiteDB
            _parcialJsonService.GuardarMateria(
                $"{claveMateriaBase}_PRE",
                materiaPre);

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "PRE aprobado. P1, P2 y P3 fijados a 6.0; SEM = 6; Estado = PA."
            };
        }
        catch (Exception ex)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    $"Error procesando PREEXTRAORDINARIO: {ex.Message}"
            };
        }
    }

    // =========================================================================
    // REVERTIR PRE
    // =========================================================================

    public PreOperacionResultado RevertirPrePorAlumno(
        string claveMateriaBase,
        string matricula,
        IEnumerable<Alumno>? alumnos = null)
    {
        if (string.IsNullOrWhiteSpace(claveMateriaBase))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje = "No se pudo determinar la materia."
            };
        }

        if (string.IsNullOrWhiteSpace(matricula))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje = "No se pudo determinar la matrícula."
            };
        }

        try
        {
            string claveAlumno =
                matricula.Trim();

            var alumno =
                alumnos?.FirstOrDefault(
                    a => a != null &&
                         string.Equals(
                             a.Matricula,
                             claveAlumno,
                             StringComparison.OrdinalIgnoreCase));

            var resultado =
                RestaurarOriginalesDePre(
                    claveMateriaBase,
                    claveAlumno,
                    alumno,
                    eliminarRegistroPre: true);

            if (!resultado.Exito)
                return resultado;

            if (alumno != null)
            {
                // ---------------------------------------------------------
                // LEER P2 RESTAURADO
                // ---------------------------------------------------------

                var m2 =
                    _parcialJsonService.ObtenerMateria(
                        $"{claveMateriaBase}_P2");

                if (m2 != null)
                {
                    alumno.Calificación["P2"] =
                        FormatearCalificacion(
                            CalcularCalificacionDesdeMateria(
                                m2,
                                claveAlumno));
                }

                // ---------------------------------------------------------
                // LEER P3 RESTAURADO
                // ---------------------------------------------------------

                var m3 =
                    _parcialJsonService.ObtenerMateria(
                        $"{claveMateriaBase}_P3");

                if (m3 != null)
                {
                    alumno.Calificación["P3"] =
                        FormatearCalificacion(
                            CalcularCalificacionDesdeMateria(
                                m3,
                                claveAlumno));
                }

                /*
                 * RestaurarOriginalesDePre ya actualizó
                 * alumno.Calificación["SEM"] con el valor original.
                 */
            }

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "PRE eliminado. P2, P3 y SEM fueron restaurados a sus valores originales."
            };
        }
        catch (Exception ex)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    $"No se pudo revertir el PRE: {ex.Message}"
            };
        }
    }

    private PreOperacionResultado RestaurarOriginalesDePre(
        string claveMateriaBase,
        string matricula,
        Alumno? alumno,
        bool eliminarRegistroPre)
    {
        try
        {
            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_PRE");

            string semOriginal =
                string.Empty;

            // -------------------------------------------------------------
            // RECUPERAR SEM ORIGINAL
            // -------------------------------------------------------------

            if (materiaPre?.Calificaciones != null &&
                materiaPre.Calificaciones.TryGetValue(
                    matricula,
                    out var registroPre) &&
                registroPre != null)
            {
                if (registroPre.TryGetValue(
                        PreSemOriginalValido,
                        out double valido) &&
                    valido > 0 &&
                    registroPre.TryGetValue(
                        PreSemOriginal,
                        out double semOriginalDouble))
                {
                    semOriginal =
                        FormatearCalificacion(
                            semOriginalDouble);
                }
            }

            // -------------------------------------------------------------
            // RESTAURAR P2
            // -------------------------------------------------------------

            var m2 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P2");

            if (m2?.PreOriginals != null &&
                m2.PreOriginals.TryGetValue(
                    matricula,
                    out var original2) &&
                original2 != null)
            {
                RestaurarMateriaParcial(
                    m2,
                    matricula,
                    original2);

                m2.PreOriginals.Remove(
                    matricula);

                if (m2.PreOriginals.Count == 0)
                    m2.PreOriginals = null;

                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_P2",
                    m2);
            }

            // -------------------------------------------------------------
            // RESTAURAR P3
            // -------------------------------------------------------------

            var m3 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P3");

            if (m3?.PreOriginals != null &&
                m3.PreOriginals.TryGetValue(
                    matricula,
                    out var original3) &&
                original3 != null)
            {
                RestaurarMateriaParcial(
                    m3,
                    matricula,
                    original3);

                m3.PreOriginals.Remove(
                    matricula);

                if (m3.PreOriginals.Count == 0)
                    m3.PreOriginals = null;

                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_P3",
                    m3);
            }

            // -------------------------------------------------------------
            // RESTAURAR SEM EN MAINVIEWMODEL
            // -------------------------------------------------------------

            if (alumno != null &&
                !string.IsNullOrWhiteSpace(
                    semOriginal))
            {
                alumno.Calificación["SEM"] =
                    semOriginal;
            }

            // -------------------------------------------------------------
            // ELIMINAR REGISTRO PRE
            // -------------------------------------------------------------

            if (eliminarRegistroPre &&
                materiaPre?.Calificaciones != null)
            {
                materiaPre.Calificaciones.Remove(
                    matricula);

                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_PRE",
                    materiaPre);
            }

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "Valores originales restaurados correctamente."
            };
        }
        catch (Exception ex)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    $"Error restaurando PRE: {ex.Message}"
            };
        }
    }

    // =========================================================================
    // REGISTRO PRE
    // =========================================================================

    private void RegistrarEstadoPre(
        MateriaParcial materiaPre,
        string matricula,
        int resultado,
        bool aprobado)
    {
        materiaPre.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        if (!materiaPre.Calificaciones.TryGetValue(
                matricula,
                out var registro) ||
            registro == null)
        {
            registro =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            materiaPre.Calificaciones[matricula] =
                registro;
        }

        registro[PreHizo] = 1.0;
        registro[PreCalificacion] = resultado;
        registro[PreEstado] =
            aprobado ? 1.0 : 0.0;
    }

    private void GuardarSemOriginalEnRegistroPre(
        MateriaParcial materiaPre,
        string matricula,
        string? semOriginal)
    {
        materiaPre.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        if (!materiaPre.Calificaciones.TryGetValue(
                matricula,
                out var registro) ||
            registro == null)
        {
            registro =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            materiaPre.Calificaciones[matricula] =
                registro;
        }

        if (TryObtenerDouble(
                semOriginal,
                out double sem))
        {
            registro[PreSemOriginal] = sem;
            registro[PreSemOriginalValido] = 1.0;
        }
        else
        {
            registro[PreSemOriginal] = 0.0;
            registro[PreSemOriginalValido] = 0.0;
        }
    }

    private void MarcarPreEnCaptura(
        MateriaParcial materia,
        string matricula)
    {
        materia.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        if (!materia.Calificaciones.TryGetValue(
                matricula,
                out var captura) ||
            captura == null)
        {
            captura =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            materia.Calificaciones[matricula] =
                captura;
        }

        captura["pre"] = 1.0;
    }

    // =========================================================================
    // AJUSTAR ACTIVIDADES
    // =========================================================================

    private bool AjustarActividadesParaCalificacion(
        MateriaParcial materia,
        string matricula,
        double calificacionObjetivo)
    {
        if (materia == null)
            return false;

        var actividadesActivas =
            materia.Actividades?
                .Where(
                    a =>
                        a != null &&
                        a.Activa &&
                        !string.IsNullOrWhiteSpace(
                            a.Nombre) &&
                        a.PuntajeMaximo > 0)
                .Take(4)
                .ToList()
            ?? new List<ActividadParcial>();

        if (actividadesActivas.Count == 0)
            return false;

        materia.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        if (!materia.Calificaciones.TryGetValue(
                matricula,
                out var capturas) ||
            capturas == null)
        {
            capturas =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            materia.Calificaciones[matricula] =
                capturas;
        }

        /*
         * El algoritmo real de ParcialesViewModel es:
         *
         * (obtenido / maximo) * porcentaje
         *
         * y después normaliza los porcentajes.
         *
         * Si todas las actividades tienen la proporción:
         *
         * objetivo / 10
         *
         * todas producen exactamente el objetivo.
         */
        double proporcion =
            calificacionObjetivo / 10.0;

        foreach (var actividad in actividadesActivas)
        {
            double nuevoPuntaje =
                actividad.PuntajeMaximo *
                proporcion;

            nuevoPuntaje =
                Math.Max(
                    0.0,
                    Math.Min(
                        actividad.PuntajeMaximo,
                        nuevoPuntaje));

            capturas[
                actividad.Nombre.Trim()] =
                nuevoPuntaje;
        }

        return true;
    }

    // =========================================================================
    // CALCULAR PARCIAL DESDE LAS ACTIVIDADES
    // =========================================================================

    private double CalcularCalificacionDesdeMateria(
        MateriaParcial materia,
        string matricula)
    {
        if (materia == null)
            return 0.0;

        if (!materia.Calificaciones.TryGetValue(
                matricula,
                out var capturas) ||
            capturas == null)
        {
            return 0.0;
        }

        var actividades =
            materia.Actividades?
                .Where(
                    a =>
                        a != null &&
                        a.Activa &&
                        !string.IsNullOrWhiteSpace(
                            a.Nombre) &&
                        a.PuntajeMaximo > 0)
                .Take(4)
                .ToList()
            ?? new List<ActividadParcial>();

        if (actividades.Count == 0)
            return 0.0;

        decimal sumaPorcentajes = 0m;

        foreach (var actividad in actividades)
        {
            if (!capturas.TryGetValue(
                    actividad.Nombre.Trim(),
                    out double obtenido))
            {
                return 0.0;
            }

            if (obtenido < 0 ||
                obtenido > actividad.PuntajeMaximo)
            {
                return 0.0;
            }

            sumaPorcentajes +=
                (decimal)actividad.Porcentaje;
        }

        if (sumaPorcentajes <= 0)
            return 0.0;

        decimal escala =
            100m / sumaPorcentajes;

        decimal acumulado = 0m;

        foreach (var actividad in actividades)
        {
            double obtenido =
                capturas[
                    actividad.Nombre.Trim()];

            decimal porcentajeNormalizado =
                (decimal)actividad.Porcentaje *
                escala;

            acumulado +=
                ((decimal)obtenido /
                 (decimal)actividad.PuntajeMaximo) *
                porcentajeNormalizado;
        }

        decimal calificacion = acumulado / 10m;

        // Devolvemos la calificación con la máxima precisión posible aquí.
        // La presentación (UI / PDF) debe truncar a 1 decimal cuando se requiera.
        return (double)calificacion;
    }

    // =========================================================================
    // RESTAURAR ACTIVIDADES
    // =========================================================================

    private void RestaurarMateriaParcial(
        MateriaParcial materia,
        string matricula,
        PreOriginalData original)
    {
        if (materia == null ||
            original == null)
        {
            return;
        }

        materia.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        materia.Calificaciones[matricula] =
            new Dictionary<string, double>(
                original.CapturasOriginal,
                StringComparer.OrdinalIgnoreCase);
    }

    /*
     * Este overload corrige las llamadas que restauran desde
     * PreOriginals automáticamente.
     */
    private void RestaurarMateriaParcial(
        MateriaParcial materia,
        string matricula)
    {
        if (materia == null)
            return;

        if (materia.PreOriginals == null)
            return;

        if (!materia.PreOriginals.TryGetValue(
                matricula,
                out var original) ||
            original == null)
        {
            return;
        }

        RestaurarMateriaParcial(
            materia,
            matricula,
            original);
    }

    private Dictionary<string, double> ObtenerCapturasOriginales(
        MateriaParcial materia,
        string matricula)
    {
        if (materia.Calificaciones.TryGetValue(
                matricula,
                out var capturas) &&
            capturas != null)
        {
            return new Dictionary<string, double>(
                capturas,
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, double>(
            StringComparer.OrdinalIgnoreCase);
    }

    // =========================================================================
    // REVERTIR TODA LA MATERIA
    // =========================================================================

    public void RevertirPreParaMateria(
        string claveMateriaBase)
    {
        if (string.IsNullOrWhiteSpace(
                claveMateriaBase))
        {
            return;
        }

        try
        {
            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_PRE");

            if (materiaPre?.Calificaciones == null)
                return;

            var matriculas =
                materiaPre.Calificaciones.Keys
                    .Where(
                        k =>
                            !string.Equals(
                                k,
                                "$CONFIG$",
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();

            foreach (var matricula in matriculas)
            {
                RevertirPrePorAlumno(
                    claveMateriaBase,
                    matricula);
            }

            materiaPre.Calificaciones.Clear();

            _parcialJsonService.GuardarMateria(
                $"{claveMateriaBase}_PRE",
                materiaPre);
        }
        catch
        {
        }
    }

    // =========================================================================
    // COMPATIBILIDAD CON CÓDIGO ANTERIOR
    // =========================================================================

    public void AplicarAjusteParciales(
        string claveMateriaBase,
        IEnumerable<Alumno> alumnos,
        int actividades = 4,
        bool marcarPre = true)
    {
        if (!marcarPre)
            return;

        foreach (var alumno in alumnos ?? Enumerable.Empty<Alumno>())
        {
            if (alumno == null)
                continue;

            if (!EsSemestralReprobada(
                    alumno.Calificación["SEM"]))
            {
                continue;
            }

            GuardarResultadoPre(
                claveMateriaBase,
                alumno.Matricula,
                6,
                alumnos);
        }
    }

    // =========================================================================
    // LISTADO DE ELEGIBLES
    // =========================================================================

    public List<(string Matricula, string Grupo)> GenerarListadoElegibles(
        IEnumerable<Alumno> alumnos)
    {
        var lista =
            new List<(string Matricula, string Grupo)>();

        foreach (var alumno in alumnos ?? Enumerable.Empty<Alumno>())
        {
            if (alumno == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                    alumno.Calificación["P1"]) ||
                string.IsNullOrWhiteSpace(
                    alumno.Calificación["P2"]) ||
                string.IsNullOrWhiteSpace(
                    alumno.Calificación["P3"]) ||
                string.IsNullOrWhiteSpace(
                    alumno.Calificación["SEM"]))
            {
                continue;
            }

            if (!TryObtenerDouble(
                    alumno.Calificación["SEM"],
                    out double sem))
            {
                continue;
            }

            if (sem >= 0.0 &&
                sem <= 5.99)
            {
                lista.Add(
                    (
                        alumno.Matricula ?? string.Empty,
                        alumno.Grupo ?? string.Empty
                    ));
            }
        }

        return lista;
    }

    public void GuardarListadoAArchivo(
        IEnumerable<(string Matricula, string Grupo)> lista,
        string rutaSalida)
    {
        try
        {
            var arrays =
                lista.Select(
                    x =>
                        new[]
                        {
                            x.Matricula ?? string.Empty,
                            x.Grupo ?? string.Empty
                        })
                .ToArray();

            var options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string json =
                JsonSerializer.Serialize(
                    arrays,
                    options);

            File.WriteAllText(
                rutaSalida,
                json);
        }
        catch
        {
        }
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private static bool EsSemestralReprobada(
        string? texto)
    {
        if (!TryObtenerDouble(
                texto,
                out double sem))
        {
            return false;
        }

        return sem >= 0.0 &&
               sem <= 5.99;
    }

    private static bool TryObtenerDouble(
        string? texto,
        out double valor)
    {
        valor = 0.0;

        if (string.IsNullOrWhiteSpace(texto))
            return false;

        string limpio =
            texto.Trim().Replace(',', '.');

        return double.TryParse(
            limpio,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out valor);
    }

    private static string NormalizarVisual(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        if (!TryObtenerDouble(
                valor,
                out double numero))
        {
            return valor;
        }

        return FormatearCalificacion(
            numero);
    }

    private static string FormatearCalificacion(
        double valor)
    {
        return valor.ToString(
            "0.##",
            CultureInfo.InvariantCulture);
    }

    private static string CalcularPromedio(
        string? p1Texto,
        string? p2Texto,
        string? p3Texto)
    {
        if (!TryObtenerDouble(
                p1Texto,
                out double p1))
        {
            return string.Empty;
        }

        if (!TryObtenerDouble(
                p2Texto,
                out double p2))
        {
            return string.Empty;
        }

        if (!TryObtenerDouble(
                p3Texto,
                out double p3))
        {
            return string.Empty;
        }

        double promedio =
            (p1 + p2 + p3) / 3.0;

        promedio =
            Math.Truncate(
                promedio * 10.0) /
            10.0;

        return promedio.ToString(
            "0.0",
            CultureInfo.InvariantCulture);
    }
}