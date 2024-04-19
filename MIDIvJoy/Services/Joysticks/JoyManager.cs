using System.Windows;

namespace MIDIvJoy.Services.Joysticks;

using vJoyInterfaceWrap;

public class JoyManager
{
    private readonly vJoy _vJoy = new();
    private readonly object _vJoyLock = new();

    private readonly JoyFeeder[] _joysticks = new JoyFeeder[16];

    public uint VersionDll { get; private init; }
    public uint VersionDrv { get; private init; }

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

        uint dllV = 0, drvV = 0;
        if (_vJoy.DriverMatch(ref dllV, ref drvV))
        {
            Console.WriteLine("Version of Driver Matches DLL Version ({0:X})", dllV);
        }
        else
        {
            Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", drvV, dllV);
        }


        // Test if DLL matches the driver
        uint dll = 0, drv = 0;
        _vJoy.DriverMatch(ref dll, ref drv);
        Console.WriteLine($"vJoy DLL: {dll}, Driver: {drv}");
        VersionDll = dll;
        VersionDrv = drv;

        for (uint i = 0; i < _joysticks.Length; i++)
        {
            var feeder = new JoyFeeder(ref _vJoyLock, ref _vJoy, i + 1);
            _joysticks[i] = feeder;

            var random = new Random((int)i);
            Task.Run(async () =>
            {
                while (true)
                {
                    var status = feeder.UpdateStatus();
                    var time = .5 + random.NextDouble();
                    if (status == JoyStatus.Occupied) time += 1;
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