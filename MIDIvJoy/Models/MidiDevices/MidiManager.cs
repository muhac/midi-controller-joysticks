using NAudio.Midi;
using MIDIvJoy.Models.DataModels;
using MidiEvent = MIDIvJoy.Models.DataModels.MidiEvent;

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
        _devices = new MidiDevice[MidiIn.NumberOfDevices];

        Parallel.For(0, MidiIn.NumberOfDevices, id =>
        {
            var info = MidiIn.DeviceInfo(id);
            var key = $"{info.ProductName} - {info.ProductId}@{info.Manufacturer}";
            _devices[id] = new MidiDevice
            {
                Key = key,
                Info = MidiIn.DeviceInfo(id),
                Handler = Listen(id, key),
            };
        });

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
            /*
            Console.WriteLine(DateTime.Now + e.MidiEvent switch
            {
                ControlChangeEvent evt => $"{evt.Channel} Control Change {evt.Controller} {evt.ControllerValue}",
                NoteEvent evt => $"{evt.Channel} Note {evt.NoteName} {evt.NoteNumber} {evt.Velocity}",
                PitchWheelChangeEvent evt => $"{evt.Channel} Pitch Wheel {evt.Pitch} / 16384",
                _ => $"Device {key} MIDI Message {e.MidiEvent.GetType()} Event {e.MidiEvent}"
            });
            */

            var midiEvent = e.MidiEvent switch
            {
                ControlChangeEvent evt => new MidiEvent(
                    key,
                    $"Ctrl {evt.Controller}",
                    evt.ControllerValue,
                    0, 127
                ),

                NoteEvent evt => new MidiEvent(
                    key,
                    $"Note {evt.NoteName}",
                    evt.Velocity,
                    0, 127
                ),

                PitchWheelChangeEvent evt => new MidiEvent(
                    key,
                    $"Pitch {evt.Channel}",
                    evt.Pitch,
                    0, 16384
                ),

                _ => null,
            };

            if (midiEvent == null) return;
            EventReceived?.Invoke(this, new MidiEventArgs(midiEvent));
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