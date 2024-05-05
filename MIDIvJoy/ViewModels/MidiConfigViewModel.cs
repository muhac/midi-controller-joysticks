using NAudio.Midi;

namespace MIDIvJoy.ViewModels;

public struct MidiDevice
{
    public int Id;
    public string Name;
    internal MidiIn? Handler { get; init; }
}

public class MidiConfigViewModel
{
    private readonly ReaderWriterLockSlim _devicesLock = new();
    public MidiDevice[] DevicesAvailable { get; private set; } = [];
    public int DeviceId { get; private set; }

    private Timer? _timer;

    public void OnInitialized()
    {
        UpdateDevices();

        _timer?.Dispose(); // OnInitialized is called every time the page is loaded
        _timer = new Timer(Watch, null, TimeSpan.Zero, TimeSpan.FromSeconds(1d / 30));
    }

    public void SwitchId(int id)
    {
        DeviceId = id;
    }

    private void Watch(object? _)
    {
        if (MidiIn.NumberOfDevices == DevicesAvailable.Length) return;

        UpdateDevices();
        StateHasChanged();
    }

    private static MidiIn? Listen(int id)
    {
        try
        {
            var handler = new MidiIn(id);
            handler.MessageReceived += MessageReceived(id);
            handler.ErrorReceived += ErrorReceived(id);
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
            Console.WriteLine($"{device.Name}: {e.Message}");
        }
    }

    private void UpdateDevices()
    {
        _devicesLock.EnterWriteLock();

        DevicesAvailable.ToList().ForEach(Dispose);
        DevicesAvailable = Enumerable.Range(0, MidiIn.NumberOfDevices)
            .Select(id => new MidiDevice
            {
                Id = id,
                Name = MidiIn.DeviceInfo(id).ProductName,
                Handler = Listen(id),
            })
            .ToArray();

        _devicesLock.ExitWriteLock();

        Console.WriteLine("Available MIDI Devices: " + DevicesAvailable.Length);
        DeviceId = DevicesAvailable.Select(device => device.Id).DefaultIfEmpty(0).First();
    }

    private static EventHandler<MidiInMessageEventArgs> MessageReceived(int id)
    {
        return (_, e) =>
        {
            switch (e.MidiEvent)
            {
                case ControlChangeEvent evt:
                    Console.WriteLine($"Control Change {evt.Controller} {evt.ControllerValue}");
                    break;
                case NoteEvent evt:
                    Console.WriteLine($"Note {evt.NoteName} {evt.NoteNumber} {evt.Velocity}");
                    break;
                case PitchWheelChangeEvent evt:
                    Console.WriteLine($"Pitch Wheel {evt.Channel} {evt.Pitch}");
                    break;
                default:
                    // other types of MIDI events unsupported yet
                    Console.WriteLine($"Device {id} MIDI Message {e.MidiEvent.GetType()} Event {e.MidiEvent}");
                    break;
            }
        };
    }

    private static EventHandler<MidiInMessageEventArgs> ErrorReceived(int id)
    {
        return (_, e) =>
        {
            Console.WriteLine($"Device {id} MIDI Error Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
        };
    }

    public event Action StateHasChanged = delegate { };
}