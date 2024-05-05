using MIDIvJoy.Models.DataModels;
using MIDIvJoy.Models.Joysticks;

namespace MIDIvJoy.ViewModels;

public struct JoyDevice
{
    public int Id;
    public JoystickStatus Status;
}

public struct JoyState
{
    public bool Ok;

    public JoyStateAxis[] Axes;

    public int ButtonNumber;
    public bool[] ButtonStates;
}

public struct JoyStateAxis
{
    public string Name;
    public int Index2Col;

    public bool Enabled;
    public int Value;
    public double Percent;
}

public class JoyWatcherViewModel(IJoysticks m)
{
    public JoyDevice[] DevicesAvailable { get; private set; } = [];
    public int DeviceId { get; private set; } = 1;

    public bool IsDisplayData { get; private set; }
    public JoyState DisplayState { get; private set; } = new() { Ok = false };

    private Timer? _timer;

    public void OnInitialized()
    {
        UpdateIds();
        DeviceId = DevicesAvailable.Select(device => device.Id).DefaultIfEmpty(1).First();

        _timer?.Dispose();
        _timer = new Timer(Watch, 1, TimeSpan.Zero, TimeSpan.FromSeconds(1d / 30));
        Watch(true);

        Enumerable.Range(1, m.GetJoystickCount())
            .ToList()
            .ForEach(id => m.GetJoystick(id).StatusChanged += OnStatusChanged);
    }

    private void OnStatusChanged(object? sender, JoystickStatusEventArgs e)
    {
        UpdateIds();
        Watch(true);
    }

    private void UpdateIds()
    {
        var status = Enumerable.Range(1, m.GetJoystickCount())
            .Select(id => m.GetJoystick(id).GetStatus())
            .ToArray();

        DevicesAvailable = Enumerable.Range(1, m.GetJoystickCount())
            .Select(id => new JoyDevice { Id = id, Status = status[id - 1] })
            .Where(device => device.Status != JoystickStatus.Unknown)
            .ToArray();
    }

    public void SwitchId(int id)
    {
        DeviceId = id;
        Watch(true);
    }

    private void Watch(object? isForceUpdate)
    {
        // no need to update if the device is not up
        if (!IsDisplayData && isForceUpdate == null) return;

        DisplayState = FormatState();
        IsDisplayData = DisplayState.Ok;

        StateHasChanged();
    }


    private JoyState FormatState()
    {
        var joystick = m.GetJoystick(DeviceId);
        var hw = joystick.GetHardware();
        var state = joystick.GetState();

        var displayState = new JoyState
        {
            Ok = state.Ok,
            Axes =
            [
                new JoyStateAxis
                {
                    Name = "X",
                    Index2Col = 1,
                    Enabled = hw.AxisXEnabled,
                    Value = state.AxisX,
                    Percent = GetPercent(state.AxisX),
                },

                new JoyStateAxis
                {
                    Name = "Y",
                    Index2Col = 3,
                    Enabled = hw.AxisYEnabled,
                    Value = state.AxisY,
                    Percent = GetPercent(state.AxisY),
                },
                new JoyStateAxis
                {
                    Name = "Z",
                    Index2Col = 5,
                    Enabled = hw.AxisZEnabled,
                    Value = state.AxisZ,
                    Percent = GetPercent(state.AxisZ),
                },

                new JoyStateAxis
                {
                    Name = "X Rot",
                    Index2Col = 2,
                    Enabled = hw.AxisXRotEnabled,
                    Value = state.AxisXRot,
                    Percent = GetPercent(state.AxisXRot),
                },
                new JoyStateAxis
                {
                    Name = "Y Rot",
                    Index2Col = 4,
                    Enabled = hw.AxisYRotEnabled,
                    Value = state.AxisYRot,
                    Percent = GetPercent(state.AxisYRot),
                },
                new JoyStateAxis
                {
                    Name = "Z Rot",
                    Index2Col = 6,
                    Enabled = hw.AxisZRotEnabled,
                    Value = state.AxisZRot,
                    Percent = GetPercent(state.AxisZRot),
                },

                new JoyStateAxis
                {
                    Name = "Slider",
                    Index2Col = 7,
                    Enabled = hw.AxisSliderEnabled,
                    Value = state.AxisSlider,
                    Percent = GetPercent(state.AxisSlider),
                },
                new JoyStateAxis
                {
                    Name = "Dial",
                    Index2Col = 9,
                    Enabled = hw.AxisDialEnabled,
                    Value = state.AxisDial,
                    Percent = GetPercent(state.AxisDial),
                },

                new JoyStateAxis
                {
                    Name = "Throttle",
                    Index2Col = 11,
                    Enabled = hw.AxisThrottleEnabled,
                    Value = state.AxisThrottle,
                    Percent = GetPercent(state.AxisThrottle),
                },
                new JoyStateAxis
                {
                    Name = "Rudder",
                    Index2Col = 13,
                    Enabled = hw.AxisRudderEnabled,
                    Value = state.AxisRudder,
                    Percent = GetPercent(state.AxisRudder),
                },
                new JoyStateAxis
                {
                    Name = "Aileron",
                    Index2Col = 15,
                    Enabled = hw.AxisAileronEnabled,
                    Value = state.AxisAileron,
                    Percent = GetPercent(state.AxisAileron),
                },

                new JoyStateAxis
                {
                    Name = "Wheel",
                    Index2Col = 8,
                    Enabled = hw.AxisWheelEnabled,
                    Value = state.AxisWheel,
                    Percent = GetPercent(state.AxisWheel),
                },
                new JoyStateAxis
                {
                    Name = "Acc",
                    Index2Col = 10,
                    Enabled = hw.AxisAcceleratorEnabled,
                    Value = state.AxisAccelerator,
                    Percent = GetPercent(state.AxisAccelerator),
                },
                new JoyStateAxis
                {
                    Name = "Brake",
                    Index2Col = 12,
                    Enabled = hw.AxisBrakeEnabled,
                    Value = state.AxisBrake,
                    Percent = GetPercent(state.AxisBrake),
                },
                new JoyStateAxis
                {
                    Name = "Clutch",
                    Index2Col = 14,
                    Enabled = hw.AxisClutchEnabled,
                    Value = state.AxisClutch,
                    Percent = GetPercent(state.AxisClutch),
                },
                new JoyStateAxis
                {
                    Name = "Steering",
                    Index2Col = 16,
                    Enabled = hw.AxisSteeringEnabled,
                    Value = state.AxisSteering,
                    Percent = GetPercent(state.AxisSteering),
                }
            ],
            ButtonNumber = hw.ButtonNumber,
            ButtonStates = new bool[hw.ButtonNumber],
        };

        for (var i = 0; i < displayState.ButtonStates.Length; i++)
        {
            displayState.ButtonStates[i] = (state.Buttons & 1 << i) != 0;
        }

        return displayState;

        double GetPercent(int value)
        {
            var min = hw.AxisMin;
            var max = hw.AxisMax;
            var pct = (double)(value - min) / (max - min) * 100;

            // the ui lib has a bug to display 100% in a different style
            return double.Max(double.Min(pct, 99.9999), 0.0001);
        }
    }


    public event Action StateHasChanged = delegate { };
}