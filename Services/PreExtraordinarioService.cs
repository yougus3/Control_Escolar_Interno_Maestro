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

    public string Mensaje { get; init; } =
        string.Empty;
}

public sealed class PreEstado
{
    public bool TienePRE { get; init; }

    public int? Calificacion { get; init; }

    public string Estado { get; init; } =
        string.Empty;

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
    public string Matricula { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string Grupo { get; set; } =
        string.Empty;

    public string P1 { get; set; } =
        string.Empty;

    public string P2 { get; set; } =
        string.Empty;

    public string P3 { get; set; } =
        string.Empty;

    public string Promedio { get; set; } =
        string.Empty;

    public string Pre { get; set; } =
        string.Empty;

    public string Estado { get; set; } =
        string.Empty;
}

public class PreExtraordinarioService
{
    private const string PreHizo =
        "__PRE_HIZO__";

    private const string PreCalificacion =
        "__PRE_CALIFICACION__";

    private const string PreEstado =
        "__PRE_ESTADO__";

    private const string PreSemOriginal =
        "__PRE_SEM_ORIGINAL__";

    private const string PreSemOriginalValido =
        "__PRE_SEM_ORIGINAL_VALIDO__";

    private readonly ParcialJsonService
        _parcialJsonService;

    public PreExtraordinarioService()
    {
        _parcialJsonService =
            new ParcialJsonService();
    }

    // =========================================================================
    // CLAVE DEL REGISTRO PRE
    //
    // EJEMPLO:
    // CALIF__PRE
    // =========================================================================

    private static string ObtenerClavePre(
        string capBaseName)
    {
        return $"{capBaseName.Trim()}_PRE";
    }

    // =========================================================================
    // ALUMNOS DE PRE
    // =========================================================================

    public List<PreAlumnoData> ObtenerAlumnosParaPre(
        string claveMateriaBase,
        IEnumerable<Alumno> alumnos)
    {
        var lista =
            new List<PreAlumnoData>();

        if (string.IsNullOrWhiteSpace(
                claveMateriaBase))
        {
            return lista;
        }

        foreach (var alumno in
                 alumnos ??
                 Enumerable.Empty<Alumno>())
        {
            if (alumno == null)
                continue;

            string matricula =
                alumno.Matricula?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    matricula))
            {
                continue;
            }

            var estado =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    matricula);

            lista.Add(
                new PreAlumnoData
                {
                    Matricula =
                        matricula,

                    Nombre =
                        alumno.Nombre
                        ?? string.Empty,

                    Grupo =
                        alumno.Grupo
                        ?? string.Empty,

                    P1 =
                        NormalizarVisual(
                            alumno.Calificación["P1"]),

                    P2 =
                        NormalizarVisual(
                            alumno.Calificación["P2"]),

                    P3 =
                        NormalizarVisual(
                            alumno.Calificación["P3"]),

                    Promedio =
                        CalcularPromedio(
                            alumno.Calificación["P1"],
                            alumno.Calificación["P2"],
                            alumno.Calificación["P3"]),

                    Pre =
                        estado.Calificacion.HasValue
                            ? estado.Calificacion.Value.ToString(
                                CultureInfo.InvariantCulture)
                            : string.Empty,

                    Estado =
                        estado.Estado
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
        if (string.IsNullOrWhiteSpace(
                claveMateriaBase) ||
            string.IsNullOrWhiteSpace(
                matricula))
        {
            return new PreEstado();
        }

        try
        {
            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    ObtenerClavePre(
                        claveMateriaBase));

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

            int? calificacion =
                null;

            if (registro.TryGetValue(
                    PreCalificacion,
                    out double valorPre))
            {
                calificacion =
                    (int)Math.Round(
                        valorPre,
                        MidpointRounding.AwayFromZero);
            }

            string estado =
                string.Empty;

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
                TienePRE =
                    true,

                Calificacion =
                    calificacion,

                Estado =
                    estado
            };
        }
        catch
        {
            return new PreEstado();
        }
    }

    // =========================================================================
    // GUARDAR RESULTADO PRE
    // =========================================================================

    public PreOperacionResultado GuardarResultadoPre(
        string claveMateriaBase,
        string matricula,
        int resultado,
        IEnumerable<Alumno> alumnos)
    {
        if (string.IsNullOrWhiteSpace(
                claveMateriaBase))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "No se pudo determinar la materia."
            };
        }

        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "No se pudo determinar la matrícula."
            };
        }

        // =====================================================================
        // PRE SOLO ADMITE 0 A 6.
        // NP CANCELADO.
        // =====================================================================

        if (resultado < 0 ||
            resultado > 6)
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
                a =>
                    a != null &&
                    string.Equals(
                        a.Matricula,
                        matricula,
                        StringComparison.OrdinalIgnoreCase));

        if (alumno == null)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "No se encontró el alumno."
            };
        }

        try
        {
            string claveAlumno =
                matricula.Trim();

            string clavePre =
                ObtenerClavePre(
                    claveMateriaBase);

            var estadoAnterior =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    claveAlumno);

            // =====================================================================
            // CARGAR PRE
            // =====================================================================

            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    clavePre);

            materiaPre.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            // =====================================================================
            // SI EL PRE YA EXISTÍA Y CAMBIAMOS DE PA A PR:
            //
            // PRIMERO RESTAURAMOS EL ESTADO ORIGINAL PERSISTENTE.
            // =====================================================================

            if (estadoAnterior.TienePRE &&
                estadoAnterior.Aprobado &&
                resultado < 6)
            {
                var restauracion =
                    RestaurarOriginalesPersistidos(
                        claveMateriaBase,
                        claveAlumno,
                        alumno);

                if (!restauracion.Exito)
                    return restauracion;

                // IMPORTANTE:
                // NO destruimos PreOriginals.
                // Se conserva porque el alumno sigue teniendo PRE,
                // ahora como PR.
                //
                // Solo actualizamos su registro PRE.
                LimpiarRegistroPre(
                    materiaPre,
                    claveAlumno);
            }
            else if (estadoAnterior.TienePRE)
            {
                // Si simplemente cambia de PR 3 a PR 5,
                // no tocamos P1/P2/P3.
                //
                // Solo reemplazamos el registro PRE.
                LimpiarRegistroPre(
                    materiaPre,
                    claveAlumno);
            }

            // =====================================================================
            // PRE REPROBADO: 0 A 5
            //
            // LAS CALIFICACIONES ORIGINALES YA QUEDARON RESTAURADAS.
            // =====================================================================

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
                    clavePre,
                    materiaPre);

                return new PreOperacionResultado
                {
                    Exito = true,
                    Mensaje =
                        "PRE registrado como PR y se restauró el estado original del alumno."
                };
            }

            // =====================================================================
            // PRE APROBADO = 6
            // =====================================================================

            if (!TryObtenerDouble(
                    alumno.Calificación["P1"],
                    out _))
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

            if (m1 == null ||
                m2 == null ||
                m3 == null)
            {
                return new PreOperacionResultado
                {
                    Exito = false,
                    Mensaje =
                        "No se pudieron cargar P1, P2 y P3 desde LiteDB."
                };
            }

            m1.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            m2.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            m3.Calificaciones ??=
                new Dictionary<string, Dictionary<string, double>>(
                    StringComparer.OrdinalIgnoreCase);

            // =====================================================================
            // RESPALDAR P1, P2 Y P3 EN PreOriginals
            //
            // ESTA ES LA PARTE IMPORTANTE QUE FALTABA.
            // =====================================================================

            RespaldarOriginal(
                m1,
                claveAlumno);

            RespaldarOriginal(
                m2,
                claveAlumno);

            RespaldarOriginal(
                m3,
                claveAlumno);

            // =====================================================================
            // GUARDAR SEM ORIGINAL
            // =====================================================================

            GuardarSemOriginalEnRegistroPre(
                materiaPre,
                claveAlumno,
                alumno.Calificación["SEM"]);

            // =====================================================================
            // P1
            // =====================================================================

            AplicarSeisAlParcial(
                m1,
                claveAlumno);

            MarcarPreEnCaptura(
                m1,
                claveAlumno);

            _parcialJsonService.GuardarMateria(
                $"{claveMateriaBase}_P1",
                m1);

            // =====================================================================
            // P2
            // =====================================================================

            AplicarSeisAlParcial(
                m2,
                claveAlumno);

            MarcarPreEnCaptura(
                m2,
                claveAlumno);

            _parcialJsonService.GuardarMateria(
                $"{claveMateriaBase}_P2",
                m2);

            // =====================================================================
            // P3
            // =====================================================================

            AplicarSeisAlParcial(
                m3,
                claveAlumno);

            MarcarPreEnCaptura(
                m3,
                claveAlumno);

            _parcialJsonService.GuardarMateria(
                $"{claveMateriaBase}_P3",
                m3);

            // =====================================================================
            // ACTUALIZAR ALUMNO EN MEMORIA
            // =====================================================================

            alumno.Calificación["P1"] =
                "6.0";

            alumno.Calificación["P2"] =
                "6.0";

            alumno.Calificación["P3"] =
                "6.0";

            alumno.Calificación["SEM"] =
                "6";

            // =====================================================================
            // REGISTRAR PRE APROBADO
            // =====================================================================

            RegistrarEstadoPre(
                materiaPre,
                claveAlumno,
                6,
                aprobado: true);

            _parcialJsonService.GuardarMateria(
                clavePre,
                materiaPre);

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "PRE aprobado. Se respaldaron P1, P2 y P3 y se aplicó 6.0."
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
    // RESPALDAR ORIGINAL EN PreOriginals
    // =========================================================================

    private void RespaldarOriginal(
        MateriaParcial materia,
        string matricula)
    {
        materia.PreOriginals ??=
            new Dictionary<string, PreOriginalData>(
                StringComparer.OrdinalIgnoreCase);

        // No sobrescribir jamás el original.
        if (materia.PreOriginals.ContainsKey(
                matricula))
        {
            return;
        }

        materia.PreOriginals[
            matricula] =
            new PreOriginalData
            {
                CapturasOriginal =
                    ObtenerCapturasOriginales(
                        materia,
                        matricula)
            };
    }

    // =========================================================================
    // APLICAR 6 AL PARCIAL
    // =========================================================================

    private void AplicarSeisAlParcial(
        MateriaParcial materia,
        string matricula)
    {
        if (materia == null)
            return;

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

            materia.Calificaciones[
                matricula] =
                capturas;
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

        foreach (var actividad in actividades)
        {
            capturas[
                actividad.Nombre.Trim()] =
                actividad.PuntajeMaximo *
                0.6;
        }
    }

    // =========================================================================
    // RESTAURAR ORIGINALES PERSISTIDOS
    // =========================================================================

    private PreOperacionResultado RestaurarOriginalesPersistidos(
        string claveMateriaBase,
        string matricula,
        Alumno alumno)
    {
        try
        {
            var materiaP1 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P1");

            var materiaP2 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P2");

            var materiaP3 =
                _parcialJsonService.ObtenerMateria(
                    $"{claveMateriaBase}_P3");

            if (materiaP1 == null ||
                materiaP2 == null ||
                materiaP3 == null)
            {
                return new PreOperacionResultado
                {
                    Exito = false,
                    Mensaje =
                        "No se pudieron cargar P1, P2 y P3 para restaurar el estado original."
                };
            }

            bool p1Restaurado =
                RestaurarDesdePreOriginals(
                    materiaP1,
                    matricula);

            bool p2Restaurado =
                RestaurarDesdePreOriginals(
                    materiaP2,
                    matricula);

            bool p3Restaurado =
                RestaurarDesdePreOriginals(
                    materiaP3,
                    matricula);

            if (p1Restaurado)
            {
                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_P1",
                    materiaP1);

                alumno.Calificación["P1"] =
                    FormatearCalificacion(
                        CalcularCalificacionDesdeMateria(
                            materiaP1,
                            matricula));
            }

            if (p2Restaurado)
            {
                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_P2",
                    materiaP2);

                alumno.Calificación["P2"] =
                    FormatearCalificacion(
                        CalcularCalificacionDesdeMateria(
                            materiaP2,
                            matricula));
            }

            if (p3Restaurado)
            {
                _parcialJsonService.GuardarMateria(
                    $"{claveMateriaBase}_P3",
                    materiaP3);

                alumno.Calificación["P3"] =
                    FormatearCalificacion(
                        CalcularCalificacionDesdeMateria(
                            materiaP3,
                            matricula));
            }

            // =====================================================================
            // SEM ORIGINAL
            // =====================================================================

            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    ObtenerClavePre(
                        claveMateriaBase));

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
                        out double semOriginal))
                {
                    alumno.Calificación["SEM"] =
                        FormatearCalificacion(
                            semOriginal);
                }
            }

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "P1, P2, P3 y SEM fueron restaurados desde los respaldos originales."
            };
        }
        catch (Exception ex)
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    $"Error restaurando originales: {ex.Message}"
            };
        }
    }

    // =========================================================================
    // RESTAURAR MateriaParcial DESDE PreOriginals
    // =========================================================================

    private bool RestaurarDesdePreOriginals(
        MateriaParcial materia,
        string matricula)
    {
        if (materia?.PreOriginals == null)
            return false;

        if (!materia.PreOriginals.TryGetValue(
                matricula,
                out var original) ||
            original == null)
        {
            return false;
        }

        materia.Calificaciones ??=
            new Dictionary<string, Dictionary<string, double>>(
                StringComparer.OrdinalIgnoreCase);

        materia.Calificaciones[
            matricula] =
            new Dictionary<string, double>(
                original.CapturasOriginal,
                StringComparer.OrdinalIgnoreCase);

        return true;
    }

    // =========================================================================
    // REVERTIR PRE POR ALUMNO
    // =========================================================================

    public PreOperacionResultado RevertirPrePorAlumno(
        string claveMateriaBase,
        string matricula,
        IEnumerable<Alumno>? alumnos = null)
    {
        if (string.IsNullOrWhiteSpace(
                claveMateriaBase))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "No se pudo determinar la materia."
            };
        }

        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return new PreOperacionResultado
            {
                Exito = false,
                Mensaje =
                    "No se pudo determinar la matrícula."
            };
        }

        try
        {
            string claveAlumno =
                matricula.Trim();

            var alumno =
                alumnos?.FirstOrDefault(
                    a =>
                        a != null &&
                        string.Equals(
                            a.Matricula,
                            claveAlumno,
                            StringComparison.OrdinalIgnoreCase));

            var estado =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    claveAlumno);

            if (!estado.TienePRE)
            {
                return new PreOperacionResultado
                {
                    Exito = true,
                    Mensaje =
                        "El alumno no tiene un PRE registrado."
                };
            }

            var resultado =
                RestaurarOriginalesPersistidos(
                    claveMateriaBase,
                    claveAlumno,
                    alumno ??
                    new Alumno
                    {
                        Matricula =
                            claveAlumno
                    });

            if (!resultado.Exito)
                return resultado;

            // =====================================================================
            // ELIMINAR REGISTRO PRE POR COMPLETO
            //
            // Esto es para "quitar PRE", no para PA -> PR.
            // =====================================================================

            var materiaPre =
                _parcialJsonService.ObtenerMateria(
                    ObtenerClavePre(
                        claveMateriaBase));

            if (materiaPre?.Calificaciones != null)
            {
                materiaPre.Calificaciones.Remove(
                    claveAlumno);

                _parcialJsonService.GuardarMateria(
                    ObtenerClavePre(
                        claveMateriaBase),
                    materiaPre);
            }

            // =====================================================================
            // AHORA SÍ ELIMINAMOS PreOriginals
            // PORQUE EL PRE FUE ELIMINADO COMPLETAMENTE.
            // =====================================================================

            EliminarPreOriginal(
                $"{claveMateriaBase}_P1",
                claveAlumno);

            EliminarPreOriginal(
                $"{claveMateriaBase}_P2",
                claveAlumno);

            EliminarPreOriginal(
                $"{claveMateriaBase}_P3",
                claveAlumno);

            return new PreOperacionResultado
            {
                Exito = true,
                Mensaje =
                    "PRE eliminado. P1, P2, P3, SEM y actividades fueron restaurados."
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

    // =========================================================================
    // ELIMINAR PreOriginals SOLO CUANDO EL PRE YA NO EXISTE
    // =========================================================================

    private void EliminarPreOriginal(
        string claveMateria,
        string matricula)
    {
        var materia =
            _parcialJsonService.ObtenerMateria(
                claveMateria);

        if (materia?.PreOriginals == null)
            return;

        if (!materia.PreOriginals.Remove(
                matricula))
        {
            return;
        }

        if (materia.PreOriginals.Count == 0)
        {
            materia.PreOriginals =
                null;
        }

        _parcialJsonService.GuardarMateria(
            claveMateria,
            materia);
    }

    // =========================================================================
    // LIMPIAR SOLO EL REGISTRO PRE
    // =========================================================================

    private void LimpiarRegistroPre(
        MateriaParcial materiaPre,
        string matricula)
    {
        if (materiaPre.Calificaciones == null)
            return;

        materiaPre.Calificaciones.Remove(
            matricula);
    }

    // =========================================================================
    // REGISTRAR ESTADO PRE
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

            materiaPre.Calificaciones[
                matricula] =
                registro;
        }

        registro[PreHizo] =
            1.0;

        registro[PreCalificacion] =
            resultado;

        registro[PreEstado] =
            aprobado
                ? 1.0
                : 0.0;
    }

    // =========================================================================
    // GUARDAR SEM ORIGINAL
    // =========================================================================

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

            materiaPre.Calificaciones[
                matricula] =
                registro;
        }

        if (TryObtenerDouble(
                semOriginal,
                out double sem))
        {
            registro[
                PreSemOriginal] =
                sem;

            registro[
                PreSemOriginalValido] =
                1.0;
        }
        else
        {
            registro[
                PreSemOriginal] =
                0.0;

            registro[
                PreSemOriginalValido] =
                0.0;
        }
    }

    // =========================================================================
    // MARCAR PRE EN CAPTURA
    // =========================================================================

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

            materia.Calificaciones[
                matricula] =
                captura;
        }

        captura["pre"] =
            1.0;
    }

    // =========================================================================
    // CALCULAR CALIFICACIÓN DESDE ACTIVIDADES
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

        decimal sumaPorcentajes =
            0m;

        foreach (var actividad in
                 actividades)
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
            100m /
            sumaPorcentajes;

        decimal acumulado =
            0m;

        foreach (var actividad in
                 actividades)
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

        return (double)(
            acumulado / 10m);
    }

    // =========================================================================
    // CAPTURAS ORIGINALES
    // =========================================================================

    private Dictionary<string, double>
        ObtenerCapturasOriginales(
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
                    ObtenerClavePre(
                        claveMateriaBase));

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

            foreach (var matricula in
                     matriculas)
            {
                RevertirPrePorAlumno(
                    claveMateriaBase,
                    matricula);
            }

            materiaPre =
                _parcialJsonService.ObtenerMateria(
                    ObtenerClavePre(
                        claveMateriaBase));

            if (materiaPre?.Calificaciones == null)
                return;

            materiaPre.Calificaciones.Clear();

            _parcialJsonService.GuardarMateria(
                ObtenerClavePre(
                    claveMateriaBase),
                materiaPre);
        }
        catch
        {
        }
    }

    // =========================================================================
    // COMPATIBILIDAD
    //
    // NO convierte nuevamente en 6 a alguien que ya tiene PRE.
    // =========================================================================

    public void AplicarAjusteParciales(
        string claveMateriaBase,
        IEnumerable<Alumno> alumnos,
        int actividades = 4,
        bool marcarPre = true)
    {
        if (!marcarPre)
            return;

        foreach (var alumno in
                 alumnos ??
                 Enumerable.Empty<Alumno>())
        {
            if (alumno == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                    alumno.Matricula))
            {
                continue;
            }

            var estadoPre =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    alumno.Matricula);

            if (estadoPre.TienePRE)
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
    // APLICAR ESTADO PRE PERSISTIDO AL CARGAR
    //
    // Este método NO elimina PRE y NO cambia la estructura del JSON.
    //
    // PA:
    //   se conservan los 6 ya persistidos.
    //
    // PR:
    //   se restauran P1/P2/P3 desde PreOriginals y SEM desde
    //   __PRE_SEM_ORIGINAL__.
    // =========================================================================

    public void AplicarEstadoPersistido(
        string claveMateriaBase,
        IEnumerable<Alumno> alumnos)
    {
        if (string.IsNullOrWhiteSpace(
                claveMateriaBase))
        {
            return;
        }

        foreach (var alumno in
                 alumnos ??
                 Enumerable.Empty<Alumno>())
        {
            if (alumno == null ||
                string.IsNullOrWhiteSpace(
                    alumno.Matricula))
            {
                continue;
            }

            var estado =
                ObtenerEstadoPre(
                    claveMateriaBase,
                    alumno.Matricula);

            if (!estado.TienePRE)
                continue;

            if (estado.Aprobado &&
                estado.Calificacion == 6)
            {
                alumno.Calificación["P1"] =
                    "6.0";

                alumno.Calificación["P2"] =
                    "6.0";

                alumno.Calificación["P3"] =
                    "6.0";

                alumno.Calificación["SEM"] =
                    "6";

                continue;
            }

            if (estado.Reprobado)
            {
                AplicarOriginalesPersistidosSinEliminarPRE(
                    claveMateriaBase,
                    alumno);
            }
        }
    }

    // =========================================================================
    // APLICAR ORIGINALES SIN BORRAR PRE
    // =========================================================================

    private void AplicarOriginalesPersistidosSinEliminarPRE(
        string claveMateriaBase,
        Alumno alumno)
    {
        string matricula =
            alumno.Matricula;

        var materiaP1 =
            _parcialJsonService.ObtenerMateria(
                $"{claveMateriaBase}_P1");

        var materiaP2 =
            _parcialJsonService.ObtenerMateria(
                $"{claveMateriaBase}_P2");

        var materiaP3 =
            _parcialJsonService.ObtenerMateria(
                $"{claveMateriaBase}_P3");

        if (materiaP1 != null &&
            RestaurarDesdePreOriginals(
                materiaP1,
                matricula))
        {
            alumno.Calificación["P1"] =
                FormatearCalificacion(
                    CalcularCalificacionDesdeMateria(
                        materiaP1,
                        matricula));
        }

        if (materiaP2 != null &&
            RestaurarDesdePreOriginals(
                materiaP2,
                matricula))
        {
            alumno.Calificación["P2"] =
                FormatearCalificacion(
                    CalcularCalificacionDesdeMateria(
                        materiaP2,
                        matricula));
        }

        if (materiaP3 != null &&
            RestaurarDesdePreOriginals(
                materiaP3,
                matricula))
        {
            alumno.Calificación["P3"] =
                FormatearCalificacion(
                    CalcularCalificacionDesdeMateria(
                        materiaP3,
                        matricula));
        }

        var materiaPre =
            _parcialJsonService.ObtenerMateria(
                ObtenerClavePre(
                    claveMateriaBase));

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
                    out double semOriginal))
            {
                alumno.Calificación["SEM"] =
                    FormatearCalificacion(
                        semOriginal);
            }
        }
    }

    // =========================================================================
    // LISTADO ELEGIBLES
    // =========================================================================

    public List<(string Matricula, string Grupo)>
        GenerarListadoElegibles(
            IEnumerable<Alumno> alumnos)
    {
        var lista =
            new List<(string Matricula, string Grupo)>();

        foreach (var alumno in
                 alumnos ??
                 Enumerable.Empty<Alumno>())
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
                        alumno.Matricula
                        ?? string.Empty,

                        alumno.Grupo
                        ?? string.Empty
                    ));
            }
        }

        return lista;
    }

    // =========================================================================
    // GUARDAR LISTADO
    // =========================================================================

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
                            x.Matricula
                                ?? string.Empty,

                            x.Grupo
                                ?? string.Empty
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
        valor =
            0.0;

        if (string.IsNullOrWhiteSpace(
                texto))
        {
            return false;
        }

        string limpio =
            texto
                .Trim()
                .Replace(
                    ',',
                    '.');

        return double.TryParse(
            limpio,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out valor);
    }

    private static string NormalizarVisual(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return string.Empty;
        }

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
            (p1 +
             p2 +
             p3) /
            3.0;

        promedio =
            Math.Truncate(
                promedio * 10.0) /
            10.0;

        return promedio.ToString(
            "0.0",
            CultureInfo.InvariantCulture);
    }
}