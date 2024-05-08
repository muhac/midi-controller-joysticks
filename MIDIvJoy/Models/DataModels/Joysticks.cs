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

public class JoystickAction
{
    public int DeviceId = 1;
    public ActionType Type = ActionType.None;
    public JoystickActionAxis Axis = new(JoystickAxis.X);
    public JoystickActionButton Button = new(1);
    public int Value = 0;
}

public enum ActionType
{
    None,
    Axis,
    Button,
}

public enum ActionTypeButton
{
    Press,
    Release,
    Click,
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

public class JoystickActionAxis(JoystickAxis axis)
{
    public JoystickAxis Axis { get; set; } = axis;
    public double Percent { get; set; } = 0;
}

public class JoystickActionButton(int number)
{
    public int Number { get; set; } = number;
    public ActionTypeButton Type { get; set; } = ActionTypeButton.Press;
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