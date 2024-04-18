namespace MIDIvJoy.Services.Joysticks;

using vJoyInterfaceWrap;

public enum JoyStatus
{
    Free,
    Engaged,
    Occupied,
    Unknown,
}

public class JoyFeeder
{
    private readonly object _driverGlobalLock;
    private readonly object _vjStateLocalLock = new();

    private readonly uint _id;
    private readonly vJoy _vj;
    private vJoy.JoystickState _state;

    private long _axisMax;
    private long _axisMin;

    public JoyStatus Status { get; private set; }

    internal JoyFeeder(ref object wLock, ref vJoy vj, uint id)
    {
        _driverGlobalLock = wLock;
        _id = id;
        _vj = vj;
        _state = new vJoy.JoystickState();
        Status = JoyStatus.Unknown;
    }

    private void GetState()
    {
#if DEBUG
        var s = new Stopwatch();
#endif
        lock (_vjStateLocalLock)
        lock (_driverGlobalLock)
            _vj.GetPosition(_id, ref _state);
#if DEBUG
        s.Lap("[GET s]", _id);
#endif
    }

    private void SetState()
    {
#if DEBUG
        var s = new Stopwatch();
#endif
        lock (_vjStateLocalLock)
        lock (_driverGlobalLock)
            _vj.UpdateVJD(_id, ref _state);
#if DEBUG
        s.Lap("[SET s]", _id);
#endif
    }

    private void InitState()
    {
        lock (_driverGlobalLock)
        {
            _vj.GetVJDAxisMax(_id, HID_USAGES.HID_USAGE_X, ref _axisMax);
            _vj.GetVJDAxisMin(_id, HID_USAGES.HID_USAGE_X, ref _axisMin);
        }

        Console.WriteLine("vJoy {0}: Axis Min: {1}, Max: {2}", _id, _axisMin, _axisMax);

        var mid = AxisPct2Val(.5);
        _state = new vJoy.JoystickState
        {
            AxisX = mid,
            AxisY = mid,
            AxisZ = mid,
            AxisXRot = mid,
            AxisYRot = mid,
            AxisZRot = mid,
        };

        SetState();
    }

    public void Acquire()
    {
#if DEBUG
        var s = new Stopwatch();
#endif
        lock (_driverGlobalLock) _vj.AcquireVJD(_id);
#if DEBUG
        s.Lap("[SET 1]", _id);
#endif
        InitState();
        UpdateStatus();
    }

    public void Release()
    {
#if DEBUG
        var s = new Stopwatch();
#endif
        lock (_driverGlobalLock) _vj.RelinquishVJD(_id);
#if DEBUG
        s.Lap("[SET 0]", _id);
#endif
        UpdateStatus();
    }

    public delegate void StatusChangedHandler(JoyFeeder sender, JoyStatus status);

    public event StatusChangedHandler? StatusChanged;

    internal JoyStatus UpdateStatus()
    {
#if DEBUG
        var s = new Stopwatch();
#endif
        VjdStat status;
        lock (_driverGlobalLock) status = _vj.GetVJDStatus(_id);
#if DEBUG
        s.Lap("[GET -]", _id);
#endif

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

    private int AxisPct2Val(double pct) => (int)(.5 + _axisMin + pct * (_axisMax - _axisMin));

#if DEBUG
    private class Stopwatch
    {
        private readonly DateTime _start = DateTime.Now;

        public void Lap(string command, uint id)
        {
            var cost = DateTime.Now - _start;
            Console.WriteLine(
                "{0} {1,2} - {2,7:F4} ms {3}",
                command, id, cost.TotalMilliseconds,
                new string('!', (int)cost.TotalMilliseconds)
            );
        }
    }
#endif
}