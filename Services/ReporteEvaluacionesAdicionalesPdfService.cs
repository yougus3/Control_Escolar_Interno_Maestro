using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public class ReporteEvaluacionesAdicionalesPdfService
    {
        private readonly CapParserService _parserService;
        private readonly SqliteService _sqliteService;
        private readonly PreExtraordinarioService _preService;

        public ReporteEvaluacionesAdicionalesPdfService()
        {
            _parserService = new CapParserService();
            _sqliteService = new SqliteService();
            _preService = new PreExtraordinarioService();

            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);
        }

        // ============================================================
        // GENERAR EXTRAORDINARIO
        //
        // ESTE MÉTODO CONSERVA EL COMPORTAMIENTO ACTUAL.
        // ============================================================

        public byte[] GenerarDiseno(
            IEnumerable<string> rutasCap)
        {
            if (rutasCap == null)
                throw new ArgumentNullException(
                    nameof(rutasCap));

            List<string> rutas =
                rutasCap
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (rutas.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se seleccionaron archivos CAP.");
            }

            var materias =
                new List<MateriaExtra>();

            foreach (string ruta in rutas)
            {
                if (!File.Exists(ruta))
                    continue;

                MateriaExtra? materia =
                    CargarMateria(ruta);

                if (materia != null)
                    materias.Add(materia);
            }

            if (materias.Count == 0)
            {
                throw new InvalidOperationException(
                    "No fue posible obtener información de los archivos CAP seleccionados.");
            }

            return Document
                .Create(document =>
                {
                    foreach (MateriaExtra materia
                             in materias)
                    {
                        AgregarPaginaMateria(
                            document,
                            materia);
                    }
                })
                .GeneratePdf();
        }

        // ============================================================
        // GENERAR PREEXTRAORDINARIO
        //
        // SOLO ALUMNOS QUE TIENEN PRE.
        //
        // LA CALIFICACIÓN SALE DE PreExtraordinarioService.
        // NO utiliza alumno.Calificación["EXTRA"].
        // ============================================================

        public byte[] GenerarDisenoPre(
            IEnumerable<string> rutasCap)
        {
            if (rutasCap == null)
                throw new ArgumentNullException(
                    nameof(rutasCap));

            List<string> rutas =
                rutasCap
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (rutas.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se seleccionaron archivos CAP.");
            }

            var materias =
                new List<MateriaExtra>();

            foreach (string ruta in rutas)
            {
                if (!File.Exists(ruta))
                    continue;

                MateriaExtra? materia =
                    CargarMateriaPre(ruta);

                if (materia != null &&
                    materia.Alumnos.Count > 0)
                {
                    materias.Add(materia);
                }
            }

            if (materias.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se encontraron alumnos con PRE en los archivos CAP seleccionados.");
            }

            return Document
                .Create(document =>
                {
                    foreach (MateriaExtra materia
                             in materias)
                    {
                        AgregarPaginaMateriaPre(
                            document,
                            materia);
                    }
                })
                .GeneratePdf();
        }

        // ============================================================
        // CARGAR MATERIA DESDE CAP
        //
        // ESTE MÉTODO ES EL EXISTENTE PARA EXTRA.
        // ============================================================

        private MateriaExtra? CargarMateria(
            string rutaCap)
        {
            try
            {
                List<Alumno> alumnos =
                    _parserService
                        .ProcesarArchivo(rutaCap);

                // ====================================================
                // DATOS DIRECTOS DEL CAP
                // ====================================================

                string asignatura =
                    ObtenerDatoCap(
                        rutaCap,
                        "ASIGNATURA_STR");

                string codigoGrupo =
                    ObtenerDatoCap(
                        rutaCap,
                        "CodigoGrupo");

                string grupoCap =
                    ObtenerDatoCap(
                        rutaCap,
                        "Grupo");

                string nombreProfesor =
                    ObtenerDatoCap(
                        rutaCap,
                        "NombreProfesor");

                var materia =
                    new MateriaExtra
                    {
                        RutaCap =
                            rutaCap,

                        Asignatura =
                            asignatura.Trim(),

                        CodigoGrupo =
                            codigoGrupo.Trim(),

                        GrupoCap =
                            grupoCap.Trim(),

                        NombreProfesor =
                            nombreProfesor.Trim(),

                        Alumnos =
                            new List<AlumnoExtra>()
                    };

                // ====================================================
                // ALUMNOS
                // ====================================================

                if (alumnos != null)
                {
                    foreach (Alumno alumno
                             in alumnos)
                    {
                        if (alumno == null)
                            continue;

                        string matricula =
                            alumno.Matricula?
                                .Trim()
                            ?? string.Empty;

                        string nombre =
                            alumno.Nombre?
                                .Trim()
                            ?? string.Empty;

                        string grupoTradicional =
                            ObtenerGrupoAlumno(
                                alumno,
                                matricula,
                                materia.GrupoCap);

                        string calificacionNumero =
                            ObtenerCalificacionNumero(
                                alumno);

                        string calificacionLetra =
                            ConvertirNumeroALetra(
                                calificacionNumero);

                        materia.Alumnos.Add(
                            new AlumnoExtra
                            {
                                Matricula =
                                    matricula,

                                Nombre =
                                    nombre,

                                Grupo =
                                    grupoTradicional,

                                CalificacionNumero =
                                    calificacionNumero,

                                CalificacionLetra =
                                    calificacionLetra
                            });
                    }
                }

                return materia;
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // CARGAR MATERIA PARA PRE
        //
        // AQUÍ SE CONSERVA EL CARÁCTER DE PRE.
        //
        // La materia, alumnos y datos generales siguen saliendo
        // del CAP.
        //
        // La calificación sale exclusivamente de:
        //
        // PreExtraordinarioService.ObtenerEstadoPre(...)
        // ============================================================

        private MateriaExtra? CargarMateriaPre(
            string rutaCap)
        {
            try
            {
                List<Alumno> alumnos =
                    _parserService
                        .ProcesarArchivo(rutaCap);

                string asignatura =
                    ObtenerDatoCap(
                        rutaCap,
                        "ASIGNATURA_STR");

                string codigoGrupo =
                    ObtenerDatoCap(
                        rutaCap,
                        "CodigoGrupo");

                string grupoCap =
                    ObtenerDatoCap(
                        rutaCap,
                        "Grupo");

                string nombreProfesor =
                    ObtenerDatoCap(
                        rutaCap,
                        "NombreProfesor");

                var materia =
                    new MateriaExtra
                    {
                        RutaCap =
                            rutaCap,

                        Asignatura =
                            asignatura.Trim(),

                        CodigoGrupo =
                            codigoGrupo.Trim(),

                        GrupoCap =
                            grupoCap.Trim(),

                        NombreProfesor =
                            nombreProfesor.Trim(),

                        Alumnos =
                            new List<AlumnoExtra>()
                    };

                string capBaseName =
                    Path.GetFileNameWithoutExtension(
                        rutaCap);

                // ====================================================
                // ALUMNOS CON PRE
                // ====================================================

                if (alumnos != null)
                {
                    foreach (Alumno alumno
                             in alumnos)
                    {
                        if (alumno == null)
                            continue;

                        string matricula =
                            alumno.Matricula?
                                .Trim()
                            ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(
                                matricula))
                        {
                            continue;
                        }

                        PreEstado estadoPre =
                            _preService
                                .ObtenerEstadoPre(
                                    capBaseName,
                                    matricula);

                        // ============================================
                        // SOLO ALUMNOS QUE REALMENTE TIENEN PRE
                        // ============================================

                        if (!estadoPre.TienePRE)
                            continue;

                        string nombre =
                            alumno.Nombre?
                                .Trim()
                            ?? string.Empty;

                        string grupoTradicional =
                            ObtenerGrupoAlumno(
                                alumno,
                                matricula,
                                materia.GrupoCap);

                        string calificacionNumero =
                            ObtenerCalificacionPre(
                                estadoPre);

                        string calificacionLetra =
                            ConvertirNumeroALetra(
                                calificacionNumero);

                        materia.Alumnos.Add(
                            new AlumnoExtra
                            {
                                Matricula =
                                    matricula,

                                Nombre =
                                    nombre,

                                Grupo =
                                    grupoTradicional,

                                CalificacionNumero =
                                    calificacionNumero,

                                CalificacionLetra =
                                    calificacionLetra
                            });
                    }
                }

                return materia;
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // GRUPO DEL ALUMNO
        //
        // SE CONSERVA LA PRIORIDAD EXISTENTE:
        //
        // 1. configuracion.bin
        // 2. grupo del alumno
        // 3. grupo del CAP
        // ============================================================

        private string ObtenerGrupoAlumno(
            Alumno alumno,
            string matricula,
            string grupoCap)
        {
            string grupoTradicional =
                string.Empty;

            if (!string.IsNullOrWhiteSpace(
                    matricula))
            {
                try
                {
                    grupoTradicional =
                        _sqliteService
                            .ObtenerGrupoPorMatricula(
                                matricula)?
                            .Trim()
                        ?? string.Empty;
                }
                catch
                {
                    grupoTradicional =
                        string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(
                    grupoTradicional))
            {
                grupoTradicional =
                    alumno.Grupo?
                        .Trim()
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(
                    grupoTradicional))
            {
                grupoTradicional =
                    grupoCap;
            }

            return grupoTradicional;
        }

        // ============================================================
        // PÁGINA DE MATERIA EXTRA
        //
        // DISEÑO EXISTENTE.
        // NO SE MODIFICA.
        // ============================================================

        private static void AgregarPaginaMateria(
            IDocumentContainer document,
            MateriaExtra materia)
        {
            document.Page(page =>
            {
                page.Size(
                    PageSizes.Letter);

                page.MarginTop(28);
                page.MarginBottom(28);
                page.MarginLeft(28);
                page.MarginRight(28);

                page.DefaultTextStyle(
                    style =>
                        style
                            .FontFamily("Tahoma")
                            .FontSize(8));

                // ====================================================
                // ENCABEZADO
                // ====================================================

                page.Header()
                    .Column(header =>
                    {
                        header.Spacing(3);

                        if (!string.IsNullOrWhiteSpace(
                                materia.NombreProfesor))
                        {
                            header.Item()
                                .AlignCenter()
                                .Text(
                                    materia.NombreProfesor)
                                .Bold()
                                .FontSize(11);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                materia.Asignatura))
                        {
                            header.Item()
                                .AlignCenter()
                                .Text(
                                    materia.Asignatura)
                                .Bold()
                                .FontSize(9);
                        }
                    });

                // ====================================================
                // TABLA
                // ====================================================

                page.Content()
                    .PaddingTop(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(
                            columns =>
                            {
                                columns.RelativeColumn(
                                    3.2f);

                                columns.RelativeColumn(
                                    1.4f);

                                columns.RelativeColumn(
                                    1.6f);

                                columns.RelativeColumn(
                                    4.8f);

                                columns.RelativeColumn(
                                    1.8f);

                                columns.RelativeColumn(
                                    3.0f);
                            });

                        CrearEncabezadoTabla(
                            table,
                            "ASIGNATURA");

                        CrearEncabezadoTabla(
                            table,
                            "GPO");

                        CrearEncabezadoTabla(
                            table,
                            "MATR");

                        CrearEncabezadoTabla(
                            table,
                            "NOMBRE");

                        CrearEncabezadoTabla(
                            table,
                            "C. NÚMERO");

                        CrearEncabezadoTabla(
                            table,
                            "C. LETRA");

                        foreach (
                            AlumnoExtra alumno
                            in materia.Alumnos)
                        {
                            CrearCelda(
                                table,
                                materia.Asignatura);

                            CrearCelda(
                                table,
                                alumno.Grupo,
                                true);

                            CrearCelda(
                                table,
                                alumno.Matricula,
                                true);

                            CrearCelda(
                                table,
                                alumno.Nombre);

                            CrearCelda(
                                table,
                                alumno.CalificacionNumero,
                                true);

                            CrearCelda(
                                table,
                                alumno.CalificacionLetra,
                                true);
                        }
                    });

                // ====================================================
                // PIE
                // ====================================================

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(
                            "Página ");

                        text.CurrentPageNumber();

                        text.Span(
                            " de ");

                        text.TotalPages();
                    });
            });
        }

        // ============================================================
        // PÁGINA DE MATERIA PRE
        //
        // MISMA MAQUETA DEL ACTA ADICIONAL.
        //
        // LA DIFERENCIA ES EL TÍTULO Y QUE LOS DATOS YA VIENEN
        // FILTRADOS POR PRE.
        // ============================================================

        private static void AgregarPaginaMateriaPre(
            IDocumentContainer document,
            MateriaExtra materia)
        {
            document.Page(page =>
            {
                page.Size(
                    PageSizes.Letter);

                page.MarginTop(28);
                page.MarginBottom(28);
                page.MarginLeft(28);
                page.MarginRight(28);

                page.DefaultTextStyle(
                    style =>
                        style
                            .FontFamily("Tahoma")
                            .FontSize(8));

                // ====================================================
                // ENCABEZADO
                // ====================================================

                page.Header()
                    .Column(header =>
                    {
                        header.Spacing(3);

                        if (!string.IsNullOrWhiteSpace(
                                materia.NombreProfesor))
                        {
                            header.Item()
                                .AlignCenter()
                                .Text(
                                    materia.NombreProfesor)
                                .Bold()
                                .FontSize(11);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                materia.Asignatura))
                        {
                            header.Item()
                                .AlignCenter()
                                .Text(
                                    materia.Asignatura)
                                .Bold()
                                .FontSize(9);
                        }

                        header.Item()
                            .PaddingTop(3)
                            .AlignCenter()
                            .Text(
                                "ACTA DE EVALUACIÓN PREEXTRAORDINARIA")
                            .Bold()
                            .FontSize(9);
                    });

                // ====================================================
                // TABLA
                // ====================================================

                page.Content()
                    .PaddingTop(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(
                            columns =>
                            {
                                columns.RelativeColumn(
                                    3.2f);

                                columns.RelativeColumn(
                                    1.4f);

                                columns.RelativeColumn(
                                    1.6f);

                                columns.RelativeColumn(
                                    4.8f);

                                columns.RelativeColumn(
                                    1.8f);

                                columns.RelativeColumn(
                                    3.0f);
                            });

                        CrearEncabezadoTabla(
                            table,
                            "ASIGNATURA");

                        CrearEncabezadoTabla(
                            table,
                            "GPO");

                        CrearEncabezadoTabla(
                            table,
                            "MATR");

                        CrearEncabezadoTabla(
                            table,
                            "NOMBRE");

                        CrearEncabezadoTabla(
                            table,
                            "C. NÚMERO");

                        CrearEncabezadoTabla(
                            table,
                            "C. LETRA");

                        foreach (
                            AlumnoExtra alumno
                            in materia.Alumnos)
                        {
                            CrearCelda(
                                table,
                                materia.Asignatura);

                            CrearCelda(
                                table,
                                alumno.Grupo,
                                true);

                            CrearCelda(
                                table,
                                alumno.Matricula,
                                true);

                            CrearCelda(
                                table,
                                alumno.Nombre);

                            CrearCelda(
                                table,
                                alumno.CalificacionNumero,
                                true);

                            CrearCelda(
                                table,
                                alumno.CalificacionLetra,
                                true);
                        }
                    });

                // ====================================================
                // PIE
                // ====================================================

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(
                            "Página ");

                        text.CurrentPageNumber();

                        text.Span(
                            " de ");

                        text.TotalPages();
                    });
            });
        }

        // ============================================================
        // ENCABEZADO TABLA
        // ============================================================

        private static void CrearEncabezadoTabla(
            TableDescriptor table,
            string texto)
        {
            table.Cell()
                .Border(0.5f)
                .Background("#1B365D")
                .PaddingVertical(5)
                .PaddingHorizontal(3)
                .AlignMiddle()
                .AlignCenter()
                .Text(texto)
                .FontColor("#FFFFFF")
                .Bold()
                .FontSize(7.5f);
        }

        // ============================================================
        // CELDA
        // ============================================================

        private static void CrearCelda(
            TableDescriptor table,
            string? texto,
            bool centrar = false)
        {
            IContainer celda =
                table.Cell()
                    .Border(0.35f)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3)
                    .AlignMiddle();

            if (centrar)
            {
                celda =
                    celda.AlignCenter();
            }

            celda
                .Text(
                    texto ?? string.Empty)
                .FontSize(7.5f);
        }

        // ============================================================
        // OBTENER EXTRA
        // ============================================================

        private static string ObtenerCalificacionNumero(
            Alumno alumno)
        {
            if (alumno?.Calificación == null)
                return string.Empty;

            string valor =
                alumno.Calificación["EXTRA"]?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto =
                valor.ToUpperInvariant();

            if (texto == "NP" || texto == "-555")
                return "NP";

            if (texto == "SD" || texto == "-999")
                return "SD";

            texto =
                texto
                    .TrimEnd('S')
                    .Replace(',', '.');

            if (double.TryParse(
                texto,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double numero))
            {
                if (numero < 0)
                    numero = 0;

                if (numero > 10)
                    numero = 10;

                if (numero % 1 == 0)
                {
                    return ((int)numero)
                        .ToString(
                            CultureInfo.InvariantCulture);
                }

                return numero.ToString(
                    "0.#",
                    CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        // ============================================================
        // OBTENER PRE
        //
        // PRE MANEJA VALORES NUMÉRICOS.
        //
        // SI TODAVÍA NO EXISTE CALIFICACIÓN, SE DEJA VACÍO.
        // ============================================================

        private static string ObtenerCalificacionPre(
            PreEstado estadoPre)
        {
            if (estadoPre == null ||
                !estadoPre.TienePRE ||
                !estadoPre.Calificacion.HasValue)
            {
                return string.Empty;
            }

            double numero =
                estadoPre.Calificacion.Value;

            if (numero < 0)
                numero = 0;

            if (numero > 10)
                numero = 10;

            if (numero % 1 == 0)
            {
                return ((int)numero)
                    .ToString(
                        CultureInfo.InvariantCulture);
            }

            return numero.ToString(
                "0.#",
                CultureInfo.InvariantCulture);
        }

        // ============================================================
        // CALIFICACIÓN A LETRA
        // ============================================================

        private static string ConvertirNumeroALetra(
            string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            if (valor.Equals(
                    "NP",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "NO PRESENTÓ";
            }

            if (valor.Equals(
                    "SD",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "SIN DERECHO";
            }

            if (!double.TryParse(
                valor.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double numero))
            {
                return string.Empty;
            }

            int entero =
                (int)Math.Truncate(numero);

            return entero switch
            {
                0 => "CERO",
                1 => "UNO",
                2 => "DOS",
                3 => "TRES",
                4 => "CUATRO",
                5 => "CINCO",
                6 => "SEIS",
                7 => "SIETE",
                8 => "OCHO",
                9 => "NUEVE",
                10 => "DIEZ",
                _ => string.Empty
            };
        }

        // ============================================================
        // LEER DATO DIRECTAMENTE DEL CAP
        // ============================================================

        private static string ObtenerDatoCap(
            string rutaCap,
            string claveBuscada)
        {
            if (!File.Exists(
                    rutaCap))
            {
                return string.Empty;
            }

            try
            {
                Encoding encoding =
                    Encoding.GetEncoding(
                        "iso-8859-1");

                foreach (
                    string lineaOriginal
                    in File.ReadLines(
                        rutaCap,
                        encoding))
                {
                    string linea =
                        lineaOriginal?
                            .Trim()
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(
                            linea))
                    {
                        continue;
                    }

                    if (linea.StartsWith(
                            ";"))
                    {
                        continue;
                    }

                    int posicion =
                        linea.IndexOf('=');

                    if (posicion <= 0)
                    {
                        continue;
                    }

                    string clave =
                        linea
                            .Substring(
                                0,
                                posicion)
                            .Trim();

                    if (!string.Equals(
                            clave,
                            claveBuscada,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return
                        linea
                            .Substring(
                                posicion + 1)
                            .Trim()
                            .Trim('"');
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        // ============================================================
        // MODELOS INTERNOS
        // ============================================================

        private sealed class MateriaExtra
        {
            public string RutaCap { get; set; } =
                string.Empty;

            public string Asignatura { get; set; } =
                string.Empty;

            public string CodigoGrupo { get; set; } =
                string.Empty;

            public string GrupoCap { get; set; } =
                string.Empty;

            public string NombreProfesor { get; set; } =
                string.Empty;

            public List<AlumnoExtra> Alumnos { get; set; } =
                new();
        }

        private sealed class AlumnoExtra
        {
            public string Matricula { get; set; } =
                string.Empty;

            public string Nombre { get; set; } =
                string.Empty;

            public string Grupo { get; set; } =
                string.Empty;

            public string CalificacionNumero { get; set; } =
                string.Empty;

            public string CalificacionLetra { get; set; } =
                string.Empty;
        }
    }
}