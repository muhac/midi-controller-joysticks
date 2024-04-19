namespace MIDIvJoy.Services.Joysticks;

using vJoyInterfaceWrap;

public enum JoyStatus
{
    Free,
    Engaged,
    Occupied,
    Unknown,
}

public class JoyState
{
    internal vJoy.JoystickState Data;
    public bool Ok;

    public long AxisMax;
    public long AxisMin;

    public bool AxisXEnabled;
    public int AxisX;
    public bool AxisYEnabled;
    public int AxisY;
    public bool AxisZEnabled;
    public int AxisZ;

    public bool AxisXRotEnabled;
    public int AxisXRot;
    public bool AxisYRotEnabled;
    public int AxisYRot;
    public bool AxisZRotEnabled;
    public int AxisZRot;

    public bool AxisSliderEnabled;
    public int AxisSlider;
    public bool AxisDialEnabled;
    public int AxisDial;

    public bool AxisThrottleEnabled;
    public int AxisThrottle;
    public bool AxisRudderEnabled;
    public int AxisRudder;
    public bool AxisAileronEnabled;
    public int AxisAileron;

    public bool AxisWheelEnabled;
    public int AxisWheel;
    public bool AxisAcceleratorEnabled;
    public int AxisAccelerator;
    public bool AxisBrakeEnabled;
    public int AxisBrake;
    public bool AxisClutchEnabled;
    public int AxisClutch;
    public bool AxisSteeringEnabled;
    public int AxisSteering;

    public int ButtonNumber;
    public uint ButtonStates;
}

public class JoyFeeder
{
    private readonly object _driverGlobalLock;
    private readonly object _vjStateLocalLock = new();

    private readonly uint _id;
    private readonly vJoy _vj;
    private readonly JoyState _vjState;

    private JoyStatus _status;
    private readonly object _statusLock = new();

    public JoyStatus Status
    {
        get
        {
            lock (_statusLock) return _status;
        }
        private set
        {
            lock (_statusLock) _status = value;
        }
    }

    internal JoyFeeder(ref object wLock, ref vJoy vj, uint id)
    {
        _driverGlobalLock = wLock;
        _id = id;
        _vj = vj;
        _vjState = new JoyState { Ok = false };
        Status = JoyStatus.Unknown;
    }

    private void GetState()
    {
        lock (_vjStateLocalLock)
        lock (_driverGlobalLock)
            _vj.GetPosition(_id, ref _vjState.Data);
    }

    private void SetState()
    {
        lock (_vjStateLocalLock)
        lock (_driverGlobalLock)
            _vj.UpdateVJD(_id, ref _vjState.Data);
    }

    private void ResetState()
    {
        var mid = AxisPct2Val(.5);

        lock (_vjStateLocalLock)
        {
            _vjState.Data = new vJoy.JoystickState
            {
                AxisX = mid,
                AxisY = mid,
                AxisZ = mid,

                AxisXRot = mid,
                AxisYRot = mid,
                AxisZRot = mid,

                Slider = 0,
                Dial = 0,

                Throttle = 0,
                Rudder = mid,
                Aileron = mid,

                Wheel = 0,
                Accelerator = 0,
                Brake = 0,
                Clutch = mid,
                Steering = mid,

                Buttons = 0b0,
            };
        }

        SetState();
    }

    private void InitState()
    {
        lock (_vjStateLocalLock)
        lock (_driverGlobalLock)
        {
            HID_USAGES[] axes =
            [
                HID_USAGES.HID_USAGE_X,
                HID_USAGES.HID_USAGE_Y,
                HID_USAGES.HID_USAGE_Z,

                HID_USAGES.HID_USAGE_RX,
                HID_USAGES.HID_USAGE_RY,
                HID_USAGES.HID_USAGE_RZ,

                HID_USAGES.HID_USAGE_SL0,
                HID_USAGES.HID_USAGE_SL1,

                HID_USAGES.HID_USAGE_THROTTLE,
                HID_USAGES.HID_USAGE_RUDDER,
                HID_USAGES.HID_USAGE_AILERON,

                HID_USAGES.HID_USAGE_WHL,
                HID_USAGES.HID_USAGE_ACCELERATOR,
                HID_USAGES.HID_USAGE_BRAKE,
                HID_USAGES.HID_USAGE_CLUTCH,
                HID_USAGES.HID_USAGE_STEERING,
            ];

            var axesEnabled = new bool[axes.Length];
            var axisRangeChecked = false;

            for (var i = 0; i < axes.Length; i++)
            {
                var axis = axes[i];
                axesEnabled[i] = _vj.GetVJDAxisExist(_id, axis);
                if (!axesEnabled[i] || axisRangeChecked) continue;

                _vj.GetVJDAxisMin(_id, axis, ref _vjState.AxisMin);
                _vj.GetVJDAxisMax(_id, axis, ref _vjState.AxisMax);
                axisRangeChecked = true;
            }

            _vjState.AxisXEnabled = axesEnabled[0];
            _vjState.AxisYEnabled = axesEnabled[1];
            _vjState.AxisZEnabled = axesEnabled[2];

            _vjState.AxisXRotEnabled = axesEnabled[3];
            _vjState.AxisYRotEnabled = axesEnabled[4];
            _vjState.AxisZRotEnabled = axesEnabled[5];

            _vjState.AxisSliderEnabled = axesEnabled[6];
            _vjState.AxisDialEnabled = axesEnabled[7];

            _vjState.AxisThrottleEnabled = axesEnabled[8];
            _vjState.AxisRudderEnabled = axesEnabled[9];
            _vjState.AxisAileronEnabled = axesEnabled[10];

            _vjState.AxisWheelEnabled = axesEnabled[11];
            _vjState.AxisAcceleratorEnabled = axesEnabled[12];
            _vjState.AxisBrakeEnabled = axesEnabled[13];
            _vjState.AxisClutchEnabled = axesEnabled[14];
            _vjState.AxisSteeringEnabled = axesEnabled[15];

            _vjState.ButtonNumber = int.Max(0, _vj.GetVJDButtonNumber(_id));

            Console.WriteLine("vJoy {0,2}: {1} axes, {2} buttons", _id,
                axesEnabled.Count(x => x), _vjState.ButtonNumber);
        }
    }

    public void ShowState(ref JoyState state)
    {
        GetState();

        lock (_vjStateLocalLock)
        {
            state.Data = _vjState.Data;
            state.Ok = _vjState.Ok;

            state.AxisMax = _vjState.AxisMax;
            state.AxisMin = _vjState.AxisMin;

            state.AxisXEnabled = _vjState.AxisXEnabled;
            state.AxisYEnabled = _vjState.AxisYEnabled;
            state.AxisZEnabled = _vjState.AxisZEnabled;

            state.AxisXRotEnabled = _vjState.AxisXRotEnabled;
            state.AxisYRotEnabled = _vjState.AxisYRotEnabled;
            state.AxisZRotEnabled = _vjState.AxisZRotEnabled;

            state.AxisSliderEnabled = _vjState.AxisSliderEnabled;
            state.AxisDialEnabled = _vjState.AxisDialEnabled;

            state.AxisThrottleEnabled = _vjState.AxisThrottleEnabled;
            state.AxisRudderEnabled = _vjState.AxisRudderEnabled;
            state.AxisAileronEnabled = _vjState.AxisAileronEnabled;

            state.AxisWheelEnabled = _vjState.AxisWheelEnabled;
            state.AxisAcceleratorEnabled = _vjState.AxisAcceleratorEnabled;
            state.AxisBrakeEnabled = _vjState.AxisBrakeEnabled;
            state.AxisClutchEnabled = _vjState.AxisClutchEnabled;
            state.AxisSteeringEnabled = _vjState.AxisSteeringEnabled;

            state.ButtonNumber = _vjState.ButtonNumber;
        }

        if (!state.Ok) return;

        state.AxisX = state.AxisXEnabled ? state.Data.AxisX : 0;
        state.AxisY = state.AxisYEnabled ? state.Data.AxisY : 0;
        state.AxisZ = state.AxisZEnabled ? state.Data.AxisZ : 0;

        state.AxisXRot = state.AxisXRotEnabled ? state.Data.AxisXRot : 0;
        state.AxisYRot = state.AxisYRotEnabled ? state.Data.AxisYRot : 0;
        state.AxisZRot = state.AxisZRotEnabled ? state.Data.AxisZRot : 0;

        state.AxisSlider = state.AxisSliderEnabled ? state.Data.Slider : 0;
        state.AxisDial = state.AxisDialEnabled ? state.Data.Dial : 0;

        state.AxisThrottle = state.AxisThrottleEnabled ? state.Data.Throttle : 0;
        state.AxisRudder = state.AxisRudderEnabled ? state.Data.Rudder : 0;
        state.AxisAileron = state.AxisAileronEnabled ? state.Data.Aileron : 0;

        state.AxisWheel = state.AxisWheelEnabled ? state.Data.Wheel : 0;
        state.AxisAccelerator = state.AxisAcceleratorEnabled ? state.Data.Accelerator : 0;
        state.AxisBrake = state.AxisBrakeEnabled ? state.Data.Brake : 0;
        state.AxisClutch = state.AxisClutchEnabled ? state.Data.Clutch : 0;
        state.AxisSteering = state.AxisSteeringEnabled ? state.Data.Steering : 0;

        state.ButtonStates = state.ButtonNumber > 0 ? state.Data.Buttons : 0;
    }

    public void Acquire()
    {
        bool ok;
        lock (_driverGlobalLock) ok = _vj.AcquireVJD(_id);
        if (!ok) return;

        lock (_vjStateLocalLock) _vjState.Ok = true;
        InitState();
        ResetState();
        UpdateStatus();

        Console.WriteLine($"Acquired vJoy device {_id}");
    }

    public void Release()
    {
        lock (_driverGlobalLock) _vj.RelinquishVJD(_id);
        lock (_vjStateLocalLock) _vjState.Ok = false;
        UpdateStatus();

        Console.WriteLine($"Released vJoy device {_id}");
    }

    public delegate void StatusChangedHandler(JoyFeeder sender, JoyStatus status);

    public event StatusChangedHandler? StatusChanged;

    internal JoyStatus UpdateStatus()
    {
        VjdStat status;
        lock (_driverGlobalLock) status = _vj.GetVJDStatus(_id);

        var newStatus = status switch
        {
            VjdStat.VJD_STAT_FREE => JoyStatus.Free,
            VjdStat.VJD_STAT_OWN => JoyStatus.Engaged,
            VjdStat.VJD_STAT_BUSY => JoyStatus.Occupied,
            _ => JoyStatus.Unknown
        };

        if (newStatus == Status) return Status;

        if (newStatus == JoyStatus.Unknown && Status != JoyStatus.Occupied)
        {
            // a transition status added to give a retry chance
            // this is useful when adding/removing vJoy devices
            newStatus = JoyStatus.Occupied;
        }

        Status = newStatus;
        StatusChanged?.Invoke(this, Status);
        return Status;
    }

    private int AxisPct2Val(double pct)
    {
        lock (_vjStateLocalLock)
        {
            return (int)(.5 + _vjState.AxisMin + pct * (_vjState.AxisMax - _vjState.AxisMin));
        }
    }
}