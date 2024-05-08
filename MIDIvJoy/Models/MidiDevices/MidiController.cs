using System.Collections.Concurrent;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiController : IMidiController
{
    // for binded commands, only exact matches trigger the command
    private readonly ConcurrentDictionary<CommandKey, ConcurrentBag<Command>> _commandsSaved = new();

    // for unbinded commands, all commands of the same key are grouped
    private readonly ConcurrentDictionary<CommandKey, Command> _commandsAvail = new();

    public event EventHandler<MidiEventArgs>? CommandsChanged;

    public MidiController()
    {
        Task.Run(LoadCommands);
    }

    public async Task<bool> LoadCommands()
    {
        Command[] commands = [];

        foreach (var command in commands)
        {
            if (command.Id != string.Empty) AddCommand(command);
        }

        await Task.Delay(1000);
        CommandsChanged?.Invoke(this, new MidiEventArgs(null));
        return true;
    }

    public async Task<bool> SaveCommands()
    {
        return await LoadCommands();
    }

    public Command[] ListCommands() => _commandsAvail.Values
        .Concat(_commandsSaved.Values.SelectMany(bag => bag)).ToArray();

    public bool AddCommand(Command command)
    {
        // the saved commands are identified by id
        if (command.Id != string.Empty)
        {
            if (command.Action.Type == ActionType.None) return false;

            var has = _commandsSaved.TryGetValue(command.Key, out var bag);
            if (!has || bag == null)
            {
                bag = new ConcurrentBag<Command> { command };
                _commandsSaved.TryAdd(command.Key, bag);
            }
            else
            {
                bag.Add(command);
            }
        }
        else
        {
            _commandsAvail[command.Key] = command;
        }

        CommandsChanged?.Invoke(this, new MidiEventArgs(command));
        return true;
    }

    public bool DelCommand(Command command)
    {
        // if id is empty, it's an unbinded command
        if (command.Id == string.Empty)
        {
            return _commandsAvail.TryRemove(command.Key, out _);
        }

        var has = _commandsSaved.TryGetValue(command.Key, out var bag);
        if (!has || bag == null) return false;

        var commands = bag.Where(c => c.Id != command.Id);
        _commandsSaved[command.Key] = new ConcurrentBag<Command>(commands);
        CommandsChanged?.Invoke(this, new MidiEventArgs(command));
        return true;
    }

    public Command? GetAction(Command query)
    {
        var has = _commandsSaved.TryGetValue(query.Key, out var actions);
        if (!has || actions == null || actions.IsEmpty) return null;

        // search for a button press action
        var actionButton = actions
            .Where(a => a.Action.Type == ActionType.Button)
            .FirstOrDefault(a => a.Event.Value == query.Event.Value);
        if (actionButton is not null) return actionButton;

        // search for an axis action
        var actionAxis = actions
            .Where(a => a.Action.Type == ActionType.Axis)
            .FirstOrDefault(a => a.Event.Value <= query.Event.Value && query.Event.Value <= a.Event.ValueRangeHigh);
        if (actionAxis is null) return null;

        double range = actionAxis.Event.ValueMax - actionAxis.Event.ValueMin;
        actionAxis.Action.Axis.Percent = (query.Event.Value - actionAxis.Event.ValueMin) / range;
        return actionAxis;
    }
}