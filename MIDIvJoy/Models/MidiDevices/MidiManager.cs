using NAudio.Midi;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.MidiDevices;

public class MidiManager : IMidiDevices
{
    private readonly ReaderWriterLockSlim _devicesLock = new();
    private MidiDevice[] _devices = [];

    public MidiManager()
    {
        UpdateDevices();
    }

    public int GetMidiDevicesCount() => MidiIn.NumberOfDevices;

    public IEnumerable<MidiDevice> GetDevices()
    {
        if (_devices.Length != GetMidiDevicesCount()) UpdateDevices();
        return _devices;
    }

    private void UpdateDevices()
    {
        _devicesLock.EnterWriteLock();

        _devices.ToList().ForEach(Dispose);
        _devices = Enumerable.Range(0, MidiIn.NumberOfDevices)
            .Select(id =>
            {
                var info = MidiIn.DeviceInfo(id);
                var key = $"{info.ProductName} - {info.ProductId}@{info.Manufacturer}";
                return new MidiDevice
                {
                    Key = key,
                    Info = MidiIn.DeviceInfo(id),
                    Handler = Listen(id, key),
                };
            })
            .ToArray();

        _devicesLock.ExitWriteLock();
    }

    private MidiIn? Listen(int id, string key)
    {
        try
        {
            var handler = new MidiIn(id);
            handler.MessageReceived += MessageReceived(key);
            handler.ErrorReceived += ErrorReceived(key);
            handler.Start();
            return handler;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{id}: {e.Message}");
            return null;
        }
    }

    private static void Dispose(MidiDevice device)
    {
        try
        {
            device.Handler?.Stop();
            device.Handler?.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{device.Key}: {e.Message}");
        }
    }

    private EventHandler<MidiInMessageEventArgs> MessageReceived(string key)
    {
        return (_, e) =>
        {
            var info = e.MidiEvent switch
            {
                ControlChangeEvent evt => $"{evt.Channel} Control Change {evt.Controller} {evt.ControllerValue}",
                NoteEvent evt => $"{evt.Channel} Note {evt.NoteName} {evt.NoteNumber} {evt.Velocity}",
                PitchWheelChangeEvent evt => $"{evt.Channel} Pitch Wheel {evt.Pitch} / 16384",
                _ => $"Device {key} MIDI Message {e.MidiEvent.GetType()} Event {e.MidiEvent}"
            };

            Console.WriteLine(info);

            var command = e.MidiEvent switch
            {
                ControlChangeEvent evt => new Command(
                    key,
                    $"Ctrl {evt.Controller}",
                    ControllerType.None,
                    evt.ControllerValue,
                    0, 127
                ),

                NoteEvent evt => new Command(
                    key,
                    $"Note {evt.NoteName}",
                    ControllerType.None,
                    evt.Velocity,
                    0, 127
                ),

                PitchWheelChangeEvent evt => new Command(
                    key,
                    $"Pitch {evt.Channel}",
                    ControllerType.None,
                    evt.Pitch,
                    0, 16384
                ),

                _ => null,
            };

            if (command == null) return;
            EventReceived?.Invoke(this, new MidiEventArgs(command));
        };
    }

    private EventHandler<MidiInMessageEventArgs> ErrorReceived(string key)
    {
        return (_, e) =>
        {
            Console.WriteLine($"Device {key} MIDI Error Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
        };
    }

    public event EventHandler<MidiEventArgs>? EventReceived;
}