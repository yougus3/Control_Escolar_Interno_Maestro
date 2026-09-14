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

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ReporteCalificacionesPdfService
{
    // ============================================================
    // PARÁMETROS DE DISEÑO COMPACTOS
    // ============================================================
    private readonly float _fontSize = 8f;
    private readonly float _borderWidth = 0.2f;

    private const string ColorGray = "#ADADAD";
    private const string ColorWhite = "#FFFFFF";
    private const string ColorRed = "#FF0000";
    private const string FontFamily = "Tahoma";

    private const float StudentCellHeight = 10f;

    private const float NumberColumnWidth = 14f;
    private const float NameColumnWidth = 220f;
    private const float NumericColumnWidth = 16f;

    private const float FinalColumnWidth = 14f;

    // ============================================================
    // CAP
    // ============================================================
    private readonly string _capFilePath;

    private string _colegio = "";
    private string _cicloCodigoCorto = "";
    private string _cicloDescripcion = "";
    private string _nivel = "";
    private string _nivelStr = "";
    private string _codigoGrupo = "";
    private string _grado = "";
    private string _grupo = "";

    private string _claveAsignatura = "";
    private string _asignatura = "";

    private string _claveProfesor = "";
    private string _nombreProfesor = "";

    private string _capBaseName = "";

    // ============================================================
    // ALUMNOS
    // ============================================================
    private List<Alumno> _alumnos = new();

    // ============================================================
    // PARCIALES
    // ============================================================
    private MateriaParcial? _parcialP1;
    private MateriaParcial? _parcialP2;
    private MateriaParcial? _parcialP3;

    // ============================================================
    // PRE
    // ============================================================
    private PreExtraordinarioService? _preService;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================
    public ReporteCalificacionesPdfService(string capFilePath)
    {
        QuestPDF.Settings.License =
            LicenseType.Community;

        QuestPDF.Settings.EnableDebugging =
            true;

        if (string.IsNullOrWhiteSpace(
                capFilePath))
        {
            throw new ArgumentException(
                "La ruta del archivo CAP no puede estar vacía.",
                nameof(capFilePath));
        }

        if (!File.Exists(
                capFilePath))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo CAP.",
                capFilePath);
        }

        _capFilePath =
            capFilePath;
    }

    // ============================================================
    // GENERAR
    // ============================================================
    public byte[] GenerarDiseno()
    {
        CargarDatos();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(
                    PageSizes.Letter);

                page.MarginLeft(5);
                page.MarginRight(5);
                page.MarginTop(20);
                page.MarginBottom(20);

                page.PageColor(
                    Colors.White);

                page.Header()
                    .PaddingLeft(20)
                    .PaddingRight(20)
                    .Element(
                        ComposeHeader);

                page.Content()
                    .PaddingLeft(20)
                    .PaddingRight(20)
                    .Element(
                        ComposeContent);

                page.Footer()
                    .Element(
                        ComposeFooter);
            });
        }).GeneratePdf();
    }
    
    // ============================================================
// GENERAR DATOS PARA EXPORTACIÓN JSON
//
// Utiliza exactamente la misma lógica del PDF para:
//
// P1
// P2
// P3
// SEM
// PREEXTRAORDINARIO
//
// NO modifica la lógica existente del PDF.
// ============================================================
public ExportacionMateriaJson GenerarDatosExportacionJson()
{
    CargarDatos();

    var materia =
        new ExportacionMateriaJson
        {
            ArchivoCap =
                Path.GetFileName(
                    _capFilePath),

            ClaveMateria =
                !string.IsNullOrWhiteSpace(
                    _claveAsignatura)
                    ? _claveAsignatura
                    : _capBaseName
        };

    foreach (Alumno alumno in _alumnos)
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

        string nombreAlumno =
            alumno.Nombre?
                .Trim()
            ?? string.Empty;

        // ========================================================
        // P1
        // ========================================================

        string p1 =
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP1,
                "P1");

        AgregarAlumnoSiTieneCalificacion(
            materia.Evaluaciones.P1,
            matricula,
            nombreAlumno,
            p1);

        // ========================================================
        // P2
        // ========================================================

        string p2 =
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP2,
                "P2");

        AgregarAlumnoSiTieneCalificacion(
            materia.Evaluaciones.P2,
            matricula,
            nombreAlumno,
            p2);

        // ========================================================
        // P3
        // ========================================================

        string p3 =
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP3,
                "P3");

        AgregarAlumnoSiTieneCalificacion(
            materia.Evaluaciones.P3,
            matricula,
            nombreAlumno,
            p3);

        // ========================================================
        // SEM
        //
        // Usa exactamente ObtenerSemReporte().
        // ========================================================

        string sem =
            ObtenerSemReporte(
                alumno);

        AgregarAlumnoSiTieneCalificacion(
            materia.Evaluaciones.SEM,
            matricula,
            nombreAlumno,
            sem);

        // ========================================================
        // PREEXTRAORDINARIO
        //
        // PRE solamente maneja valores numéricos.
        // No se exporta NP.
        // ========================================================

        PreEstado estadoPre =
            ObtenerEstadoPre(
                matricula);

        if (estadoPre.TienePRE &&
            estadoPre.Calificacion.HasValue)
        {
            materia
                .Evaluaciones
                .PREEXTRAORDINARIO
                .Add(
                    new ExportacionAlumnoJson
                    {
                        Matricula =
                            matricula,

                        NombreAlumno =
                            nombreAlumno,

                        Calificacion =
                            estadoPre.Calificacion.Value
                    });
        }
    }

    return materia;
}

// ============================================================
// AGREGAR ALUMNO CON CALIFICACIÓN
// ============================================================
private void AgregarAlumnoSiTieneCalificacion(
    List<ExportacionAlumnoJson> destino,
    string matricula,
    string nombreAlumno,
    string valor)
{
    if (string.IsNullOrWhiteSpace(
            valor))
    {
        return;
    }

    if (!TryObtenerDouble(
            valor,
            out double calificacion))
    {
        return;
    }

    destino.Add(
        new ExportacionAlumnoJson
        {
            Matricula =
                matricula,

            NombreAlumno =
                nombreAlumno,

            Calificacion =
                calificacion
        });
}

    // ============================================================
    // CARGAR DATOS
    // ============================================================
    private void CargarDatos()
    {
        _capBaseName =
            Path.GetFileNameWithoutExtension(
                _capFilePath);

        CargarDatosDesdeCap();

        CargarParcialesDesdeLiteDb();

        _preService =
            new PreExtraordinarioService();
    }

    // ============================================================
    // CARGAR CAP
    // ============================================================
    private void CargarDatosDesdeCap()
    {
        var parser =
            new CapParserService();

        CapParseResult resultado =
            parser.ProcesarArchivoCompleto(
                _capFilePath);

        _alumnos =
            resultado.Alumnos ??
            new List<Alumno>();

        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        Encoding encodingCap =
            Encoding.GetEncoding(
                "iso-8859-1");

        string[] lineas =
            File.ReadAllLines(
                _capFilePath,
                encodingCap);

        foreach (string lineaOriginal
                 in lineas)
        {
            string linea =
                lineaOriginal.Trim();

            if (string.IsNullOrWhiteSpace(
                    linea) ||
                !linea.Contains('='))
            {
                continue;
            }

            string[] partes =
                linea.Split(
                    '=',
                    2);

            if (partes.Length != 2)
                continue;

            string clave =
                partes[0].Trim();

            string valor =
                partes[1].Trim();

            switch (clave.ToUpperInvariant())
            {
                case "COLEGIO":
                    _colegio = valor;
                    break;

                case "CICLO_CODIGOCORTO":
                    _cicloCodigoCorto = valor;
                    break;

                case "CICLO_DESCRIPCION":
                    _cicloDescripcion = valor;
                    break;

                case "NIVEL":
                    _nivel = valor;
                    break;

                case "NIVEL_STR":
                    _nivelStr = valor;
                    break;

                case "CODIGOGRUPO":
                    _codigoGrupo = valor;
                    break;

                case "GRADO":
                    _grado = valor;
                    break;

                case "GRUPO":
                    _grupo = valor;
                    break;

                case "CLAVEASIGNATURA":
                    _claveAsignatura = valor;
                    break;

                case "ASIGNATURA_STR":
                    _asignatura = valor;
                    break;

                case "CLAVEPROFESOR":
                    _claveProfesor = valor;
                    break;

                case "NOMBREPROFESOR":
                    _nombreProfesor = valor;
                    break;
            }
        }

        foreach (Alumno alumno in _alumnos)
        {
            if (string.IsNullOrWhiteSpace(
                    alumno.Grupo))
            {
                alumno.Grupo =
                    string.IsNullOrWhiteSpace(
                        _grupo)
                        ? "S/G"
                        : _grupo;
            }
        }
    }

    // ============================================================
    // CARGAR PARCIALES
    // ============================================================
    private void CargarParcialesDesdeLiteDb()
    {
        var servicio =
            new ParcialJsonService();

        _parcialP1 =
            servicio.ObtenerMateria(
                $"{_capBaseName}_P1");

        _parcialP2 =
            servicio.ObtenerMateria(
                $"{_capBaseName}_P2");

        _parcialP3 =
            servicio.ObtenerMateria(
                $"{_capBaseName}_P3");
    }

    // ============================================================
    // OBTENER ESTADO PRE
    // ============================================================
    private PreEstado ObtenerEstadoPre(
        string matricula)
    {
        if (_preService == null ||
            string.IsNullOrWhiteSpace(
                _capBaseName) ||
            string.IsNullOrWhiteSpace(
                matricula))
        {
            return new PreEstado();
        }

        return _preService.ObtenerEstadoPre(
            _capBaseName,
            matricula);
    }

    // ============================================================
    // OBTENER PRE
    //
    // Se conserva el asterisco como indicador visual.
    // ============================================================
    private string ObtenerPreCalificacion(
        Alumno alumno)
    {
        var estado =
            ObtenerEstadoPre(
                alumno.Matricula);

        return estado.TienePRE
            ? "•"
            : "";
    }

    // ============================================================
    // ENCABEZADO
    // ============================================================
    private void ComposeHeader(
        IContainer container)
    {
        container
            .Padding(2)
            .Column(column =>
            {
                column.Item()
                    .Border(_borderWidth)
                    .Padding(5)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .PaddingRight(4)
                            .Column(left =>
                            {
                                left.Item()
                                    .Text(
                                        "SECRETARÍA DE EDUCACIÓN PÚBLICA")
                                    .FontSize(_fontSize)
                                    .FontFamily(FontFamily)
                                    .SemiBold();

                                left.Item()
                                    .Text(
                                        "DIRECCIÓN GENERAL DEL BACHILLERATO")
                                    .FontSize(_fontSize)
                                    .FontFamily(FontFamily);

                                string escuelaLinea =
                                    !string.IsNullOrWhiteSpace(
                                        _nivelStr)
                                        ? _nivelStr
                                        : _nivel;

                                left.Item()
                                    .Text(
                                        string.Equals(
                                            escuelaLinea?.Trim(),
                                            "BACHILLERATO GENERAL",
                                            StringComparison.OrdinalIgnoreCase)
                                            ? "ESCUELA PREPARATORIA FEDERAL POR COOPERACIÓN"
                                            : escuelaLinea)
                                    .FontSize(_fontSize)
                                    .FontFamily(FontFamily);

                                left.Item()
                                    .Text(
                                        "JOSE MA. MORELOS Y PAVON")
                                    .FontSize(_fontSize)
                                    .FontFamily(FontFamily)
                                    .SemiBold();
                            });

                        row.ConstantItem(6)
                            .LineVertical(1)
                            .LineColor(
                                Colors.Black);

                        row.RelativeItem()
                            .PaddingLeft(3)
                            .Column(right =>
                            {
                                right.Item()
                                    .AlignMiddle()
                                    .Column(groupData =>
                                    {
                                        groupData.Item()
                                            .PaddingBottom(1)
                                            .PaddingTop(4)
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        "GRUPO: ")
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily);

                                                text.Span(
                                                        string.IsNullOrWhiteSpace(
                                                            _grupo)
                                                            ? "S/G"
                                                            : _grupo)
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily)
                                                    .SemiBold();
                                            });

                                        groupData.Item()
                                            .PaddingBottom(1)
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        "ASIGNATURA: ")
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily);

                                                text.Span(
                                                        ConstruirTextoAsignatura())
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily)
                                                    .SemiBold();
                                            });

                                        groupData.Item()
                                            .PaddingBottom(1)
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        "PROFR(A).: ")
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily);

                                                text.Span(
                                                        _nombreProfesor)
                                                    .FontSize(
                                                        _fontSize)
                                                    .FontFamily(
                                                        FontFamily)
                                                    .SemiBold();
                                            });
                                    });
                            });
                    });

                column.Item()
                    .PaddingVertical(5)
                    .AlignCenter()
                    .Text(
                        "FORMAS PARA EVIDENCIAR EL APRENDIZAJE")
                    .FontSize(10)
                    .FontFamily(FontFamily)
                    .Bold();
            });
    }

    private string ConstruirTextoAsignatura()
    {
        if (!string.IsNullOrWhiteSpace(
                _claveAsignatura) &&
            !string.IsNullOrWhiteSpace(
                _asignatura))
        {
            return
                $"{_claveAsignatura} {_asignatura}";
        }

        return
            !string.IsNullOrWhiteSpace(
                _claveAsignatura)
                ? _claveAsignatura
                : _asignatura;
    }

    // ============================================================
    // CONTENIDO
    // ============================================================
    private void ComposeContent(
        IContainer container)
    {
        container
            .AlignCenter()
            .Table(table =>
            {
                table.ColumnsDefinition(
                    columns =>
                    {
                        columns.ConstantColumn(
                            NumberColumnWidth);

                        columns.ConstantColumn(
                            NameColumnWidth);

                        for (int i = 0; i < 5; i++)
                        {
                            columns.ConstantColumn(
                                NumericColumnWidth);
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            columns.ConstantColumn(
                                NumericColumnWidth);
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            columns.ConstantColumn(
                                NumericColumnWidth);
                        }

                        columns.ConstantColumn(
                            NumericColumnWidth);

                        columns.ConstantColumn(
                            FinalColumnWidth);

                        columns.ConstantColumn(
                            FinalColumnWidth);

                        columns.ConstantColumn(
                            FinalColumnWidth);
                    });

                table.Header(header =>
                {
                    ComposeHeaderRow1(header);
                    ComposeHeaderRow2(header);
                    ComposeHeaderRow3(header);
                    ComposeHeaderRow4(header);
                });

                for (int i = 0;
                     i < _alumnos.Count;
                     i++)
                {
                    ComposeStudentRow(
                        table,
                        _alumnos[i],
                        i);
                }
            });
    }

    // ============================================================
    // FILA 1
    // ============================================================
    private void ComposeHeaderRow1(
        TableCellDescriptor header)
    {
        header.Cell()
            .RowSpan(4)
            .Background(ColorGray)
            .Border(_borderWidth)
            .Padding(0.2f)
            .AlignCenter()
            .AlignMiddle()
            .Text("N")
            .FontSize(_fontSize)
            .FontFamily(FontFamily)
            .Bold();

        header.Cell()
            .RowSpan(4)
            .Background(ColorWhite)
            .Border(_borderWidth)
            .Padding(0)
            .Table(nestedTable =>
            {
                nestedTable.ColumnsDefinition(
                    columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                nestedTable.Cell()
                    .ColumnSpan(2)
                    .BorderBottom(_borderWidth)
                    .BorderColor(
                        Colors.Black)
                    .Padding(0.2f)
                    .MinHeight(10)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(
                        "Porcentajes por cada parcial:")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();

                nestedTable.Cell()
                    .ColumnSpan(2)
                    .BorderBottom(_borderWidth)
                    .BorderColor(
                        Colors.Black)
                    .Padding(0.2f)
                    .MinHeight(10)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(
                        "Porcentajes por cada actividad:")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();

                nestedTable.Cell()
                    .ColumnSpan(2)
                    .BorderBottom(_borderWidth)
                    .BorderColor(
                        Colors.Black)
                    .Padding(0.2f)
                    .MinHeight(10)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(
                        "Puntaje máximo esperado:")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();

                nestedTable.Cell()
                    .BorderRight(_borderWidth)
                    .BorderColor(
                        Colors.Black)
                    .Padding(0.2f)
                    .MinHeight(20)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(
                        "N O M B R E")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();

                nestedTable.Cell()
                    .Padding(0.2f)
                    .MinHeight(20)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(
                        "Formas para evidenciar el aprendizaje:")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();
            });

        ComposePeriodoHeader(
            header,
            _parcialP1,
            "P\nA\nR\n1");

        ComposePeriodoHeader(
            header,
            _parcialP2,
            "P\nA\nR\n2");

        ComposePeriodoHeader(
            header,
            _parcialP3,
            "P\nA\nR\n3");

        ComposeVerticalHeader(
            header,
            "P\nR\nO\nM");

        ComposeVerticalHeader(
            header,
            "E\nX\nA\nM");

        ComposeVerticalHeader(
            header,
            "P\nR\nE");

        ComposeVerticalHeader(
            header,
            "S\nE\nM");
    }

    // ============================================================
    // ENCABEZADO PARCIAL
    // ============================================================
    private void ComposePeriodoHeader(
        TableCellDescriptor header,
        MateriaParcial? materia,
        string textoPeriodo)
    {
        double suma =
            ObtenerSumaPorcentajes(
                materia);

        header.Cell()
            .ColumnSpan(4)
            .Background(ColorGray)
            .Border(_borderWidth)
            .Padding(0.2f)
            .MinHeight(10)
            .AlignCenter()
            .AlignMiddle()
            .Text(
                suma > 0
                    ? FormatearNumero(suma)
                    : "")
            .FontSize(_fontSize)
            .FontFamily(FontFamily)
            .Bold();

        header.Cell()
            .RowSpan(4)
            .Background(ColorWhite)
            .Border(_borderWidth)
            .Padding(0.2f)
            .AlignCenter()
            .AlignMiddle()
            .Text(textoPeriodo)
            .FontSize(_fontSize)
            .FontFamily(FontFamily)
            .Bold();
    }

    // ============================================================
    // ENCABEZADO VERTICAL
    // ============================================================
    private void ComposeVerticalHeader(
        TableCellDescriptor header,
        string text)
    {
        header.Cell()
            .RowSpan(4)
            .Background(ColorGray)
            .Border(_borderWidth)
            .Padding(0.2f)
            .AlignCenter()
            .AlignMiddle()
            .Text(text)
            .FontSize(_fontSize)
            .FontFamily(FontFamily)
            .Bold();
    }

    // ============================================================
    // FILA 2
    // ============================================================
    private void ComposeHeaderRow2(
        TableCellDescriptor header)
    {
        ComposePorcentajesActividades(
            header,
            _parcialP1);

        ComposePorcentajesActividades(
            header,
            _parcialP2);

        ComposePorcentajesActividades(
            header,
            _parcialP3);
    }

    private void ComposePorcentajesActividades(
        TableCellDescriptor header,
        MateriaParcial? materia)
    {
        for (int i = 0; i < 4; i++)
        {
            ActividadParcial? actividad =
                ObtenerActividad(
                    materia,
                    i);

            string porcentaje =
                actividad != null &&
                actividad.Activa
                    ? FormatearNumero(
                        actividad.Porcentaje)
                    : "";

            header.Cell()
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.2f)
                .MinHeight(10)
                .AlignCenter()
                .AlignMiddle()
                .Text(porcentaje)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }
    }

    // ============================================================
    // FILA 3
    // ============================================================
    private void ComposeHeaderRow3(
        TableCellDescriptor header)
    {
        ComposeMaximos(
            header,
            _parcialP1);

        ComposeMaximos(
            header,
            _parcialP2);

        ComposeMaximos(
            header,
            _parcialP3);
    }

    private void ComposeMaximos(
        TableCellDescriptor header,
        MateriaParcial? materia)
    {
        for (int i = 0; i < 4; i++)
        {
            ActividadParcial? actividad =
                ObtenerActividad(
                    materia,
                    i);

            string puntajeMaximo =
                actividad != null &&
                actividad.Activa &&
                actividad.PuntajeMaximo > 0
                    ? FormatearNumero(
                        actividad.PuntajeMaximo)
                    : "";

            header.Cell()
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.2f)
                .MinHeight(10)
                .AlignCenter()
                .AlignMiddle()
                .Text(puntajeMaximo)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }
    }

    // ============================================================
    // FILA 4
    // ============================================================
    private void ComposeHeaderRow4(
        TableCellDescriptor header)
    {
        ComposeActividadesHeader(
            header,
            _parcialP1);

        ComposeActividadesHeader(
            header,
            _parcialP2);

        ComposeActividadesHeader(
            header,
            _parcialP3);
    }

    private void ComposeActividadesHeader(
        TableCellDescriptor header,
        MateriaParcial? materia)
    {
        for (int i = 0; i < 4; i++)
        {
            ActividadParcial? actividad =
                ObtenerActividad(
                    materia,
                    i);

            if (actividad == null ||
                !actividad.Activa)
            {
                header.Cell()
                    .Background(ColorWhite)
                    .Border(_borderWidth)
                    .Padding(0.2f)
                    .MinHeight(20)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("")
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily);

                continue;
            }

            string nombreVertical =
                ConvertirAVertical(
                    AbreviarActividad(
                        actividad.Nombre));

            header.Cell()
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.2f)
                .MinHeight(20)
                .AlignCenter()
                .AlignMiddle()
                .Text(nombreVertical)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }
    }

    // ============================================================
    // FILA ALUMNO
    // ============================================================
    private void ComposeStudentRow(
        TableDescriptor table,
        Alumno alumno,
        int index)
    {
        string background =
            index % 2 == 0
                ? ColorGray
                : ColorWhite;

        table.Cell()
            .Background(background)
            .Border(_borderWidth)
            .MinHeight(StudentCellHeight)
            .PaddingHorizontal(0.2f)
            .PaddingVertical(0)
            .AlignCenter()
            .AlignMiddle()
            .Text(
                (index + 1).ToString(
                    CultureInfo.InvariantCulture))
            .FontSize(_fontSize)
            .FontFamily(FontFamily);

        table.Cell()
            .Background(background)
            .Border(_borderWidth)
            .MinHeight(StudentCellHeight)
            .PaddingHorizontal(0.5f)
            .PaddingVertical(0)
            .AlignLeft()
            .AlignMiddle()
            .Text(
                alumno.Nombre ?? "")
            .FontSize(_fontSize)
            .FontFamily(FontFamily);

        ComposeActividadesAlumno(
            table,
            background,
            _parcialP1,
            alumno);

        ComposeResultCell(
            table,
            background,
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP1,
                "P1"));

        ComposeActividadesAlumno(
            table,
            background,
            _parcialP2,
            alumno);

        ComposeResultCell(
            table,
            background,
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP2,
                "P2"));

        ComposeActividadesAlumno(
            table,
            background,
            _parcialP3,
            alumno);

        ComposeResultCell(
            table,
            background,
            ObtenerCalificacionParcialReporte(
                alumno,
                _parcialP3,
                "P3"));

        string promedio =
            CalcularPromedioTruncado(
                alumno);

        ComposeResultCell(
            table,
            background,
            promedio,
            esProm: true);

        ComposeResultCell(
            table,
            background,
            ObtenerExamenReporte(
                alumno));

        ComposePreCell(
            table,
            background,
            ObtenerPreCalificacion(
                alumno));

        string sem =
            ObtenerSemReporte(
                alumno);

        ComposeResultCell(
            table,
            background,
            sem,
            esSem: true);
    }

    // ============================================================
    // ACTIVIDADES ALUMNO
    // ============================================================
    private void ComposeActividadesAlumno(
        TableDescriptor table,
        string background,
        MateriaParcial? materia,
        Alumno alumno)
    {
        for (int i = 0; i < 4; i++)
        {
            ActividadParcial? actividad =
                ObtenerActividad(
                    materia,
                    i);

            string valor =
                actividad != null &&
                actividad.Activa
                    ? ObtenerCalificacionActividad(
                        materia,
                        alumno.Matricula,
                        actividad.Nombre)
                    : "";

            ComposeGradeCell(
                table,
                background,
                valor);
        }
    }

    // ============================================================
    // OBTENER CALIFICACIÓN DE ACTIVIDAD
    //
    // PA:
    //   usa captura actual, es decir, la modificada por PRE.
    //
    // PR:
    //   usa PreOriginals.
    //
    // SIN PRE:
    //   usa captura normal.
    // ============================================================
    private string ObtenerCalificacionActividad(
        MateriaParcial? materia,
        string? matricula,
        string? nombreActividad)
    {
        if (materia == null ||
            string.IsNullOrWhiteSpace(
                matricula) ||
            string.IsNullOrWhiteSpace(
                nombreActividad))
        {
            return "";
        }

        string matriculaLimpia =
            matricula.Trim();

        string actividadLimpia =
            nombreActividad.Trim();

        var estadoPre =
            ObtenerEstadoPre(
                matriculaLimpia);

        // ============================================================
        // PRE REPROBADO:
        // USAR CAPTURAS ORIGINALES
        // ============================================================
        if (estadoPre.Reprobado)
        {
            if (materia.PreOriginals != null &&
                materia.PreOriginals.TryGetValue(
                    matriculaLimpia,
                    out var original) &&
                original != null &&
                original.CapturasOriginal != null)
            {
                if (original.CapturasOriginal.TryGetValue(
                        actividadLimpia,
                        out double valorOriginal))
                {
                    return FormatearNumero(
                        valorOriginal);
                }

                foreach (var par
                         in original.CapturasOriginal)
                {
                    if (string.Equals(
                            par.Key?.Trim(),
                            actividadLimpia,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return FormatearNumero(
                            par.Value);
                    }
                }
            }

            // Si no existe respaldo para esa actividad,
            // usar la captura actual como último recurso.
        }

        // ============================================================
        // PRE APROBADO O SIN PRE:
        // USAR CAPTURA ACTUAL
        // ============================================================

        if (materia.Calificaciones == null)
            return "";

        if (!materia.Calificaciones.TryGetValue(
                matriculaLimpia,
                out var captura) ||
            captura == null)
        {
            return "";
        }

        if (captura.TryGetValue(
                actividadLimpia,
                out double valor))
        {
            return FormatearNumero(
                valor);
        }

        foreach (var par in captura)
        {
            if (string.Equals(
                    par.Key?.Trim(),
                    actividadLimpia,
                    StringComparison.OrdinalIgnoreCase))
            {
                return FormatearNumero(
                    par.Value);
            }
        }

        return "";
    }

    // ============================================================
    // CALIFICACIÓN DE PARCIAL PARA REPORTE
    //
    // PA:
    //   PRE = 6, por lo tanto el parcial es 6.
    //
    // PR:
    //   se calcula a partir de PreOriginals.
    //
    // SIN PRE:
    //   se toma del alumno.
    // ============================================================
    private string ObtenerCalificacionParcialReporte(
        Alumno alumno,
        MateriaParcial? materia,
        string evaluacion)
    {
        var estadoPre =
            ObtenerEstadoPre(
                alumno.Matricula);

        if (estadoPre.Aprobado &&
            estadoPre.Calificacion == 6)
        {
            return "6.0";
        }

        if (estadoPre.Reprobado &&
            materia?.PreOriginals != null &&
            materia.PreOriginals.TryGetValue(
                alumno.Matricula,
                out var original) &&
            original != null)
        {
            double calificacion =
                CalcularCalificacionDesdeCapturas(
                    materia,
                    alumno.Matricula,
                    original.CapturasOriginal);

            return FormatearNumero(
                TruncarADecimal(
                    calificacion,
                    1),
                1);
        }

        return ObtenerCalificacionTruncada(
            alumno,
            evaluacion);
    }

    // ============================================================
    // CALIFICACIÓN DESDE CAPTURAS ORIGINALES
    // ============================================================
    private double CalcularCalificacionDesdeCapturas(
        MateriaParcial materia,
        string matricula,
        Dictionary<string, double> capturas)
    {
        if (materia == null ||
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

        foreach (var actividad
                 in actividades)
        {
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

        foreach (var actividad
                 in actividades)
        {
            double obtenido;

            if (!capturas.TryGetValue(
                    actividad.Nombre.Trim(),
                    out obtenido))
            {
                bool encontrado =
                    false;

                foreach (var par in capturas)
                {
                    if (string.Equals(
                            par.Key?.Trim(),
                            actividad.Nombre.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        obtenido =
                            par.Value;

                        encontrado =
                            true;

                        break;
                    }
                }

                if (!encontrado)
                {
                    return 0.0;
                }
            }

            if (obtenido < 0 ||
                obtenido >
                actividad.PuntajeMaximo)
            {
                return 0.0;
            }

            decimal porcentajeNormalizado =
                (decimal)actividad.Porcentaje *
                escala;

            acumulado +=
                ((decimal)obtenido /
                 (decimal)actividad.PuntajeMaximo) *
                porcentajeNormalizado;
        }

        return (double)(
            acumulado /
            10m);
    }

    // ============================================================
    // RESULTADO
    // ============================================================
    private void ComposeResultCell(
        TableDescriptor table,
        string background,
        string valor,
        bool esProm = false,
        bool esSem = false)
    {
        bool reprobado =
            EsCalificacionReprobada(
                valor,
                esProm,
                esSem);

        table.Cell()
            .Background(background)
            .Border(_borderWidth)
            .MinHeight(StudentCellHeight)
            .PaddingHorizontal(0.2f)
            .PaddingVertical(0)
            .AlignCenter()
            .AlignMiddle()
            .Text(text =>
            {
                var span =
                    text.Span(valor)
                        .FontSize(_fontSize)
                        .FontFamily(FontFamily)
                        .Bold();

                if (reprobado)
                {
                    span
                        .FontColor(ColorRed)
                        .Underline();
                }
            });
    }

    // ============================================================
    // REPROBADO
    // ============================================================
    private bool EsCalificacionReprobada(
        string valor,
        bool esProm,
        bool esSem)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return false;
        }

        if (!TryObtenerDouble(
                valor,
                out double calificacion))
        {
            return false;
        }

        if (esProm)
        {
            return
                calificacion >= 0.00 &&
                calificacion <= 5.99;
        }

        if (esSem)
        {
            return
                Math.Abs(
                    calificacion - 5.0) <
                0.000001;
        }

        return false;
    }

    // ============================================================
    // CELDA PRE
    // ============================================================
    private void ComposePreCell(
        TableDescriptor table,
        string background,
        string valor)
    {
        table.Cell()
            .Background(background)
            .Border(_borderWidth)
            .Height(StudentCellHeight)
            .Padding(0)
            .AlignCenter()
            .AlignMiddle()
            .Text(text =>
            {
                text.Span(valor)
                    .FontSize(_fontSize)
                    .FontFamily(FontFamily)
                    .Bold();
            });
    }

    // ============================================================
    // CELDA ACTIVIDAD
    // ============================================================
    private void ComposeGradeCell(
        TableDescriptor table,
        string background,
        string valor)
    {
        table.Cell()
            .Background(background)
            .Border(_borderWidth)
            .MinHeight(StudentCellHeight)
            .PaddingHorizontal(0.2f)
            .PaddingVertical(0)
            .AlignCenter()
            .AlignMiddle()
            .Text(valor)
            .FontSize(_fontSize)
            .FontFamily(FontFamily);
    }

    // ============================================================
    // CALIFICACIÓN PARCIAL TRUNCADA NORMAL
    // ============================================================
    private string ObtenerCalificacionTruncada(
        Alumno alumno,
        string evaluacion)
    {
        string raw =
            ObtenerCalificacionSinTruncar(
                alumno,
                evaluacion);

        if (string.IsNullOrWhiteSpace(
                raw))
        {
            return "";
        }

        if (TryObtenerDouble(
                raw,
                out double valor))
        {
            return FormatearNumero(
                TruncarADecimal(
                    valor,
                    1),
                1);
        }

        return raw;
    }

    // ============================================================
    // CALIFICACIÓN ALUMNO
    // ============================================================
    private string ObtenerCalificacionSinTruncar(
        Alumno alumno,
        string evaluacion)
    {
        if (alumno == null)
            return "";

        return alumno.Calificación[
                evaluacion]
            ?.Trim()
            ?? "";
    }

    // ============================================================
    // EXAM PARA REPORTE
    // ============================================================
    private string ObtenerExamenReporte(
        Alumno alumno)
    {
        var estadoPre =
            ObtenerEstadoPre(
                alumno.Matricula);

        if (estadoPre.Aprobado &&
            estadoPre.Calificacion == 6)
        {
            return "6";
        }

        if (estadoPre.Reprobado)
        {
            var materiaPreP2 =
                _parcialP2;

            var materiaPreP3 =
                _parcialP3;

            // La columna EXAM utiliza el valor SEM persistido.
            // Si existe PRE PR, su SEM original se guarda en
            // CALIF__PRE y debe ser utilizado.
            var materiaPre =
                _preService != null &&
                !string.IsNullOrWhiteSpace(
                    _capBaseName)
                    ? ObtenerMateriaPreActual()
                    : null;

            if (materiaPre?.Calificaciones != null &&
                materiaPre.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var registro) &&
                registro != null &&
                registro.TryGetValue(
                    "__PRE_SEM_ORIGINAL_VALIDO__",
                    out double valido) &&
                valido > 0 &&
                registro.TryGetValue(
                    "__PRE_SEM_ORIGINAL__",
                    out double semOriginal))
            {
                return FormatearNumero(
                    semOriginal);
            }
        }

        return ObtenerCalificacionSinTruncar(
            alumno,
            "SEM");
    }

    // ============================================================
    // OBTENER MATERIA PRE
    // ============================================================
    private MateriaParcial? ObtenerMateriaPreActual()
    {
        if (string.IsNullOrWhiteSpace(
                _capBaseName))
        {
            return null;
        }

        var servicio =
            new ParcialJsonService();

        return servicio.ObtenerMateria(
            $"{_capBaseName}__PRE");
    }

    // ============================================================
    // PROMEDIO
    // ============================================================
    private string CalcularPromedioTruncado(
        Alumno alumno)
    {
        var estadoPre =
            ObtenerEstadoPre(
                alumno.Matricula);

        if (estadoPre.Aprobado &&
            estadoPre.Calificacion == 6)
        {
            return "6.0";
        }

        double? p1 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP1,
                "P1");

        double? p2 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP2,
                "P2");

        double? p3 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP3,
                "P3");

        var valores =
            new[] { p1, p2, p3 }
                .Where(x =>
                    x.HasValue)
                .Select(x =>
                    x!.Value)
                .ToList();

        if (valores.Count == 0)
            return "";

        double promedio =
            valores.Average();

        return FormatearNumero(
            TruncarADecimal(
                promedio,
                1),
            1);
    }

    // ============================================================
    // PARCIAL COMO DOUBLE PARA PROMEDIO
    // ============================================================
    private double? ObtenerParcialComoDoubleReporte(
        Alumno alumno,
        MateriaParcial? materia,
        string evaluacion)
    {
        var estadoPre =
            ObtenerEstadoPre(
                alumno.Matricula);

        if (estadoPre.Aprobado &&
            estadoPre.Calificacion == 6)
        {
            return 6.0;
        }

        if (estadoPre.Reprobado &&
            materia?.PreOriginals != null &&
            materia.PreOriginals.TryGetValue(
                alumno.Matricula,
                out var original) &&
            original != null)
        {
            return
                CalcularCalificacionDesdeCapturas(
                    materia,
                    alumno.Matricula,
                    original.CapturasOriginal);
        }

        string valor =
            ObtenerCalificacionSinTruncar(
                alumno,
                evaluacion);

        if (TryObtenerDouble(
                valor,
                out double numero))
        {
            return numero;
        }

        return null;
    }

    // ============================================================
    // SEM FINAL
    // ============================================================
    private string ObtenerSemReporte(
        Alumno alumno)
    {
        var estadoPre =
            ObtenerEstadoPre(
                alumno.Matricula);

        if (estadoPre.Aprobado &&
            estadoPre.Calificacion == 6)
        {
            return "6";
        }

        if (estadoPre.Reprobado)
        {
            var materiaPre =
                ObtenerMateriaPreActual();

            if (materiaPre?.Calificaciones != null &&
                materiaPre.Calificaciones.TryGetValue(
                    alumno.Matricula,
                    out var registro) &&
                registro != null &&
                registro.TryGetValue(
                    "__PRE_SEM_ORIGINAL_VALIDO__",
                    out double valido) &&
                valido > 0 &&
                registro.TryGetValue(
                    "__PRE_SEM_ORIGINAL__",
                    out double semOriginal))
            {
                return FormatearNumero(
                    semOriginal);
            }
        }

        return CalcularSemFinalNormal(
            alumno);
    }

    // ============================================================
    // SEM NORMAL
    // ============================================================
    private string CalcularSemFinalNormal(
        Alumno alumno)
    {
        string promStr =
            CalcularPromedioTruncado(
                alumno);

        if (string.IsNullOrWhiteSpace(
                promStr))
        {
            return "";
        }

        if (!TryObtenerDouble(
                promStr,
                out double prom))
        {
            return "";
        }

        string examStr =
            ObtenerCalificacionSinTruncar(
                alumno,
                "SEM");

        if (string.IsNullOrWhiteSpace(
                examStr))
        {
            return "";
        }

        if (!TryObtenerDouble(
                examStr,
                out double exam))
        {
            return "";
        }

        double? p1 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP1,
                "P1");

        double? p2 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP2,
                "P2");

        double? p3 =
            ObtenerParcialComoDoubleReporte(
                alumno,
                _parcialP3,
                "P3");

        if (!p1.HasValue ||
            !p2.HasValue ||
            !p3.HasValue)
        {
            return "";
        }

        double semCalculado =
            (prom + exam) /
            2.0;

        int semEntero =
            (int)Math.Round(
                semCalculado,
                MidpointRounding.AwayFromZero);

        if (semEntero < 5)
            semEntero = 5;

        return semEntero.ToString(
            CultureInfo.InvariantCulture);
    }

    // ============================================================
    // ACTIVIDAD
    // ============================================================
    private ActividadParcial? ObtenerActividad(
        MateriaParcial? materia,
        int indice)
    {
        if (materia?.Actividades == null ||
            indice < 0 ||
            indice >= materia.Actividades.Count)
        {
            return null;
        }

        return materia.Actividades[
            indice];
    }

    // ============================================================
    // SUMA PORCENTAJES
    // ============================================================
    private double ObtenerSumaPorcentajes(
        MateriaParcial? materia)
    {
        if (materia?.Actividades == null)
            return 0;

        return materia.Actividades
            .Where(a =>
                a != null &&
                a.Activa)
            .Take(4)
            .Sum(a =>
                a.Porcentaje);
    }

    // ============================================================
    // TRUNCAR
    // ============================================================
    private double TruncarADecimal(
        double valor,
        int decimales)
    {
        double factor =
            Math.Pow(
                10,
                decimales);

        return Math.Truncate(
            valor * factor)
            / factor;
    }

    // ============================================================
    // FORMATEAR
    // ============================================================
    private string FormatearNumero(
        double valor,
        int decimales = 2)
    {
        if (Math.Abs(
                valor -
                Math.Truncate(valor))
            < 0.0000001)
        {
            return ((long)Math.Truncate(valor))
                .ToString(
                    CultureInfo.InvariantCulture);
        }

        string formato =
            decimales <= 0
                ? "0"
                : decimales == 1
                    ? "0.0"
                    : "0.##";

        return valor.ToString(
            formato,
            CultureInfo.InvariantCulture);
    }

    // ============================================================
    // TRY DOUBLE
    // ============================================================
    private bool TryObtenerDouble(
        string? texto,
        out double resultado)
    {
        resultado = 0;

        if (string.IsNullOrWhiteSpace(
                texto))
        {
            return false;
        }

        string limpio =
            texto.Trim()
                .Replace(
                    ',',
                    '.');

        return double.TryParse(
            limpio,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out resultado);
    }

    // ============================================================
    // ABREVIAR ACTIVIDAD
    // ============================================================
    private string AbreviarActividad(
        string? nombre)
    {
        if (string.IsNullOrWhiteSpace(
                nombre))
        {
            return "";
        }

        string limpio =
            nombre.Trim()
                .ToUpperInvariant();

        return limpio.Length <= 3
            ? limpio
            : limpio[..3];
    }

    // ============================================================
    // TEXTO VERTICAL
    // ============================================================
    private string ConvertirAVertical(
        string texto)
    {
        if (string.IsNullOrWhiteSpace(
                texto))
        {
            return "";
        }

        return string.Join(
            "\n",
            texto.ToCharArray());
    }
    

    // ============================================================
    // PIE
    // ============================================================
    private void ComposeFooter(
        IContainer container)
    {
        container
            .PaddingTop(5)
            .AlignCenter()
            .Text("")
            .FontSize(_fontSize)
            .FontFamily(FontFamily);
    }
}