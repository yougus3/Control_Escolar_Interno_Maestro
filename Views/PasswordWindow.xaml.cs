using System.Windows;
using System.Windows.Input;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Views;

public partial class PasswordWindow : Window
{
    private const string PasswordAdmin = "141184DANJAREDGSGSH";

    public PasswordWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => txtPassword.Focus();
    }

    private void Entrar_Click(object sender, RoutedEventArgs e)
    {
        if (txtPassword.Password == PasswordAdmin)
        {
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show(
            "Contraseña incorrecta.",
            "Acceso denegado",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        txtPassword.SelectAll();
        txtPassword.Focus();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Entrar_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            Cancelar_Click(sender, e);
        }
    }
}