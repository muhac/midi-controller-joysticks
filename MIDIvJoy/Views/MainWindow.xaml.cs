using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using MIDIvJoy.Models.Joysticks;
using MIDIvJoy.ViewModels;

namespace MIDIvJoy.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();
        serviceCollection.AddAntDesign();

        // Models
        serviceCollection.AddSingleton<IJoysticks, JoyManager>();

        // ViewModels
        serviceCollection.AddTransient<JoyStatusViewModel>();
        serviceCollection.AddTransient<JoyWatcherViewModel>();

        Resources.Add("services", serviceCollection.BuildServiceProvider());
    }
}