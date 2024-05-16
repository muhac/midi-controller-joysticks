using NAudio.Midi;

namespace MIDIvJoy.Models.DataModels;

public struct MidiDevice
{
    public string Key;
    public MidiInCapabilities Info;
    public MidiIn? Handler { get; init; }
}

public class MidiEventArgs(MidiEvent e) : EventArgs
{
    public readonly MidiEvent Event = e;
}

public class CommandEventArgs(Command? command) : EventArgs
{
    public readonly Command? Command = command;
}

public class MidiEvent(string device, string command, int value, int valueMin, int valueMax)
{
    public string Device { get; } = device;
    public string Command { get; } = command;
    public int Value { get; set; } = value;
    public int ValueRangeHigh { get; set; } = valueMax;
    public int ValueMin { get; set; } = valueMin;
    public int ValueMax { get; set; } = valueMax;
}

public class Command(MidiEvent e) : ICloneable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = e.Command;

    public MidiEvent Event { get; set; } = e;
    public JoystickAction Action { get; set; } = new();

    public CommandKey Key => new(Event);

    public object Clone()
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
        };

        var cmd = new Command(evt)
        {
            Id = Id,
            Name = Name,
            Action = act
        };

        return cmd;
    }

    public CommandSaved ToSaved()
    {
        return new CommandSaved(this);
    }

    public static Command FromSaved(CommandSaved c)
    {
        var evt = new MidiEvent(
            c.Midi,
            c.Event,
            c.TriggerLow,
            c.ValueMin,
            c.ValueMax
        )
        {
            ValueRangeHigh = c.TriggerHigh
        };

        var act = new JoystickAction
        {
            DeviceId = c.Joystick,
            Type = c.Action,
            Axis = new JoystickActionAxis(c.Axis)
            {
                Type = c.AxisType
            },
            Button = new JoystickActionButton(c.Button)
            {
                Type = c.ButtonType
            }
        };

        var cmd = new Command(evt)
        {
            Id = c.Id,
            Name = c.Name,
            Action = act
        };

        return cmd;
    }
}

public struct CommandKey(MidiEvent e)
{
    public string Device = e.Device;
    public string Event = e.Command;
}

public struct CommandSaved(Command c)
{
    public string Id { get; set; } = c.Id;
    public string Name { get; set; } = c.Name;

    public string Midi { get; set; } = c.Event.Device;
    public string Event { get; set; } = c.Event.Command;
    public int TriggerLow { get; set; } = c.Event.Value;
    public int TriggerHigh { get; set; } = c.Event.ValueRangeHigh;
    public int ValueMin { get; set; } = c.Event.ValueMin;
    public int ValueMax { get; set; } = c.Event.ValueMax;

    public int Joystick { get; set; } = c.Action.DeviceId;
    public ActionType Action { get; set; } = c.Action.Type;
    public JoystickAxis Axis { get; set; } = c.Action.Axis.Name;
    public ActionTypeAxis AxisType { get; set; } = c.Action.Axis.Type;
    public int Button { get; set; } = c.Action.Button.Number;
    public ActionTypeButton ButtonType { get; set; } = c.Action.Button.Type;
}