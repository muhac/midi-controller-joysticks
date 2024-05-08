using NAudio.Midi;

namespace MIDIvJoy.Models.DataModels;

public struct MidiDevice
{
    public string Key;
    public MidiInCapabilities Info;
    public MidiIn? Handler { get; init; }
}

public class MidiEventArgs(Command? command) : EventArgs
{
    public readonly Command? Command = command;
}

public class MidiEvent(string device, string command, int value, int valueMin, int valueMax)
{
    public string Device { get; init; } = device;
    public string Command { get; init; } = command;
    public int Value { get; set; } = value;
    public int ValueRangeHigh { get; set; } = value;
    public int ValueMin { get; set; } = valueMin;
    public int ValueMax { get; set; } = valueMax;
}

public class Command(MidiEvent e)
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = e.Command;

    public MidiEvent Event { get; set; } = e;
    public JoystickAction Action { get; set; } = new();

    public CommandKey Key => new(this);

    public Command DeepCopy()
    {
        var evt = new MidiEvent(
            Event.Device,
            Event.Command,
            Event.Value,
            Event.ValueMin,
            Event.ValueMax
        )
        {
            ValueRangeHigh = Event.ValueRangeHigh
        };

        var act = new JoystickAction
        {
            DeviceId = Action.DeviceId,
            Type = Action.Type,
            Axis = Action.Axis,
            Button = Action.Button,
            Value = Action.Value
        };

        var cmd = new Command(evt)
        {
            Id = Id,
            Name = Name,
            Action = act
        };

        return cmd;
    }
}

public struct CommandKey(Command command)
{
    public string Device = command.Event.Device;
    public string Event = command.Event.Command;
}