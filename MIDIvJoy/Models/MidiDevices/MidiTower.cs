using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiTower : IMidiTower
{
    public MidiTower(IMidiDevices m, IMidiCommands c, IJoysticks j)
    {
        _c = c;
        _j = j;
        m.EventReceived += OnMidiEventReceived;
    }

    private readonly IMidiCommands _c;
    private readonly IJoysticks _j;

    private void OnMidiEventReceived(object? sender, MidiEventArgs e)
    {
        var action = _c.GetAction(e.Event);

        var command = action ?? new Command(e.Event);
        ActionReceived?.Invoke(this, new CommandEventArgs(command));

        if (action is null)
        {
            // add a new command to unassigned
            _c.AddCommand(new Command(e.Event));
            return;
        }

        var joystick = _j.GetJoystick(action.Action.DeviceId);
        if (joystick is null) return;

        _ = action.Action.Type switch
        {
            ActionType.Axis => joystick.GetFeeder().Set(action.Action.Axis).Result,
            ActionType.Button => joystick.GetFeeder().Set(action.Action.Button).Result,
            _ => false,
        };
    }

    public event EventHandler<CommandEventArgs>? ActionReceived;
}