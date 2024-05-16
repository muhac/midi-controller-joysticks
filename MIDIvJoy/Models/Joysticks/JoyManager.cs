using System.Windows;
using vJoyInterfaceWrap;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.Joysticks;

public class JoyManager : IJoysticks
{
    private readonly vJoy _vJoy = new();
    private readonly object _vJoyLock = new SemaphoreSlim(1, 1);

    private readonly JoyFeeder[] _joysticks = new JoyFeeder[16];
    private readonly CancellationTokenSource _t = new();

    private readonly uint _versionDll;
    private readonly uint _versionDrv;

    public JoyManager()
    {
        if (!_vJoy.vJoyEnabled())
        {
            MessageBox.Show("Fatal: vJoy not enabled!");
            Application.Current.Shutdown();
        }

        // Test if DLL matches the driver
        uint dll = 0, drv = 0;
        _vJoy.DriverMatch(ref dll, ref drv);
        _versionDll = dll;
        _versionDrv = drv;

        for (uint i = 0; i < _joysticks.Length; i++)
        {
            var feeder = new JoyFeeder(ref _vJoyLock, ref _vJoy, i + 1);
            _joysticks[i] = feeder;

            var random = new Random((int)i);
            Task.Run(async () =>
            {
                while (!_t.Token.IsCancellationRequested)
                {
                    var status = feeder.UpdateStatus();
                    var time = .5 + random.NextDouble();
                    if (status == JoystickStatus.Occupied) time += 1;
                    await Task.Delay(TimeSpan.FromSeconds(time));
                }
            }, _t.Token);
        }
    }

    public void Dispose()
    {
        _t.Cancel();
    }

    public int GetJoystickCount()
    {
        return _joysticks.Length;
    }

    public IJoystick? GetJoystick(int id)
    {
        if (id < 1 || id > _joysticks.Length) return null;
        return _joysticks[id - 1];
    }

    public (bool, uint, uint) GetVersions()
    {
        return (_versionDll == _versionDrv, _versionDll, _versionDrv);
    }
}