﻿using OneOf;
using vJoyInterfaceWrap;
using MIDIvJoy.Models.DataModels;

namespace MIDIvJoy.Models.Joysticks;

public class JoyFeeder : IJoystick, IJoystickFeeder
{
    private readonly object _driverLock;

    private readonly uint _id;
    private readonly vJoy _vj;

    private JoystickInstance _instance;
    private readonly object _instanceLock = new();

    private JoystickInstance Instance
    {
        get
        {
            lock (_instanceLock) return _instance;
        }
        set
        {
            lock (_instanceLock) _instance = value;
        }
    }

    private JoystickStatus _status;
    private readonly object _statusLock = new();

    private JoystickStatus Status
    {
        get
        {
            lock (_statusLock) return _status;
        }
        set
        {
            lock (_statusLock) _status = value;
        }
    }

    internal JoyFeeder(ref object wLock, ref vJoy vj, uint id)
    {
        _driverLock = wLock;
        _id = id;
        _vj = vj;
        _instance = new JoystickInstance { Ok = false };
        _status = JoystickStatus.Unknown;
    }

    public JoystickStatus GetStatus()
    {
        UpdateStatus();
        return Status;
    }

    public JoystickHardware GetHardware()
    {
        return Instance.Hardware;
    }

    public JoystickState GetState()
    {
        GetJoyState();
        return new JoystickState
        {
            Ok = Instance.Ok,

            AxisX = Instance.State.AxisX,
            AxisY = Instance.State.AxisY,
            AxisZ = Instance.State.AxisZ,

            AxisXRot = Instance.State.AxisXRot,
            AxisYRot = Instance.State.AxisYRot,
            AxisZRot = Instance.State.AxisZRot,

            AxisSlider = Instance.State.Slider,
            AxisDial = Instance.State.Dial,

            AxisThrottle = Instance.State.Throttle,
            AxisRudder = Instance.State.Rudder,
            AxisAileron = Instance.State.Aileron,

            AxisWheel = Instance.State.Wheel,
            AxisAccelerator = Instance.State.Accelerator,
            AxisBrake = Instance.State.Brake,
            AxisClutch = Instance.State.Clutch,
            AxisSteering = Instance.State.Steering,

            Buttons = Instance.State.Buttons,
        };
    }

    public IJoystickFeeder GetFeeder()
    {
        return this;
    }

    private void GetJoyState()
    {
        lock (_driverLock) _vj.GetPosition(_id, ref Instance.State);
    }

    private bool SetJoyState()
    {
        lock (_driverLock) return _vj.UpdateVJD(_id, ref Instance.State);
    }

    private void ResetJoyState()
    {
        var mid = AxisPct2Val(.5);

        Instance.State = new vJoy.JoystickState
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

        SetJoyState();
    }

    private void InitState()
    {
        var axes = Enum.GetValues(typeof(JoystickAxis)).Cast<JoystickAxis>().ToArray();
        var axesEnabled = new bool[axes.Length];

        lock (_driverLock)
        {
            var axisRangeChecked = false;
            for (var i = 0; i < axes.Length; i++)
            {
                var axis = (HID_USAGES)axes[i];
                axesEnabled[i] = _vj.GetVJDAxisExist(_id, axis);
                if (!axesEnabled[i] || axisRangeChecked) continue;

                _vj.GetVJDAxisMin(_id, axis, ref Instance.Hardware.AxisMin);
                _vj.GetVJDAxisMax(_id, axis, ref Instance.Hardware.AxisMax);
                axisRangeChecked = true;
            }

            Instance.Hardware.ButtonNumber = int.Max(0, _vj.GetVJDButtonNumber(_id));
        }

        Instance.Hardware.AxisXEnabled = axesEnabled[0];
        Instance.Hardware.AxisYEnabled = axesEnabled[1];
        Instance.Hardware.AxisZEnabled = axesEnabled[2];

        Instance.Hardware.AxisXRotEnabled = axesEnabled[3];
        Instance.Hardware.AxisYRotEnabled = axesEnabled[4];
        Instance.Hardware.AxisZRotEnabled = axesEnabled[5];

        Instance.Hardware.AxisSliderEnabled = axesEnabled[6];
        Instance.Hardware.AxisDialEnabled = axesEnabled[7];

        Instance.Hardware.AxisThrottleEnabled = axesEnabled[8];
        Instance.Hardware.AxisRudderEnabled = axesEnabled[9];
        Instance.Hardware.AxisAileronEnabled = axesEnabled[10];

        Instance.Hardware.AxisWheelEnabled = axesEnabled[11];
        Instance.Hardware.AxisAcceleratorEnabled = axesEnabled[12];
        Instance.Hardware.AxisBrakeEnabled = axesEnabled[13];
        Instance.Hardware.AxisClutchEnabled = axesEnabled[14];
        Instance.Hardware.AxisSteeringEnabled = axesEnabled[15];

        Console.WriteLine("vJoy {0,2}: {1} axes, {2} buttons", _id,
            axesEnabled.Count(x => x), Instance.Hardware.ButtonNumber);
    }

    // Lock the click-release action to prevent multiple simultaneous clicks
    private readonly object _clickReleaseLock = new();
    private Guid? _clickReleaseId;
    private const int ClickReleaseIntervals = 50;

    public async Task<bool> Set(OneOf<JoystickActionAxis, JoystickActionButton> action)
    {
        // Special case for click-release
        if (action.IsT1 && action.AsT1.Type == ActionTypeButton.Click)
        {
            var id = Guid.NewGuid();
            lock (_clickReleaseLock)
            {
                if (_clickReleaseId is not null) return false;
                _clickReleaseId = id;
            }

            var press = new JoystickActionButton(action.AsT1!.Number)
            {
                Type = ActionTypeButton.Press
            };
            var release = new JoystickActionButton(action.AsT1!.Number)
            {
                Type = ActionTypeButton.Release
            };

            var ok = await Set(press);
            _ = Task.Run(async () =>
            {
                // keep pressed
                await Task.Delay(ClickReleaseIntervals);

                // the return value sometimes is not reliable
                // intentional double release in case the first one was missed
                await Set(release);
                await Set(release);

                // keep released
                await Task.Delay(ClickReleaseIntervals);
                lock (_clickReleaseLock) _clickReleaseId = null;
            });

            return ok;
        }

        action.Switch(
            SetAxis,
            SetButton
        );

        return SetJoyState();
    }

    private void SetAxis(JoystickActionAxis action)
    {
        var axisName = action.Name.ToString();
        if (axisName.StartsWith('X') || // X, XRot
            axisName.StartsWith('Y') || // Y, YRot
            axisName.StartsWith('Z')) //   Z, ZRot
        {
            axisName = "Axis" + axisName;
        }

        var stateType = Instance.State.GetType();
        var axisField = stateType.GetField(axisName);
        if (axisField is null) return;

        var axisValueCurrent = (int)axisField.GetValue(Instance.State)!;
        var axisValue = action.Type switch
        {
            // TODO: MSFS does not identify small increments
            ActionTypeAxis.Increment => axisValueCurrent + 1,
            ActionTypeAxis.Decrement => axisValueCurrent - 1,
            _ => AxisPct2Val(action.Percent)
        };

        if (axisValue < Instance.Hardware.AxisMin ||
            axisValue > Instance.Hardware.AxisMax)
        {
            axisValue = AxisPct2Val(.5); // reset to mid
        }

        var state = (object)Instance.State;
        axisField.SetValue(state, axisValue);
        Instance.State = (vJoy.JoystickState)state;
    }

    private void SetButton(JoystickActionButton action)
    {
        var bit = 1u << action.Number - 1;
        if (action.On || action.Type == ActionTypeButton.Press)
        {
            Instance.State.Buttons |= bit;
        }
        else
        {
            Instance.State.Buttons &= ~bit;
        }
    }

    public async Task<bool> Acquire()
    {
        return await Task.FromResult(AcquireSync());
    }

    private bool AcquireSync()
    {
        bool acquired;
        lock (_driverLock) acquired = _vj.AcquireVJD(_id);
        if (!acquired) return false;

        Instance.Ok = acquired;
        InitState();
        ResetJoyState();
        UpdateStatus();

        Console.WriteLine($"Acquired vJoy device {_id}. ({acquired})");
        return acquired;
    }

    public async Task<bool> Release()
    {
        return await Task.FromResult(ReleaseSync());
    }

    private bool ReleaseSync()
    {
        lock (_driverLock) _vj.RelinquishVJD(_id);
        var released = UpdateStatus() != JoystickStatus.Engaged;

        Instance.Ok = !released;
        Console.WriteLine($"Released vJoy device {_id}. ({released})");
        return released;
    }

    public event EventHandler<JoystickStatusEventArgs>? StatusChanged;

    internal JoystickStatus UpdateStatus()
    {
        VjdStat status;
        lock (_driverLock) status = _vj.GetVJDStatus(_id);

        var newStatus = status switch
        {
            VjdStat.VJD_STAT_FREE => JoystickStatus.Free,
            VjdStat.VJD_STAT_OWN => JoystickStatus.Engaged,
            VjdStat.VJD_STAT_BUSY => JoystickStatus.Occupied,
            _ => JoystickStatus.Unknown
        };

        if (newStatus == Status) return Status;

        Status = newStatus;
        StatusChanged?.Invoke(this, new JoystickStatusEventArgs(Status));
        return Status;
    }

    private int AxisPct2Val(double pct)
    {
        var min = Instance.Hardware.AxisMin;
        var max = Instance.Hardware.AxisMax;
        return (int)(.5 + min + pct * (max - min));
    }
}