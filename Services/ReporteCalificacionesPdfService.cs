using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services
{
    public class ReporteCalificacionesPdfService
    {
        private readonly float _fontSize = 8.5f;
        private readonly float _borderWidth = 0.3f;

        private const string ColorGray = "#ADADAD";
        private const string ColorWhite = "#FFFFFF";
        private const string FontFamily = "Arial";

        public ReporteCalificacionesPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = true;
        }

        public byte[] GenerarDiseno()
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.Letter);

                    page.MarginLeft(5);
                    page.MarginRight(5);
                    page.MarginTop(20);
                    page.MarginBottom(20);

                    page.PageColor(Colors.White);

                    page.Header()
                        .PaddingLeft(20)
                        .PaddingRight(20)
                        .Element(ComposeHeader);

                    page.Content()
                        .PaddingLeft(20)
                        .PaddingRight(20)
                        .Element(ComposeContent);

                    page.Footer()
                        .Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        // ============================================================
        // ENCABEZADO DEL DOCUMENTO
        // ============================================================

        private void ComposeHeader(IContainer container)
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
                            // PARTE IZQUIERDA
                            row.RelativeItem()
                                .PaddingRight(4)
                                .Column(left =>
                                {
                                    left.Item()
                                        .Text("SECRETARÍA DE EDUCACIÓN PÚBLICA")
                                        .FontSize(_fontSize)
                                        .FontFamily(FontFamily)
                                        .SemiBold();

                                    left.Item()
                                        .Text("DIRECCIÓN GENERAL DEL BACHILLERATO")
                                        .FontSize(_fontSize)
                                        .FontFamily(FontFamily);

                                    left.Item()
                                        .Text("EDUCACIÓN MEDIA SUPERIOR")
                                        .FontSize(_fontSize)
                                        .FontFamily(FontFamily);

                                    left.Item()
                                        .Text("JOSE MA. MORELOS Y PAVON")
                                        .FontSize(_fontSize)
                                        .FontFamily(FontFamily)
                                        .SemiBold();
                                });

                            // SEPARADOR
                            row.ConstantItem(6)
                                .LineVertical(1)
                                .LineColor(Colors.Black);

                            // PARTE DERECHA
                            row.RelativeItem()
                                .PaddingLeft(4)
                                .Column(right =>
                                {
                                    right.Item()
                                        .PaddingTop(4)
                                        .PaddingBottom(1)
                                        .Text(text =>
                                        {
                                            text.Span("GRUPO: ")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily);

                                            text.Span("________")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily)
                                                .SemiBold();
                                        });

                                    right.Item()
                                        .PaddingBottom(1)
                                        .Text(text =>
                                        {
                                            text.Span("ASIGNATURA: ")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily);

                                            text.Span("____________________________")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily)
                                                .SemiBold();
                                        });

                                    right.Item()
                                        .PaddingBottom(1)
                                        .Text(text =>
                                        {
                                            text.Span("PROFR(A).: ")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily);

                                            text.Span("____________________________")
                                                .FontSize(_fontSize)
                                                .FontFamily(FontFamily)
                                                .SemiBold();
                                        });
                                });
                        });

                    column.Item()
                        .PaddingVertical(5)
                        .AlignCenter()
                        .Text("FORMAS PARA EVIDENCIAR EL APRENDIZAJE")
                        .FontSize(10)
                        .FontFamily(FontFamily)
                        .Bold();
                });
        }

        // ============================================================
        // CONTENIDO
        // ============================================================

        private void ComposeContent(IContainer container)
        {
            container
                .AlignCenter()
                .Table(table =>
                {
                    // =================================================
                    // 20 COLUMNAS
                    // =================================================
                    //
                    //  1  = N
                    //  2  = NOMBRE
                    //
                    //  P1
                    //  3  = A
                    //  4  = B
                    //  5  = C
                    //  6  = D
                    //  7  = PAR1
                    //
                    //  P2
                    //  8  = A
                    //  9  = B
                    // 10  = C
                    // 11  = D
                    // 12  = PAR2
                    //
                    //  P3
                    // 13  = A
                    // 14  = B
                    // 15  = C
                    // 16  = D
                    // 17  = PAR3
                    //
                    // 18  = PROM
                    // 19  = EXAM
                    // 20  = SEM
                    // =================================================

                    table.ColumnsDefinition(columns =>
                    {
                        // N
                        columns.ConstantColumn(14);

                        // NOMBRE
                        columns.ConstantColumn(220);

                        // P1
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);

                        // P2
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);

                        // P3
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);

                        // RESULTADOS
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                        columns.ConstantColumn(14);
                    });

                    // =================================================
                    // ENCABEZADO
                    // =================================================

                    table.Header(header =>
                    {
                        ComposeHeaderRow1(header);
                        ComposeHeaderRow2(header);
                        ComposeHeaderRow3(header);
                        ComposeHeaderRow4(header);
                    });

                    // =================================================
                    // ALUMNOS
                    // =================================================

                    for (int i = 0; i < 55; i++)
                    {
                        ComposeStudentRow(table, i);
                    }
                });
        }

        // ============================================================
        // FILA 1 DEL ENCABEZADO
        // ============================================================

        private void ComposeHeaderRow1(TableCellDescriptor header)
        {
            // N
            header.Cell()
                .RowSpan(4)
                .Background(ColorGray)
                .Border(_borderWidth)
                .Padding(0.5f)
                .AlignCenter()
                .AlignMiddle()
                .Text("N")
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();

            // NOMBRE
            header.Cell()
                .RowSpan(4)
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0)
                .Row(row =>
                {
                    row.RelativeItem()
                        .BorderRight(_borderWidth)
                        .BorderColor(Colors.Black)
                        .Padding(0.5f)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("N O M B R E")
                        .FontSize(_fontSize)
                        .FontFamily(FontFamily)
                        .Bold();

                    row.RelativeItem()
                        .Padding(0.5f)
                        .PaddingRight(2)
                        .AlignRight()
                        .AlignMiddle()
                        .Text("Formas para evidenciar el aprendizaje:")
                        .FontSize(_fontSize)
                        .FontFamily(FontFamily)
                        .Bold();
                });

            // ========================================================
            // P1
            // ========================================================

            header.Cell()
                .ColumnSpan(4)
                .Background(ColorGray)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text("")
                .FontSize(_fontSize)
                .FontFamily(FontFamily);

            header.Cell()
                .RowSpan(4)
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.5f)
                .AlignCenter()
                .AlignMiddle()
                .Text("P\nA\nR\n1")
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();

            // ========================================================
            // P2
            // ========================================================

            header.Cell()
                .ColumnSpan(4)
                .Background(ColorGray)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text("")
                .FontSize(_fontSize)
                .FontFamily(FontFamily);

            header.Cell()
                .RowSpan(4)
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.5f)
                .AlignCenter()
                .AlignMiddle()
                .Text("P\nA\nR\n2")
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();

            // ========================================================
            // P3
            // ========================================================

            header.Cell()
                .ColumnSpan(4)
                .Background(ColorGray)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text("")
                .FontSize(_fontSize)
                .FontFamily(FontFamily);

            header.Cell()
                .RowSpan(4)
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.5f)
                .AlignCenter()
                .AlignMiddle()
                .Text("P\nA\nR\n3")
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();

            // ========================================================
            // RESULTADOS
            // ========================================================

            ComposeVerticalHeader(header, "P\nR\nO\nM");
            ComposeVerticalHeader(header, "E\nX\nA\nM");
            ComposeVerticalHeader(header, "S\nE\nM");
        }

        // ============================================================
        // FILA 2
        // ============================================================

        private void ComposeHeaderRow2(TableCellDescriptor header)
        {
            // P1
            ComposeActivityHeaderCell(header, "A");
            ComposeActivityHeaderCell(header, "B");
            ComposeActivityHeaderCell(header, "C");
            ComposeActivityHeaderCell(header, "D");

            // P2
            ComposeActivityHeaderCell(header, "A");
            ComposeActivityHeaderCell(header, "B");
            ComposeActivityHeaderCell(header, "C");
            ComposeActivityHeaderCell(header, "D");

            // P3
            ComposeActivityHeaderCell(header, "A");
            ComposeActivityHeaderCell(header, "B");
            ComposeActivityHeaderCell(header, "C");
            ComposeActivityHeaderCell(header, "D");
        }

        // ============================================================
        // FILA 3
        // ============================================================

        private void ComposeHeaderRow3(TableCellDescriptor header)
        {
            // P1
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");

            // P2
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");

            // P3
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
            ComposeActivityHeaderCell(header, "");
        }

        // ============================================================
        // FILA 4
        // ============================================================

        private void ComposeHeaderRow4(TableCellDescriptor header)
        {
            // P1
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");

            // P2
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");

            // P3
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
            ComposeVerticalActivityHeaderCell(header, "");
        }

        // ============================================================
        // CELDA DE ACTIVIDAD
        // ============================================================

        private void ComposeActivityHeaderCell(
            TableCellDescriptor table,
            string text)
        {
            table.Cell()
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }

        // ============================================================
        // CELDA VERTICAL DE ACTIVIDAD
        // ============================================================

        private void ComposeVerticalActivityHeaderCell(
            TableCellDescriptor table,
            string text)
        {
            table.Cell()
                .Background(ColorWhite)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(20)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }

        // ============================================================
        // ENCABEZADOS PROM / EXAM / SEM
        // ============================================================

        private void ComposeVerticalHeader(
            TableCellDescriptor header,
            string text)
        {
            header.Cell()
                .RowSpan(4)
                .Background(ColorGray)
                .Border(_borderWidth)
                .Padding(0.5f)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }

        // ============================================================
        // FILA DEL ALUMNO
        // ============================================================

        private void ComposeStudentRow(
            TableDescriptor table,
            int index)
        {
            string background =
                index % 2 == 0
                    ? ColorGray
                    : ColorWhite;

            // ========================================================
            // NÚMERO
            // ========================================================

            table.Cell()
                .Background(background)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text((index + 1).ToString())
                .FontSize(_fontSize)
                .FontFamily(FontFamily);

            // ========================================================
            // NOMBRE
            // ========================================================

            table.Cell()
                .Background(background)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignLeft()
                .AlignMiddle()
                .Text(" ALUMNO DE EJEMPLO")
                .FontSize(_fontSize)
                .FontFamily(FontFamily);

            // ========================================================
            // P1
            // ========================================================

            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");

            ComposeResultCell(table, background, "");

            // ========================================================
            // P2
            // ========================================================

            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");

            ComposeResultCell(table, background, "");

            // ========================================================
            // P3
            // ========================================================

            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");
            ComposeGradeCell(table, background, "0");

            ComposeResultCell(table, background, "");

            // ========================================================
            // RESULTADOS
            // ========================================================

            ComposeResultCell(table, background, "");
            ComposeResultCell(table, background, "");
            ComposeResultCell(table, background, "");
        }

        // ============================================================
        // CELDA DE CALIFICACIÓN
        // ============================================================

        private void ComposeGradeCell(
            TableDescriptor table,
            string background,
            string value)
        {
            table.Cell()
                .Background(background)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text(value)
                .FontSize(_fontSize)
                .FontFamily(FontFamily);
        }

        // ============================================================
        // CELDA DE RESULTADO
        // ============================================================

        private void ComposeResultCell(
            TableDescriptor table,
            string background,
            string value)
        {
            table.Cell()
                .Background(background)
                .Border(_borderWidth)
                .Padding(0.5f)
                .MinHeight(6)
                .AlignCenter()
                .AlignMiddle()
                .Text(value)
                .FontSize(_fontSize)
                .FontFamily(FontFamily)
                .Bold();
        }

        // ============================================================
        // PIE
        // ============================================================

        private void ComposeFooter(IContainer container)
        {
            container
                .PaddingTop(5)
                .AlignCenter()
                .Text("")
                .FontSize(7)
                .FontFamily(FontFamily);
        }
    }
}