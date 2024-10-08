﻿using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public interface IMidiDevices
{
    public int GetMidiDevicesCount();
    public IEnumerable<MidiDevice> GetDevices();
    event EventHandler<MidiEventArgs> EventReceived;
}

public interface IMidiCommands
{
    public Task<bool> LoadCommands();
    public Task<bool> SaveCommands();
    public Command[] ListCommands();

    public bool AddCommand(Command command);
    public bool DelCommand(Command command);
    event EventHandler<CommandEventArgs> CommandsChanged;

    public Command? GetAction(MidiEvent query);
}

public interface IMidiTower
{
    event EventHandler<CommandEventArgs> ActionReceived;
}