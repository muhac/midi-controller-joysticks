using Microsoft.AspNetCore.Components.Web;
using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.MidiDevices;

namespace MIDIvJoy.ViewModels;

public struct MidiDeviceInfo
{
    public string Id;
    public string Name;
}

public class MidiConfigViewModel
{
    public MidiConfigViewModel(IMidiDevices m, IMidiController c)
    {
        _m = m;
        _c = c;

        _m.EventReceived += OnCommandsReceived;
        _c.CommandsChanged += OnCommandsChanged;

        _t = new Timer(Watch, null, TimeSpan.Zero, TimeSpan.FromSeconds(1d / 30));
    }

    private readonly IMidiDevices _m;
    private readonly IMidiController _c;
    private Timer _t;

    public MidiDeviceInfo[] DevicesAvailable { get; private set; } = [];
    public string DeviceId { get; private set; } = "NA";

    private Command[] _commands = [];

    public Command[] CommandsAxis => _commands
        .Where(cmd => cmd.Type == ControllerType.Axis)
        .Where(cmd => cmd.DeviceKey == DeviceId)
        .OrderBy(cmd => cmd.Name)
        .ToArray();

    public Command[] CommandsButton => _commands
        .Where(cmd => cmd.Type == ControllerType.Button)
        .Where(cmd => cmd.DeviceKey == DeviceId)
        .OrderBy(cmd => cmd.Name)
        .ToArray();

    public Command[] CommandsUnbinded => _commands
        .Where(cmd => cmd.Type == ControllerType.None)
        .Where(cmd => cmd.DeviceKey == DeviceId)
        .OrderBy(cmd => cmd.Name)
        .ToArray();

    public bool IsSettingsVisible { get; private set; }
    public bool IsSettingsLoading { get; private set; }
    public Command? CommandReceived { get; private set; }
    public Command? CommandSelected { get; private set; }
    public Command? CommandEditing { get; private set; }

    public void OnInitialized()
    {
        UpdateDevices();
        UpdateCommands();
    }

    public void SwitchId(string id)
    {
        DeviceId = id;
    }

    private void Watch(object? _)
    {
        if (_m.GetMidiDevicesCount() == DevicesAvailable.Length) return;

        UpdateDevices();
        StateHasChanged();
    }

    private void UpdateDevices()
    {
        DevicesAvailable = _m.GetDevices()
            .Select((device) => new MidiDeviceInfo { Id = device.Key, Name = device.Info.ProductName })
            //.OrderBy(device => device.Name)
            .ToArray();

        Console.WriteLine("Available MIDI Devices: " + DevicesAvailable.Length);
        DeviceId = DevicesAvailable.Select(device => device.Id).DefaultIfEmpty("NA").First();
    }

    private void UpdateCommands()
    {
        _commands = _c.ListCommands();
    }

    private void OnCommandsChanged(object? sender, CommandEventArgs e)
    {
        UpdateCommands();
        StateHasChanged();
    }

    private void OnCommandsReceived(object? sender, MidiEventArgs e)
    {
        var action = _c.GetAction(e.Command);

        CommandReceived = action ?? e.Command;
        if (Program.Instance.IsWindowActivated) StateHasChanged();

        if (action is null)
        {
            _c.AddCommand(e.Command);
            return;
        }

        _ = _c.Trigger(action).Result;
    }


    public void SetCommand(Command cmd)
    {
        CommandSelected = cmd;
        CommandEditing = cmd.DeepCopy();
        IsSettingsVisible = true;
    }

    public async Task SetCommandSave(MouseEventArgs e)
    {
        IsSettingsLoading = true;
        StateHasChanged();

        if (
            CommandEditing is not null && CommandSelected is not null &&
            CommandEditing.KeyFuzzy.Equals(CommandSelected.KeyFuzzy)
        )
        {
            _c.DelCommand(CommandSelected);
            _c.AddCommand(CommandEditing);
        }

        IsSettingsVisible = false;
        IsSettingsLoading = false;
        StateHasChanged();
    }

    public void SetCommandCancel(MouseEventArgs e)
    {
        IsSettingsVisible = false;
    }

    public event Action StateHasChanged = delegate { };
}