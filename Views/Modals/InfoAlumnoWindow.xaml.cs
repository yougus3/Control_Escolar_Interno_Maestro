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

        // P1
        public string P1Calif { get; set; } = "N/A";
        public string P1Asistencia { get; set; } = "N/A";
        public string P1Estado { get; set; } = "Sin evaluar";

        // P2
        public string P2Calif { get; set; } = "N/A";
        public string P2Asistencia { get; set; } = "N/A";
        public string P2Estado { get; set; } = "Sin evaluar";

        // P3
        public string P3Calif { get; set; } = "N/A";
        public string P3Asistencia { get; set; } = "N/A";
        public string P3Estado { get; set; } = "Sin evaluar";

        // PROM (Promedio de 3 Parciales)
        public string PromedioParciales { get; set; } = "N/A";

        // SEM
        public string SEMCalif { get; set; } = "N/A";
        public string SEMEstado { get; set; } = "Sin evaluar";

        // Promedio Final
        public string PromedioFinal { get; set; } = "N/A";

        public InfoAlumnoWindow(Alumno alumno, Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datosParciales)
        {
            InitializeComponent();

            Matricula = alumno.Matricula;
            Nombre = alumno.Nombre;
            Grupo = alumno.Grupo;

            // Cargar datos de parciales
            CargarDatosParciales(datosParciales);

            DataContext = this;

            // Disparar carga de foto de forma asíncrona (sin bloquear la UI)
            _ = CargarFotoAsync(Matricula);
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
            }
            catch
            {
                // Si la imagen no existe o el servidor falla, no crashea, simplemente se queda vacío
                FotoCargada = false;
                System.Diagnostics.Debug.WriteLine($"Fallo al cargar la foto para la matrícula {matricula}.");
            }
        }

        private void CargarDatosParciales(Dictionary<string, (string calif, string estado, int faltas, int totalClases)> datos)
        {
            // P1
            if (datos.TryGetValue("P1", out var p1))
            {
                P1Calif = string.IsNullOrWhiteSpace(p1.calif) ? "N/A" : p1.calif;
                P1Asistencia = $"{p1.faltas} / {p1.totalClases} faltas";
                P1Estado = p1.estado;
            }

            // P2
            if (datos.TryGetValue("P2", out var p2))
            {
                P2Calif = string.IsNullOrWhiteSpace(p2.calif) ? "N/A" : p2.calif;
                P2Asistencia = $"{p2.faltas} / {p2.totalClases} faltas";
                P2Estado = p2.estado;
            }

            // P3
            if (datos.TryGetValue("P3", out var p3))
            {
                P3Calif = string.IsNullOrWhiteSpace(p3.calif) ? "N/A" : p3.calif;
                P3Asistencia = $"{p3.faltas} / {p3.totalClases} faltas";
                P3Estado = p3.estado;
            }

            // SEM
            if (datos.TryGetValue("SEM", out var sem))
            {
                SEMCalif = string.IsNullOrWhiteSpace(sem.calif) ? "N/A" : sem.calif;
                SEMEstado = sem.estado;
            }

            CalcularPromedioFinal();
        }

        private void CalcularPromedioFinal()
        {
            double suma = 0;
            int count = 0;

            if (double.TryParse(P1Calif, out double p1) && p1 >= 0) { suma += p1; count++; }
            if (double.TryParse(P2Calif, out double p2) && p2 >= 0) { suma += p2; count++; }
            if (double.TryParse(P3Calif, out double p3) && p3 >= 0) { suma += p3; count++; }

            if (count == 3)
            {
                // Paso 1: Promedio de los 3 parciales (solo primer decimal, SIN redondear)
                double promedioParciales = suma / 3.0;
                double promedioParcialesTruncado = Math.Truncate(promedioParciales * 10) / 10;
                
                // Asignar el promedio de parciales para mostrar en la tabla
                PromedioParciales = promedioParcialesTruncado.ToString("0.0");
                
                // Paso 2: Obtener SEM
                double sem = 0;
                bool tieneSem = double.TryParse(SEMCalif, out sem) && sem >= 0;
                
                if (tieneSem)
                {
                    // Paso 3: Promedio Final = (PromedioParcialesTruncado + SEM) / 2
                    double promedioFinal = (promedioParcialesTruncado + sem) / 2.0;
                    // Redondear al entero más cercano (.5 hacia arriba)
                    int promedioFinalRedondeado = (int)Math.Round(promedioFinal, 0, MidpointRounding.AwayFromZero);
                    PromedioFinal = promedioFinalRedondeado.ToString();
                }
                else
                {
                    // Redondear al entero más cercano (.5 hacia arriba)
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