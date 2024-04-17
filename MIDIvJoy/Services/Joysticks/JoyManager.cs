using System.Windows;

namespace MIDIvJoy.Services.Joysticks;

using vJoyInterfaceWrap;

public class JoyManager
{
    private readonly vJoy _vJoy = new();
    private readonly object _vJoyLock = new();

    private readonly JoyFeeder[] _joysticks = new JoyFeeder[16];

    // Singleton instance
    private static readonly JoyManager Instance = new();

    public static JoyManager GetInstance()
    {
        return Instance;
    }

    private JoyManager()
    {
        if (!_vJoy.vJoyEnabled())
        {
            MessageBox.Show("Fatal: vJoy not enabled!");
            Application.Current.Shutdown();
        }

        // Test if DLL matches the driver
        uint dllV = 0, drvV = 0;
        if (_vJoy.DriverMatch(ref dllV, ref drvV))
        {
            Console.WriteLine("Version of Driver Matches DLL Version ({0:X})", dllV);
        }
        else
        {
            Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", drvV, dllV);
        }

        for (uint i = 0; i < _joysticks.Length; i++)
        {
            var id = i + 1;
            var feeder = new JoyFeeder(ref _vJoyLock, ref _vJoy, id);
            _joysticks[i] = feeder;

            Task.Run(async () =>
            {
                var random = new Random((int)id);
                while (true)
                {
                    var status = feeder.UpdateStatus();
                    var time = .5 + random.NextDouble();
                    if (status == JoyStatus.Occupied) time += 2;
                    if (status == JoyStatus.Unknown) time *= 30;
                    await Task.Delay(TimeSpan.FromSeconds(time));
                }
                // ReSharper disable once FunctionNeverReturns
            });
        }
    }

    public JoyFeeder? GetJoystick(uint id)
    {
        return id is >= 1 and <= 16 ? _joysticks[id - 1] : null;
    }

    public void SubscribeToJoystickStatusChanges(uint id, JoyFeeder.StatusChangedHandler handler)
    {
        var joystick = GetJoystick(id);
        if (joystick != null) joystick.StatusChanged += handler;
    }
}