namespace MIDIvJoy.Views;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using ViewModels;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumericalGeneratedRegex();

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = NumericalGeneratedRegex().IsMatch(e.Text);
    }
}