namespace MIDIvJoy.ViewModels;

using System.ComponentModel;
using System.Windows.Input;
using Services;
using Utilities;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private uint _vJoyId = 1;

    public uint VJoyId
    {
        get => _vJoyId;
        set
        {
            if (_vJoyId == value) return;
            _vJoyId = value;
            OnPropertyChanged(nameof(VJoyId));
            Console.WriteLine($"Set vJoy ID: {value}");
        }
    }

    public ICommand GreetCommand { get; }

    public MainWindowViewModel()
    {
        GreetCommand = new RelayCommand(ActivateFeeder, _ => true);
    }

    private void ActivateFeeder(object? parameter)
    {
        var joyFeeder = new JoyFeeder();
        Task.Run(() => JoyFeeder.DemoRun(VJoyId));

        var message = $"Activated vJoy {VJoyId}!";
        Console.WriteLine(message);
        // System.Windows.MessageBox.Show(message);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}