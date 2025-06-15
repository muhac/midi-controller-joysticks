using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;
using MIDIvJoy.Models.MidiDevices;
using MIDIvJoy.ViewModels;
using System.ComponentModel;

namespace MIDIvJoy.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private NotifyIcon? _notifyIcon;
    private bool _isExiting;

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

        // Initialize system tray
        InitializeNotifyIcon();

        // Handle window closing event
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
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

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new NotifyIcon();

        var iconLoaded = false;
        try
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
            {
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                iconLoaded = true;
            }
        }
        catch
        {
            /* Ignore */
        }

        // Fallback: Use system default icon
        if (!iconLoaded)
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }

        _notifyIcon.Text = "MIDIvJoy - MIDI Controller";
        _notifyIcon.Visible = true;

        // Double-click to restore window
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

        // Create context menu with English text
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, ShowWindow_Click);
        contextMenu.Items.Add("Hide", null, HideWindow_Click);
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("Exit", null, ExitApplication_Click);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized) return;
        Hide();
        _notifyIcon?.ShowBalloonTip(1500, "MIDIvJoy",
            "Application minimized to system tray", ToolTipIcon.Info);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExiting) return;
        e.Cancel = true;
        Hide();
        _notifyIcon?.ShowBalloonTip(1500, "MIDIvJoy",
            "Application minimized to system tray. Double-click tray icon to restore window.", ToolTipIcon.Info);
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    private void ShowWindow_Click(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    private void HideWindow_Click(object? sender, EventArgs e)
    {
        Hide();
    }

    private void ExitApplication_Click(object? sender, EventArgs e)
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }
}
