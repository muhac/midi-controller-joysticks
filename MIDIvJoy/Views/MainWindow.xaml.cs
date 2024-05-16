using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;
using MIDIvJoy.Models.MidiDevices;
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
        serviceCollection.AddSingleton<IMidiDevices, MidiManager>();
        serviceCollection.AddSingleton<IMidiCommands, MidiConfigure>();
        serviceCollection.AddSingleton<IMidiTower, MidiTower>();

        // ViewModels
        serviceCollection.AddSingleton<JoyStatusViewModel>();
        serviceCollection.AddSingleton<JoyWatcherViewModel>();
        serviceCollection.AddSingleton<MidiConfigViewModel>();

        Resources.Add("services", serviceCollection.BuildServiceProvider());

        Activated += WindowActivated;
        Deactivated += WindowDeactivated;
    }

    private static void WindowActivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window Activated");
        Program.Instance.IsWindowActivated = true;
    }

    private static void WindowDeactivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window Deactivated");
        Program.Instance.IsWindowActivated = false;
    }
}