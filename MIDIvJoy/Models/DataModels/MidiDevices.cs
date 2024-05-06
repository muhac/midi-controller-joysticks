using NAudio.Midi;

namespace MIDIvJoy.Models.DataModels;

public struct MidiDevice
{
    public string Key;
    public MidiInCapabilities Info;
    public MidiIn? Handler { get; init; }
}

public class MidiEventArgs(Command command) : EventArgs
{
    public Command Command = command;
}

public class CommandEventArgs(Command command) : EventArgs
{
    public Command Command = command;
}

public class Command(string device, string command, ControllerType type, int value, int valueMin, int valueMax)
{
    public string DeviceKey { get; init; } = device;
    public string CommandKey { get; init; } = command;

    public string Name { get; set; } = command;
    public ControllerType Type { get; set; } = type;

    public int Value { get; set; } = value;
    public int ValueHigh { get; set; } = value;
    public int ValueMin { get; set; } = valueMin;
    public int ValueMax { get; set; } = valueMax;

    public bool Bind { get; set; }

    public CommandKeyExact KeyExact => new(this);
    public CommandKeyFuzzy KeyFuzzy => new(this);

    public Command DeepCopy()
    {
        var cmd = new Command(
            DeviceKey,
            CommandKey,
            Type,
            Value,
            ValueMin,
            ValueMax
        )
        {
            Name = Name,
            ValueHigh = ValueHigh,
            Bind = Bind
        };

        return cmd;
    }
}

public struct CommandKeyFuzzy(Command command)
{
    public string Device = command.DeviceKey;
    public string Command = command.CommandKey;
}

public struct CommandKeyExact(Command command)
{
    public string Device = command.DeviceKey;
    public string Command = command.CommandKey;

    public string Value = command.Type switch
    {
        ControllerType.Axis => "Axis",
        ControllerType.Button => "Btn" + command.Value.ToString(),
        _ => string.Empty,
    };
}