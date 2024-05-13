using Microsoft.AspNetCore.Components.Web;
using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;
using MIDIvJoy.Models.MidiDevices;

namespace MIDIvJoy.ViewModels;

public struct MidiDeviceInfo
{
    public string Id;
    public string Name;
}

public class MidiConfigViewModel
{
    public MidiConfigViewModel(IMidiDevices m, IMidiController c, IJoysticks j)
    {
        _m = m;
        _c = c;
        _j = j;

        _m.EventReceived += OnCommandsReceived;
        _c.CommandsChanged += OnCommandsChanged;

        _t = new Timer(Watch, null, TimeSpan.Zero, TimeSpan.FromSeconds(1d / 30));
    }

    private readonly IMidiDevices _m;
    private readonly IMidiController _c;
    private readonly IJoysticks _j;
    private Timer _t;

    public MidiDeviceInfo[] DevicesAvailable { get; private set; } = [];
    public string DeviceId { get; private set; } = "NA";

    private Command[] _commands = [];

    public Command[] CommandsAxis => _commands
        .Where(cmd => cmd.Action.Type == ActionType.Axis)
        .Where(cmd => cmd.Event.Device == DeviceId)
        .OrderBy(cmd => cmd.Name)
        .ToArray();

    public Command[] CommandsButton => _commands
        .Where(cmd => cmd.Action.Type == ActionType.Button)
        .Where(cmd => cmd.Event.Device == DeviceId)
        .OrderBy(cmd => cmd.Name)
        .ToArray();

    public Command[] CommandsUnbinded => _commands
        .Where(cmd => cmd.Action.Type == ActionType.None)
        .Where(cmd => cmd.Event.Device == DeviceId)
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

    private void OnCommandsChanged(object? sender, MidiEventArgs e)
    {
        UpdateCommands();
        StateHasChanged();
    }

    private void OnCommandsReceived(object? sender, MidiEventArgs e)
    {
        if (e.Command is null) return;
        var action = _c.GetAction(e.Command);

        CommandReceived = action ?? e.Command;
        if (Program.Instance.IsWindowActivated) StateHasChanged();

        if (action is null)
        {
            _c.AddCommand(e.Command);
            return;
        }

        var joystick = _j.GetJoystick(action.Action.DeviceId);
        if (joystick is null) return;

        var result = action.Action.Type switch
        {
            ActionType.Axis => joystick.GetFeeder().Set(action.Action.Axis).Result,
            ActionType.Button => joystick.GetFeeder().Set(action.Action.Button).Result,
            _ => false,
        };
    }

    public void EditBinding(Command cmd)
    {
        CommandSelected = cmd;
        CommandEditing = cmd.DeepCopy();
        if (CommandEditing.Id == string.Empty)
        {
            // edit a new command
            CommandEditing.Id = Guid.NewGuid().ToString().ToUpper();
        }

        IsSettingsVisible = true;
    }

    public async Task EditBindingSave(MouseEventArgs e)
    {
        IsSettingsLoading = true;
        StateHasChanged();

        if (
            CommandEditing is not null && CommandSelected is not null &&
            CommandEditing.Key.Equals(CommandSelected.Key)
        )
        {
            _c.DelCommand(CommandSelected);
            _c.AddCommand(CommandEditing);
        }

        await _c.SaveCommands();

        IsSettingsVisible = false;
        IsSettingsLoading = false;
    }

    public void EditBindingCancel(MouseEventArgs e)
    {
        IsSettingsVisible = false;
    }

    public event Action StateHasChanged = delegate { };
}