using System.Collections.Concurrent;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiConfigure : IMidiCommands
{
    private const string DataKey = "commands";

    // for binded commands, only exact matches trigger the command
    private readonly ConcurrentDictionary<CommandKey, ConcurrentBag<Command>> _commandsSaved = new();

    // for unbinded commands, all commands of the same key are grouped
    private readonly ConcurrentDictionary<CommandKey, Command> _commandsAvail = new();

    public event EventHandler<CommandEventArgs>? CommandsChanged;

    public MidiConfigure()
    {
        Task.Run(LoadCommands);
    }

    public async Task<bool> LoadCommands()
    {
        return await Task.FromResult(LoadCommandsSync());
    }

    private bool LoadCommandsSync()
    {
        var commands = Database.Get<CommandSaved[]>(DataKey) ?? [];
        Console.WriteLine($"Loaded Command Records: {commands.Length}");

        _commandsSaved.Clear();
        foreach (var command in commands.Select(Command.FromSaved))
        {
            if (command.Id != string.Empty) AddCommand(command);
        }

        CommandsChanged?.Invoke(this, new CommandEventArgs(null));
        return true;
    }

    public async Task<bool> SaveCommands()
    {
        var commands = ListCommands()
            .Where(c => c.Id != string.Empty)
            .Where(c => c.Action.Type != ActionType.None)
            .Select(c => c.ToSaved());

        Database.Set(DataKey, commands);
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

            _commandsSaved.AddOrUpdate(command.Key, [command], (_, bag) =>
            {
                bag.Add(command);
                return bag;
            });
        }
        else
        {
            _commandsAvail[command.Key] = command;
        }

        CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        return true;
    }

    public bool DelCommand(Command command)
    {
        // if id is empty, it's an unbinded command
        if (command.Id == string.Empty)
        {
            return _commandsAvail.TryRemove(command.Key, out _);
        }

        if (!_commandsSaved.TryGetValue(command.Key, out var bag)) return false;

        var commands = bag.Where(c => c.Id != command.Id);
        _commandsSaved[command.Key] = new ConcurrentBag<Command>(commands);
        CommandsChanged?.Invoke(this, new CommandEventArgs(command));
        return true;
    }

    public Command? GetAction(MidiEvent query)
    {
        var key = new CommandKey(query);
        var has = _commandsSaved.TryGetValue(key, out var actions);
        if (!has || actions == null || actions.IsEmpty) return null;

        // search for an axis action
        var actionAxis = actions
            .Where(a => a.Action.Type == ActionType.Axis)
            .FirstOrDefault(a => a.Event.Value <= query.Value && query.Value <= a.Event.ValueRangeHigh);
        if (actionAxis is not null)
        {
            double value = query.Value - actionAxis.Event.ValueMin;
            double range = actionAxis.Event.ValueMax - actionAxis.Event.ValueMin;
            actionAxis.Action.Axis.Percent = double.Max(0, double.Min(100, value / range));
            return actionAxis;
        }

        // search for a button press action
        var actionButton = actions
            .Where(a => a.Action.Type == ActionType.Button)
            .FirstOrDefault(a => a.Event.Value <= query.Value && query.Value <= a.Event.ValueRangeHigh);
        if (actionButton is null) return null;

        actionButton.Action.Button.On = actionButton.Action.Button.Type == ActionTypeButton.Press;
        if (actionButton.Action.Button.Type == ActionTypeButton.Auto)
        {
            actionButton.Action.Button.On = query.Value > actionButton.Event.ValueMin;
        }

        return actionButton;
    }
}