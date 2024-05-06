using System.Collections.Concurrent;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiController : IMidiController
{
    // for binded commands, only exact matches trigger the command
    private readonly ConcurrentDictionary<CommandKeyExact, Command> _commandsSaved = new();

    // for unbinded commands, all commands of the same key are grouped
    private readonly ConcurrentDictionary<CommandKeyFuzzy, Command> _commandsAvail = new();

    public event EventHandler<CommandEventArgs>? CommandsChanged;

    public MidiController()
    {
        LoadCommands();
    }

    public async Task<bool> LoadCommands()
    {
        Command[] commands =
        [
            new Command("SMC-Mixer - 65535@65535", "Note D#8", ControllerType.Button, 0, 0, 127),
        ];

        foreach (var command in commands)
        {
            command.Bind = true;
            _commandsSaved.TryAdd(command.KeyExact, command);
        }

        return true;
    }

    public async Task<bool> SaveCommands()
    {
        await LoadCommands();
        return true;
    }

    public Command[] ListCommands() => _commandsSaved.Values.Concat(_commandsAvail.Values).ToArray();

    public bool AddCommand(Command command)
    {
        if (command.Type == ControllerType.None)
        {
            _commandsAvail.AddOrUpdate(command.KeyFuzzy, command, (_, _) => command);
            CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        }
        else
        {
            _commandsSaved.AddOrUpdate(command.KeyExact, command, (_, _) => command);
            CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        }

        return true;
    }

    public bool DelCommand(Command command)
    {
        if (command.Type == ControllerType.None)
        {
            if (!_commandsAvail.TryRemove(command.KeyFuzzy, out _)) return false;
            CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        }
        else
        {
            if (!_commandsSaved.TryRemove(command.KeyExact, out _)) return false;
            CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        }

        return true;
    }

    public Task<bool> Trigger(Command command)
    {
        // if command is binded, trigger it
        return Task.FromResult(true);
    }

    public Command? GetAction(Command query)
    {
        var q = query.DeepCopy();

        // try to match an axis command
        q.Type = ControllerType.Axis;
        if (_commandsSaved.TryGetValue(q.KeyExact, out var action)) return action;

        // try to match a button command
        q.Type = ControllerType.Button;
        return _commandsSaved.TryGetValue(q.KeyExact, out action) ? action : null;
    }
}