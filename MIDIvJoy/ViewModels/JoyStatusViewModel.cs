using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;

namespace MIDIvJoy.ViewModels;

public enum BarType
{
    Info,
    Warning,
    Error,
}

public class JoyStatusViewModel
{
    public JoyStatusViewModel(IJoysticks m)
    {
        _m = m;
        (_, VersionDll, VersionDrv) = m.GetVersions();
        Enumerable.Range(1, m.GetJoystickCount())
            .ToList()
            .ForEach(id => m.GetJoystick(id).StatusChanged += OnStatusChanged);
    }

    private readonly IJoysticks _m;

    public uint VersionDll { get; private set; }
    public uint VersionDrv { get; private set; }

    public JoystickStatus[] Status { get; private set; } = [];
    public BarType Bar { get; private set; }

    public void OnInitialized()
    {
        Status = new JoystickStatus[_m.GetJoystickCount()];
        UpdateStatus();
    }

    private void OnStatusChanged(object? sender, JoystickStatusEventArgs e)
    {
        UpdateStatus();
        StateHasChanged();
    }

    private void UpdateStatus()
    {
        var totalCount = _m.GetJoystickCount();
        var status = Enumerable.Range(1, totalCount)
            .Select(id => _m.GetJoystick(id).GetStatus())
            .ToArray();

        var unknownCount = status.Count(s => s == JoystickStatus.Unknown);
        Bar = (unknownCount == totalCount, VersionDll != VersionDrv) switch
        {
            (true, _) => BarType.Error,
            (_, true) => BarType.Warning,
            _ => BarType.Info,
        };

        // display 8 blocks instead of 16 if there are more than 8 unknown joysticks.
        unknownCount = status[8..].Count(s => s == JoystickStatus.Unknown);
        Status = unknownCount < 8 ? status : status[..8];
    }

    public void ToggleActive(int id)
    {
        var joystick = _m.GetJoystick(id);
        _ = joystick.GetStatus() switch
        {
            JoystickStatus.Free => joystick.GetFeeder().Acquire(),
            JoystickStatus.Engaged => joystick.GetFeeder().Release(),
            _ => Task.CompletedTask
        };
    }

    // Add a method to trigger a state change in the UI
    public event Action StateHasChanged = delegate { };
}