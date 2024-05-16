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
    public MidiConfigViewModel(IMidiDevices m, IMidiCommands c, IMidiTower t)
    {
        _m = m;
        _c = c;

        _c.CommandsChanged += OnCommandsChanged;
        t.ActionReceived += OnActionReceived;

        _t = new Timer(Watch, null, TimeSpan.Zero, TimeSpan.FromSeconds(1d / 30));
    }

    private readonly IMidiDevices _m;
    private readonly IMidiCommands _c;
    private Timer _t;

    public MidiDeviceInfo[] DevicesAvailable { get; private set; } = [];
    public string DeviceId { get; private set; } = "NA";

    private Command[] _commands = [];

    public Command[] CommandsAxis => Commands(ActionType.Axis);
    public Command[] CommandsButton => Commands(ActionType.Button);
    public Command[] CommandsUnbinded => Commands(ActionType.None);

    private Command[] Commands(ActionType type) => _commands
        .Where(cmd => cmd.Action.Type == type)
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
        var prevDevices = DevicesAvailable.Select(device => device.Id);
        var currDevices = _m.GetDevices().Select(device => device.Key);
        if (currDevices.SequenceEqual(prevDevices)) return;

        UpdateDevices();
        StateHasChanged();
    }

    private void UpdateDevices()
    {
        DevicesAvailable = _m.GetDevices()
            .Select(device => new MidiDeviceInfo
            {
                Id = device.Key,
                Name = device.Info.ProductName,
            })
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
        if (Program.Instance.IsWindowActivated) StateHasChanged();
    }

    private void OnActionReceived(object? sender, CommandEventArgs e)
    {
        if (e.Command is null) return;
        CommandReceived = e.Command;
    }

    public void EditBinding(Command cmd)
    {
        CommandSelected = cmd;
        CommandEditing = (Command)cmd.Clone();
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