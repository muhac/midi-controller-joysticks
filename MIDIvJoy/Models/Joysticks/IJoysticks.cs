using MIDIvJoy.Models.DataModels;
using OneOf;

namespace MIDIvJoy.Models.Joysticks;

public interface IJoysticks
{
    public int GetJoystickCount();
    public IJoystick GetJoystick(int id);
    public (bool isMatch, uint verDll, uint verDrv) GetVersions();
}

public interface IJoystick
{
    public JoystickStatus GetStatus();
    event EventHandler<JoystickStatusEventArgs> StatusChanged;

    public JoystickHardware GetHardware();
    public JoystickState GetState();

    public IJoystickFeeder GetFeeder();
}

public interface IJoystickFeeder
{
    public Task<bool> Acquire();
    public Task<bool> Release();

    public Task<bool> Set(OneOf<JoystickAxis, JoystickButton> action, int value);
}