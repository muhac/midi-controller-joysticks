using vJoyInterfaceWrap;

namespace MIDIvJoy.Models.DataModels;

public enum JoystickStatus
{
    Free,
    Engaged,
    Occupied,
    Unknown,
}

public class JoystickStatusEventArgs(JoystickStatus status) : EventArgs
{
    public JoystickStatus Status { get; } = status;
}

public enum JoystickAxis : uint
{
    X = HID_USAGES.HID_USAGE_X,
    Y = HID_USAGES.HID_USAGE_Y,
    Z = HID_USAGES.HID_USAGE_Z,

    XRot = HID_USAGES.HID_USAGE_RX,
    YRot = HID_USAGES.HID_USAGE_RY,
    ZRot = HID_USAGES.HID_USAGE_RZ,

    Slider = HID_USAGES.HID_USAGE_SL0,
    Dial = HID_USAGES.HID_USAGE_SL1,

    Throttle = HID_USAGES.HID_USAGE_THROTTLE,
    Rudder = HID_USAGES.HID_USAGE_RUDDER,
    Aileron = HID_USAGES.HID_USAGE_AILERON,

    Wheel = HID_USAGES.HID_USAGE_WHL,
    Accelerator = HID_USAGES.HID_USAGE_ACCELERATOR,
    Brake = HID_USAGES.HID_USAGE_BRAKE,
    Clutch = HID_USAGES.HID_USAGE_CLUTCH,
    Steering = HID_USAGES.HID_USAGE_STEERING,
}

public struct JoystickButton(int number)
{
    private readonly int _number = number;

    public static implicit operator int(JoystickButton button)
    {
        return button._number;
    }

    public static implicit operator JoystickButton(int number)
    {
        return new JoystickButton(number);
    }
}

public class JoystickHardware
{
    public long AxisMax;
    public long AxisMin;

    public bool AxisXEnabled;
    public bool AxisYEnabled;
    public bool AxisZEnabled;

    public bool AxisXRotEnabled;
    public bool AxisYRotEnabled;
    public bool AxisZRotEnabled;

    public bool AxisSliderEnabled;
    public bool AxisDialEnabled;

    public bool AxisThrottleEnabled;
    public bool AxisRudderEnabled;
    public bool AxisAileronEnabled;

    public bool AxisWheelEnabled;
    public bool AxisAcceleratorEnabled;
    public bool AxisBrakeEnabled;
    public bool AxisClutchEnabled;
    public bool AxisSteeringEnabled;

    public int ButtonNumber;
}

public class JoystickState
{
    public bool Ok;

    public int AxisX;
    public int AxisY;
    public int AxisZ;

    public int AxisXRot;
    public int AxisYRot;
    public int AxisZRot;

    public int AxisSlider;
    public int AxisDial;

    public int AxisThrottle;
    public int AxisRudder;
    public int AxisAileron;

    public int AxisWheel;
    public int AxisAccelerator;
    public int AxisBrake;
    public int AxisClutch;
    public int AxisSteering;

    public uint Buttons;
}

public class JoystickInstance
{
    public bool Ok = false;
    internal JoystickHardware Hardware = new();
    internal vJoy.JoystickState State = new();
}