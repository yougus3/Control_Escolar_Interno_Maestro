using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.ViewModels;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views.Modals;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views
{
    public partial class PreextraordinarioView : UserControl
    {
        private MainViewModel? _mainVm;
        private readonly PreExtraordinarioService _preService;

        private bool _cargando;
        private bool _guardando;

        public ObservableCollection<AlumnoPreItem> AlumnosPre { get; } =
            new();

        public PreextraordinarioView()
        {
            InitializeComponent();

            _preService =
                new PreExtraordinarioService();

            DataContext =
                this;

            Loaded +=
                PreextraordinarioView_Loaded;
        }

        private void PreextraordinarioView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_mainVm == null)
                _mainVm =
                    FindMainViewModel();

            if (_mainVm == null)
                return;

            CargarAlumnos();
        }

        private MainViewModel? FindMainViewModel()
        {
            if (Application.Current?.MainWindow?.DataContext
                is MainViewModel mainVm)
            {
                return mainVm;
            }

            DependencyObject? actual =
                this;

            while (actual != null)
            {
                if (actual is FrameworkElement fe &&
                    fe.DataContext is MainViewModel vm)
                {
                    return vm;
                }

                actual =
                    LogicalTreeHelper.GetParent(
                        actual);
            }

            return null;
        }

        public void Recargar()
        {
            if (_mainVm == null)
                _mainVm =
                    FindMainViewModel();

            if (_mainVm == null)
                return;

            CargarAlumnos();
        }

        // ============================================================
        // CARGAR ALUMNOS
        // ============================================================

        private void CargarAlumnos()
        {
            if (_mainVm == null)
                return;

            try
            {
                _cargando =
                    true;

                AlumnosPre.Clear();

                string claveMateria =
                    ObtenerClaveMateria();

                if (string.IsNullOrWhiteSpace(
                        claveMateria))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[PRE] No se pudo obtener la clave base del CAP.");

                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[PRE] Clave base CAP usada para PRE = '{claveMateria}'");

                var alumnos =
                    _preService.ObtenerAlumnosParaPre(
                        claveMateria,
                        _mainVm.Alumnos);

                int candidatos =
                    alumnos.Count;

                int autorizados =
                    0;

                foreach (var alumno in alumnos)
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

                    bool tieneDerecho =
                        _mainVm.AlumnoTieneDerechoPre(
                            matricula);

                    System.Diagnostics.Debug.WriteLine(
                        $"[PRE] Alumno {matricula} -> derecho={tieneDerecho}");

                    if (!tieneDerecho)
                        continue;

                    autorizados++;

                    AlumnosPre.Add(
                        new AlumnoPreItem(
                            alumno.Matricula,
                            alumno.Nombre,
                            alumno.Grupo,
                            alumno.P1,
                            alumno.P2,
                            alumno.P3,
                            alumno.Promedio,
                            alumno.Pre,
                            alumno.Estado));
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[PRE] Candidatos = {candidatos}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PRE] Autorizados mostrados = {autorizados}");
            }
            finally
            {
                _cargando =
                    false;
            }
        }

        // ============================================================
        // CLAVE BASE DEL CAP
        // ============================================================

        private string ObtenerClaveMateria()
        {
            if (_mainVm == null)
                return string.Empty;

            string? rutaCap =
                _mainVm.ArchivoCompletoActual;

            if (string.IsNullOrWhiteSpace(
                    rutaCap) ||
                !File.Exists(
                    rutaCap))
            {
                return string.Empty;
            }

            try
            {
                string nombreArchivo =
                    Path.GetFileNameWithoutExtension(
                        rutaCap);

                if (string.IsNullOrWhiteSpace(
                        nombreArchivo))
                {
                    return string.Empty;
                }

                return nombreArchivo
                    .Trim()
                    .Replace(
                        ' ',
                        '_');
            }
            catch
            {
                return string.Empty;
            }
        }

        // ============================================================
        // PRE: VALIDACIÓN DE CAPTURA
        // ============================================================

        private void PreTextBox_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            string textoActual =
                textBox.Text ?? string.Empty;

            // N / n -> NP
            if (e.Text.Equals(
                    "N",
                    StringComparison.OrdinalIgnoreCase))
            {
                textBox.Text =
                    "NP";

                textBox.CaretIndex =
                    textBox.Text.Length;

                e.Handled =
                    true;

                return;
            }

            // Si ya es NP, no permitir más caracteres
            if (textoActual.Equals(
                    "NP",
                    StringComparison.OrdinalIgnoreCase))
            {
                e.Handled =
                    true;

                return;
            }

            // Solo dígitos
            if (string.IsNullOrEmpty(e.Text) ||
                !char.IsDigit(e.Text[0]))
            {
                e.Handled =
                    true;

                return;
            }

            string nuevoTexto;

            if (textBox.SelectionLength > 0)
            {
                nuevoTexto =
                    textoActual.Remove(
                            textBox.SelectionStart,
                            textBox.SelectionLength)
                        .Insert(
                            textBox.SelectionStart,
                            e.Text);
            }
            else
            {
                nuevoTexto =
                    textoActual.Insert(
                        textBox.SelectionStart,
                        e.Text);
            }

            // Solo enteros 0 a 10
            if (!int.TryParse(
                    nuevoTexto,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int valor) ||
                valor < 0 ||
                valor > 10)
            {
                e.Handled =
                    true;

                return;
            }

            // Máximo dos caracteres
            if (nuevoTexto.Length > 2)
            {
                e.Handled =
                    true;

                return;
            }

            e.Handled =
                false;
        }

        private void PreTextBox_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // NP + Backspace/Delete = borrar TODO el campo
            if ((e.Key == Key.Back ||
                 e.Key == Key.Delete) &&
                textBox.Text.Equals(
                    "NP",
                    StringComparison.OrdinalIgnoreCase))
            {
                textBox.Clear();
                textBox.CaretIndex =
                    0;

                e.Handled =
                    true;

                return;
            }

            // ENTER = únicamente pasar al siguiente PRE.
            // NO guarda.
            if (e.Key == Key.Enter)
            {
                e.Handled =
                    true;

                Dispatcher.BeginInvoke(
                    new Action(() =>
                        MoverAlSiguienteCampoPre(
                            textBox)),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void PreTextBox_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            // NO guardar automáticamente al perder el foco.
        }

        private void PreTextBox_Pasting(
            object sender,
            DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.CancelCommand();
                return;
            }

            if (!e.DataObject.GetDataPresent(
                    DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string pegado =
                e.DataObject.GetData(
                        DataFormats.Text) as string
                ?? string.Empty;

            pegado =
                pegado.Trim();

            // N / NP
            if (pegado.Equals(
                    "N",
                    StringComparison.OrdinalIgnoreCase) ||
                pegado.Equals(
                    "NP",
                    StringComparison.OrdinalIgnoreCase))
            {
                textBox.Text =
                    "NP";

                textBox.CaretIndex =
                    textBox.Text.Length;

                e.Handled =
                    true;

                return;
            }

            // Solo 0 a 10 sin decimales
            if (!int.TryParse(
                    pegado,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int valor) ||
                valor < 0 ||
                valor > 10)
            {
                e.CancelCommand();
                return;
            }

            textBox.Text =
                valor.ToString(
                    CultureInfo.InvariantCulture);

            textBox.CaretIndex =
                textBox.Text.Length;

            e.Handled =
                true;
        }

        // ============================================================
        // NAVEGACIÓN PRE
        // ============================================================

        private void MoverAlSiguienteCampoPre(
            TextBox campoActual)
        {
            List<TextBox> campos =
                FindVisualChildren<TextBox>(this)
                    .Where(x =>
                        x.DataContext is AlumnoPreItem &&
                        x.IsEnabled &&
                        x.IsVisible)
                    .ToList();

            int indiceActual =
                campos.IndexOf(
                    campoActual);

            if (indiceActual < 0)
                return;

            for (int i = indiceActual + 1;
                 i < campos.Count;
                 i++)
            {
                TextBox siguiente =
                    campos[i];

                if (siguiente.DataContext
                    is not AlumnoPreItem alumno)
                {
                    continue;
                }

                if (alumno.Estado.Equals(
                        "Sin derecho",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                siguiente.Focus();
                siguiente.SelectAll();

                ScrollIntoView(
                    siguiente);

                return;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(
            DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
                yield break;

            int count =
                VisualTreeHelper.GetChildrenCount(
                    parent);

            for (int i = 0;
                 i < count;
                 i++)
            {
                DependencyObject child =
                    VisualTreeHelper.GetChild(
                        parent,
                        i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (T descendant
                         in FindVisualChildren<T>(
                             child))
                {
                    yield return descendant;
                }
            }
        }

        private void ScrollIntoView(
            DependencyObject elemento)
        {
            DependencyObject? actual =
                elemento;

            while (actual != null)
            {
                if (actual is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToVerticalOffset(
                        scrollViewer.VerticalOffset);

                    break;
                }

                actual =
                    VisualTreeHelper.GetParent(
                        actual);
            }
        }

        private static string ObtenerTextoResultante(
            TextBox textBox,
            string nuevoTexto)
        {
            string actual =
                textBox.Text ?? string.Empty;

            int inicio =
                textBox.SelectionStart;

            int longitud =
                textBox.SelectionLength;

            string antes =
                inicio > 0
                    ? actual.Substring(
                        0,
                        inicio)
                    : string.Empty;

            string despues =
                inicio + longitud < actual.Length
                    ? actual.Substring(
                        inicio + longitud)
                    : string.Empty;

            return antes +
                   nuevoTexto +
                   despues;
        }

        // ============================================================
        // GUARDAR DESDE TEXTBOX
        // ============================================================
        //
        // Se conserva para no eliminar funcionalidad existente,
        // pero YA NO realiza ninguna persistencia.
        // ============================================================

        private void GuardarDesdeTextBox(
            TextBox textBox)
        {
            if (textBox.DataContext
                is not AlumnoPreItem item)
            {
                return;
            }

            string texto =
                textBox.Text?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    texto))
            {
                item.Pre =
                    string.Empty;

                return;
            }

            if (texto.Equals(
                    "NP",
                    StringComparison.OrdinalIgnoreCase))
            {
                item.Pre =
                    "NP";

                return;
            }

            if (!int.TryParse(
                    texto,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int resultado))
            {
                textBox.Text =
                    item.Pre;

                textBox.CaretIndex =
                    textBox.Text.Length;

                return;
            }

            if (resultado < 0 ||
                resultado > 10)
            {
                textBox.Text =
                    item.Pre;

                textBox.CaretIndex =
                    textBox.Text.Length;

                return;
            }

            item.Pre =
                resultado.ToString(
                    CultureInfo.InvariantCulture);
        }

        // ============================================================
        // GUARDAR PRE
        // ============================================================
        //
        // IMPORTANTE:
        // Este método ya NO guarda en SQLite/CAP.
        // Solo actualiza el valor visual/en memoria.
        // ============================================================

        private void GuardarPre(
            AlumnoPreItem item,
            int resultado)
        {
            item.Pre =
                resultado.ToString(
                    CultureInfo.InvariantCulture);
        }

        // ============================================================
        // REVERTIR PRE
        // ============================================================
        //
        // Se conserva sin eliminar funcionalidad existente.
        // Ya no es invocado automáticamente por LostFocus/Enter.
        // ============================================================

        private void RevertirPre(
            AlumnoPreItem item)
        {
            if (_mainVm == null)
                return;

            string claveMateria =
                ObtenerClaveMateria();

            if (string.IsNullOrWhiteSpace(
                    claveMateria))
            {
                return;
            }

            var confirmar =
                MessageBox.Show(
                    $"¿Deseas eliminar el PRE de {item.Nombre}?\n\n" +
                    "Se restaurarán P2, P3 y SEM a sus valores originales.",
                    "Revertir PREEXTRAORDINARIO",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmar !=
                MessageBoxResult.Yes)
            {
                item.Pre =
                    item.PreAnterior;

                return;
            }

            try
            {
                _guardando =
                    true;

                var respuesta =
                    _preService.RevertirPrePorAlumno(
                        claveMateria,
                        item.Matricula,
                        _mainVm.Alumnos);

                if (!respuesta.Exito)
                {
                    MessageBox.Show(
                        respuesta.Mensaje,
                        "PREEXTRAORDINARIO",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    item.Pre =
                        item.PreAnterior;

                    return;
                }

                item.Pre =
                    string.Empty;

                item.PreAnterior =
                    string.Empty;

                item.Estado =
                    string.Empty;

                bool guardado =
                    _mainVm.GuardarResultadosPreEnCap();

                if (!guardado)
                {
                    MessageBox.Show(
                        "El PRE fue revertido, pero no fue posible restaurar P2, P3 y SEM en el CAP.",
                        "Advertencia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                ActualizarTodosDesdeMainVm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo revertir el PRE.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                item.Pre =
                    item.PreAnterior;
            }
            finally
            {
                _guardando =
                    false;
            }
        }

        // ============================================================
        // ACTUALIZAR DATOS
        // ============================================================

        private void ActualizarTodosDesdeMainVm()
        {
            if (_mainVm == null)
                return;

            foreach (var item in AlumnosPre)
            {
                ActualizarItemDesdeAlumno(
                    item);
            }
        }

        private void ActualizarItemDesdeAlumno(
            AlumnoPreItem item)
        {
            if (_mainVm == null)
                return;

            var alumno =
                _mainVm.Alumnos.FirstOrDefault(
                    a =>
                        string.Equals(
                            a.Matricula,
                            item.Matricula,
                            StringComparison.OrdinalIgnoreCase));

            if (alumno == null)
                return;

            item.P1 =
                NormalizarVisual(
                    alumno.Calificación["P1"]);

            item.P2 =
                NormalizarVisual(
                    alumno.Calificación["P2"]);

            item.P3 =
                NormalizarVisual(
                    alumno.Calificación["P3"]);

            item.Promedio =
                CalcularPromedioVisual(
                    item.P1,
                    item.P2,
                    item.P3);

            item.Estado =
                _preService
                    .ObtenerEstadoPre(
                        ObtenerClaveMateria(),
                        item.Matricula)
                    .Estado;
        }

        private static string NormalizarVisual(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(
                    valor))
            {
                return string.Empty;
            }

            if (!double.TryParse(
                    valor.Replace(
                        ',',
                        '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double numero))
            {
                return valor;
            }

            return numero.ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        private static string CalcularPromedioVisual(
            string p1Texto,
            string p2Texto,
            string p3Texto)
        {
            if (!double.TryParse(
                    p1Texto.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double p1))
            {
                return string.Empty;
            }

            if (!double.TryParse(
                    p2Texto.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double p2))
            {
                return string.Empty;
            }

            if (!double.TryParse(
                    p3Texto.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double p3))
            {
                return string.Empty;
            }

            double promedio =
                (p1 + p2 + p3) /
                3.0;

            promedio =
                Math.Truncate(
                    promedio * 10.0) /
                10.0;

            return promedio.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
        }

        // ============================================================
        // INFO ALUMNO
        // ============================================================

        private void InfoAlumnoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext
                is not AlumnoPreItem alumnoPre)
            {
                return;
            }

            if (_mainVm == null)
                _mainVm =
                    FindMainViewModel();

            if (_mainVm == null)
                return;

            var alumno =
                _mainVm.Alumnos.FirstOrDefault(
                    a =>
                        string.Equals(
                            a.Matricula,
                            alumnoPre.Matricula,
                            StringComparison.OrdinalIgnoreCase));

            if (alumno == null)
                return;

            var datosParciales =
                new Dictionary<
                    string,
                    (
                        string calif,
                        string estado,
                        int faltas,
                        int totalClases
                    )>();

            foreach (string eval in
                     new[] { "P1", "P2", "P3", "SEM" })
            {
                string calif =
                    alumno.Calificación[eval];

                datosParciales[eval] =
                    (
                        calif,
                        string.Empty,
                        -1,
                        -1
                    );
            }

            var ventana =
                new InfoAlumnoWindow(
                    alumno,
                    datosParciales)
                {
                    Owner =
                        Window.GetWindow(
                            this)
                };

            ventana.ShowDialog();
        }

        // ============================================================
        // ITEM PRE
        // ============================================================

        public sealed class AlumnoPreItem :
            INotifyPropertyChanged
        {
            private string _p1 = string.Empty;
            private string _p2 = string.Empty;
            private string _p3 = string.Empty;
            private string _promedio = string.Empty;
            private string _pre = string.Empty;
            private string _preAnterior = string.Empty;
            private string _estado = string.Empty;

            public string Matricula { get; }

            public string Nombre { get; }

            public string Grupo { get; }

            public string P1
            {
                get => _p1;

                set
                {
                    if (_p1 == value)
                        return;

                    _p1 =
                        value;

                    OnPropertyChanged(
                        nameof(P1));

                    OnPropertyChanged(
                        nameof(P1Foreground));
                }
            }

            public string P2
            {
                get => _p2;

                set
                {
                    if (_p2 == value)
                        return;

                    _p2 =
                        value;

                    OnPropertyChanged(
                        nameof(P2));

                    OnPropertyChanged(
                        nameof(P2Foreground));
                }
            }

            public string P3
            {
                get => _p3;

                set
                {
                    if (_p3 == value)
                        return;

                    _p3 =
                        value;

                    OnPropertyChanged(
                        nameof(P3));

                    OnPropertyChanged(
                        nameof(P3Foreground));
                }
            }

            public string Promedio
            {
                get => _promedio;

                set
                {
                    if (_promedio == value)
                        return;

                    _promedio =
                        value;

                    OnPropertyChanged(
                        nameof(Promedio));

                    OnPropertyChanged(
                        nameof(PromedioForeground));
                }
            }

            public string Pre
            {
                get => _pre;

                set
                {
                    if (_pre == value)
                        return;

                    _pre =
                        value;

                    OnPropertyChanged(
                        nameof(Pre));

                    OnPropertyChanged(
                        nameof(PreForeground));
                }
            }

            public string PreAnterior
            {
                get => _preAnterior;

                set =>
                    _preAnterior =
                        value;
            }

            public string Estado
            {
                get => _estado;

                set
                {
                    if (_estado == value)
                        return;

                    _estado =
                        value;

                    OnPropertyChanged(
                        nameof(Estado));

                    OnPropertyChanged(
                        nameof(EstadoForeground));
                }
            }

            public Brush P1Foreground =>
                ObtenerColorCalificacion(
                    P1);

            public Brush P2Foreground =>
                ObtenerColorCalificacion(
                    P2);

            public Brush P3Foreground =>
                ObtenerColorCalificacion(
                    P3);

            public Brush PromedioForeground =>
                ObtenerColorCalificacion(
                    Promedio);

            public Brush PreForeground
            {
                get
                {
                    return Pre switch
                    {
                        "6" =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    22,
                                    163,
                                    74)),

                        "5" or
                        "4" or
                        "3" or
                        "2" or
                        "1" or
                        "0" =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    220,
                                    38,
                                    38)),

                        _ =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    15,
                                    23,
                                    42))
                    };
                }
            }

            public Brush EstadoForeground
            {
                get
                {
                    return Estado switch
                    {
                        "PA" =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    22,
                                    163,
                                    74)),

                        "PR" =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    220,
                                    38,
                                    38)),

                        _ =>
                            new SolidColorBrush(
                                Color.FromRgb(
                                    100,
                                    116,
                                    139))
                    };
                }
            }

            private static Brush ObtenerColorCalificacion(
                string? valor)
            {
                if (!double.TryParse(
                        valor?.Replace(
                            ',',
                            '.'),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double numero))
                {
                    return new SolidColorBrush(
                        Color.FromRgb(
                            30,
                            41,
                            59));
                }

                return numero < 6
                    ? new SolidColorBrush(
                        Color.FromRgb(
                            220,
                            38,
                            38))
                    : new SolidColorBrush(
                        Color.FromRgb(
                            22,
                            163,
                            74));
            }

            public AlumnoPreItem(
                string matricula,
                string nombre,
                string grupo,
                string p1,
                string p2,
                string p3,
                string promedio,
                string pre,
                string estado)
            {
                Matricula =
                    matricula ??
                    string.Empty;

                Nombre =
                    nombre ??
                    string.Empty;

                Grupo =
                    grupo ??
                    string.Empty;

                _p1 =
                    p1 ??
                    string.Empty;

                _p2 =
                    p2 ??
                    string.Empty;

                _p3 =
                    p3 ??
                    string.Empty;

                _promedio =
                    promedio ??
                    string.Empty;

                _pre =
                    pre ??
                    string.Empty;

                _preAnterior =
                    pre ??
                    string.Empty;

                _estado =
                    estado ??
                    string.Empty;
            }

            public event PropertyChangedEventHandler?
                PropertyChanged;

            private void OnPropertyChanged(
                string propertyName)
            {
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        propertyName));
            }
        }
    }
}