using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals
{
    public partial class InfoAlumnoWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Propiedades básicas
        public string Matricula { get; set; }
        public string Nombre { get; set; }
        public string Grupo { get; set; }

        private bool _fotoCargada = false;
        public bool FotoCargada
        {
            get => _fotoCargada;
            set { _fotoCargada = value; OnPropertyChanged(); }
        }

        private bool _fotoCargando = false;
        public bool FotoCargando
        {
            get => _fotoCargando;
            set { _fotoCargando = value; OnPropertyChanged(); }
        }

        private bool _fotoFallida = false;
        public bool FotoFallida
        {
            get => _fotoFallida;
            set { _fotoFallida = value; OnPropertyChanged(); }
        }

        // P1
        public string P1Calif { get; set; } = "N/A";
        public string P1Asistencia { get; set; } = "N/A";
        public string P1Estado { get; set; } = "Sin evaluar";
        public string P1LeyendaFaltas { get; set; } = string.Empty;

        // P2
        public string P2Calif { get; set; } = "N/A";
        public string P2Asistencia { get; set; } = "N/A";
        public string P2Estado { get; set; } = "Sin evaluar";
        public string P2LeyendaFaltas { get; set; } = string.Empty;

        // P3
        public string P3Calif { get; set; } = "N/A";
        public string P3Asistencia { get; set; } = "N/A";
        public string P3Estado { get; set; } = "Sin evaluar";
        public string P3LeyendaFaltas { get; set; } = string.Empty;

        // PROM (Promedio de 3 Parciales)
        public string PromedioParciales { get; set; } = "N/A";
        public string PromedioAsistenciaParciales { get; set; } = "--";

        // SEM
        public string SEMCalif { get; set; } = "N/A";
        public string SEMAsistencia { get; set; } = "N/A";
        public string SEMEstado { get; set; } = "Sin evaluar";
        public string SEMLeyendaFaltas { get; set; } = string.Empty;

        // Promedio Final
        public string PromedioFinal { get; set; } = "N/A";

        public InfoAlumnoWindow(Alumno alumno, Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datosParciales)
        {
            InitializeComponent();

            Matricula = alumno.Matricula;
            Nombre = alumno.Nombre;
            Grupo = alumno.Grupo;

            // Cargar datos de parciales con la nueva lógica de porcentajes
            CargarDatosParciales(datosParciales);

            DataContext = this;

            // Disparar carga de foto de forma asíncrona (sin bloquear la UI)
            FotoCargando = true;
            FotoFallida = false;
            _ = CargarFotoConTimeoutAsync(Matricula);
        }

        private async Task CargarFotoAsync(string matricula)
        {
            string url = $"https://www.prefecotemixco.edu.mx/fotos_alumno/{matricula}.jpg";

            try
            {
                // Descargamos y procesamos la imagen en un hilo de fondo
                var bitmap = await Task.Run(async () =>
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(8);
                        
                        var bytes = await client.GetByteArrayAsync(url);
                        
                        using (var stream = new MemoryStream(bytes))
                        {
                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = stream;
                            bi.EndInit();
                            bi.Freeze();
                            return bi;
                        }
                    }
                });

                // Asignamos a la UI
                FotoAlumnoImage.Source = bitmap;
                FotoCargada = true;
                FotoCargando = false;
            }
            catch
            {
                FotoCargada = false;
                FotoCargando = false;
                System.Diagnostics.Debug.WriteLine($"Fallo al cargar la foto para la matrícula {matricula}.");
            }
        }

        private async Task CargarFotoConTimeoutAsync(string matricula)
        {
            var cargarTask = CargarFotoAsync(matricula);
            var timeoutTask = Task.Delay(6000);

            var finished = await Task.WhenAny(cargarTask, timeoutTask);

            if (finished == timeoutTask)
            {
                if (!FotoCargada)
                {
                    FotoFallida = true;
                    FotoCargando = false;
                }
            }
            else
            {
                if (FotoCargada)
                {
                    FotoFallida = false;
                    FotoCargando = false;
                }
            }
        }

        private void CargarDatosParciales(Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datos)
        {
            int sumTotalClases = 0;
            int sumFaltas = 0;
            bool tieneClasesActivas = false;

            // P1
            if (datos.TryGetValue("P1", out var p1))
            {
                P1Calif = string.IsNullOrWhiteSpace(p1.calif) ? "N/A" : p1.calif;
                P1Asistencia = FormatearAsistencia(p1.faltas, p1.totalClases);
                P1Estado = MapearEstadoTriggers(p1.estado);
                P1LeyendaFaltas = FormatearLeyendaFaltas(p1.estado, p1.faltas, p1.totalClases, "- NP");

                if (p1.totalClases > 0 && p1.faltas >= 0)
                {
                    sumTotalClases += p1.totalClases;
                    sumFaltas += p1.faltas;
                    tieneClasesActivas = true;
                }
            }

            // P2
            if (datos.TryGetValue("P2", out var p2))
            {
                P2Calif = string.IsNullOrWhiteSpace(p2.calif) ? "N/A" : p2.calif;
                P2Asistencia = FormatearAsistencia(p2.faltas, p2.totalClases);
                P2Estado = MapearEstadoTriggers(p2.estado);
                P2LeyendaFaltas = FormatearLeyendaFaltas(p2.estado, p2.faltas, p2.totalClases, "- NP");

                if (p2.totalClases > 0 && p2.faltas >= 0)
                {
                    sumTotalClases += p2.totalClases;
                    sumFaltas += p2.faltas;
                    tieneClasesActivas = true;
                }
            }

            // P3
            if (datos.TryGetValue("P3", out var p3))
            {
                P3Calif = string.IsNullOrWhiteSpace(p3.calif) ? "N/A" : p3.calif;
                P3Asistencia = FormatearAsistencia(p3.faltas, p3.totalClases);
                P3Estado = MapearEstadoTriggers(p3.estado);
                P3LeyendaFaltas = FormatearLeyendaFaltas(p3.estado, p3.faltas, p3.totalClases, "- NP");

                if (p3.totalClases > 0 && p3.faltas >= 0)
                {
                    sumTotalClases += p3.totalClases;
                    sumFaltas += p3.faltas;
                    tieneClasesActivas = true;
                }
            }

            // Acumulado Asistencia Parciales
            if (tieneClasesActivas && sumTotalClases > 0)
            {
                int asistenciasAcumuladas = sumTotalClases - sumFaltas;
                double porcentajeAcumulado = ((double)asistenciasAcumuladas / sumTotalClases) * 100.0;
                PromedioAsistenciaParciales = $"{asistenciasAcumuladas}/{sumTotalClases} ({porcentajeAcumulado:0.#}%)";
            }
            else
            {
                PromedioAsistenciaParciales = "--";
            }

            // SEM
            if (datos.TryGetValue("SEM", out var sem))
            {
                SEMCalif = string.IsNullOrWhiteSpace(sem.calif) ? "N/A" : sem.calif;
                SEMAsistencia = FormatearAsistencia(sem.faltas, sem.totalClases);
                SEMEstado = MapearEstadoTriggers(sem.estado);
                SEMLeyendaFaltas = FormatearLeyendaFaltas(sem.estado, sem.faltas, sem.totalClases, "- NP (NO SE PRESENTÓ)");
            }

            CalcularPromedioFinal();
        }

        // --- Helpers de lógicas visuales para la tabla ---

        private string MapearEstadoTriggers(string estado)
        {
            if (estado == "NP") return "Reprobado por faltas";
            if (string.IsNullOrWhiteSpace(estado)) return "Sin evaluar";
            return estado;
        }

        private string FormatearAsistencia(int faltas, int totalClases)
        {
            if (totalClases <= 0 || faltas < 0) return "--";
            int asistencias = totalClases - faltas;
            return $"{asistencias}/{totalClases}";
        }

        private string FormatearLeyendaFaltas(string estado, int faltas, int totalClases, string prefijo)
        {
            if (estado == "NP" || estado == "Reprobado por faltas")
            {
                if (totalClases > 0 && faltas >= 0)
                {
                    double porcentaje = ((double)(totalClases - faltas) / totalClases) * 100.0;
                    return $"{prefijo} ({porcentaje:0.#}%)";
                }
                return prefijo;
            }
            return string.Empty;
        }

        // --- Lógica exacta de matemáticas y redondeo ---

        private void CalcularPromedioFinal()
        {
            double suma = 0;
            int count = 0;

            if (double.TryParse(P1Calif, out double p1) && p1 >= 0) { suma += p1; count++; }
            if (double.TryParse(P2Calif, out double p2) && p2 >= 0) { suma += p2; count++; }
            if (double.TryParse(P3Calif, out double p3) && p3 >= 0) { suma += p3; count++; }

            if (count == 3)
            {
                double promedioParciales = suma / 3.0;
                double promedioParcialesTruncado = Math.Truncate(promedioParciales * 100) / 100.0;

                PromedioParciales = promedioParcialesTruncado.ToString("0.00");
                
                double sem = 0;
                bool tieneSem = double.TryParse(SEMCalif, out sem) && sem >= 0;
                
                if (tieneSem)
                {
                    double promedioFinal = (promedioParcialesTruncado + sem) / 2.0;
                    int promedioFinalRedondeado = (int)Math.Round(promedioFinal, 0, MidpointRounding.AwayFromZero);
                    PromedioFinal = promedioFinalRedondeado.ToString();
                }
                else
                {
                    int promedioParcialesRedondeado = (int)Math.Round(promedioParcialesTruncado, 0, MidpointRounding.AwayFromZero);
                    PromedioFinal = promedioParcialesRedondeado.ToString();
                }
            }
            else
            {
                PromedioParciales = "N/A";
                PromedioFinal = "N/A";
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}