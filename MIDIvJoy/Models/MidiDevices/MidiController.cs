using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiController : IMidiController
{
    private const string CommandsSavedFile = "commands.json";

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
        var json = await File.ReadAllTextAsync(CommandsSavedFile);
        var commands = JsonConvert.DeserializeObject<CommandSaved[]>(json) ?? [];
        Console.WriteLine($"Commands Loaded from File: {commands.Length}");

        _commandsSaved.Clear();
        foreach (var command in commands.Select(Command.FromSaved))
        {
            if (command.Id != string.Empty) AddCommand(command);
        }

        CommandsChanged?.Invoke(this, new MidiEventArgs(null));
        return true;
    }

    public async Task<bool> SaveCommands()
    {
        var commands = ListCommands()
            .Where(c => c.Id != string.Empty)
            .Where(c => c.Action.Type != ActionType.None)
            .Select(c => c.ToSaved());
        var json = JsonConvert.SerializeObject(commands);
        await File.WriteAllTextAsync(CommandsSavedFile, json);
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

        // search for an axis action
        var actionAxis = actions
            .Where(a => a.Action.Type == ActionType.Axis)
            .FirstOrDefault(a => a.Event.Value <= query.Event.Value && query.Event.Value <= a.Event.ValueRangeHigh);
        if (actionAxis is not null)
        {
            double value = query.Event.Value - actionAxis.Event.ValueMin;
            double range = actionAxis.Event.ValueMax - actionAxis.Event.ValueMin;
            actionAxis.Action.Axis.Percent = double.Max(0, double.Min(100, value / range));
            return actionAxis;
        }

        // search for a button press action
        var actionButton = actions
            .Where(a => a.Action.Type == ActionType.Button)
            .FirstOrDefault(a => a.Event.Value <= query.Event.Value && query.Event.Value <= a.Event.ValueRangeHigh);
        if (actionButton is null) return null;

        actionButton.Action.Button.On = actionButton.Action.Button.Type == ActionTypeButton.Press;
        if (actionButton.Action.Button.Type == ActionTypeButton.Auto)
        {
            actionButton.Action.Button.On = query.Event.Value > actionButton.Event.ValueMin;
        }

        return actionButton;
    }
}