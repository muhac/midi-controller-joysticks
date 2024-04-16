#define ROBUST
//#define EFFICIENT
#define FFB
#define DUMP_FFB_FRAME

namespace MIDIvJoy.Services;

using vJoyInterfaceWrap;

public class JoyFeeder
{
    public static vJoy? DemoRun(uint id = 1)
    {
        // Create one joystick object and a position structure.
        var joystick = new vJoy();
        var iReport = new vJoy.JoystickState();

        // Device ID can only be in the range 1-16
        if (id is < 1 or > 16)
        {
            Console.WriteLine("Illegal device ID {0}\nExit!", id);
            return null;
        }

        // Get the driver attributes (Vendor ID, Product ID, Version Number)
        if (!joystick.vJoyEnabled())
        {
            Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.");
            return null;
        }

        Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}", joystick.GetvJoyManufacturerString(),
            joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

        // Get the state of the requested device
        var status = joystick.GetVJDStatus(id);
        switch (status)
        {
            case VjdStat.VJD_STAT_OWN:
                Console.WriteLine("vJoy Device {0} is already owned by this feeder", id);
                break;
            case VjdStat.VJD_STAT_FREE:
                Console.WriteLine("vJoy Device {0} is free", id);
                break;
            case VjdStat.VJD_STAT_BUSY:
                Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue", id);
                return null;
            case VjdStat.VJD_STAT_MISS:
                Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue", id);
                return null;
            default:
                Console.WriteLine("vJoy Device {0} general error\nCannot continue", id);
                return null;
        }

        // Check which axes are supported
        bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
        bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
        bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
        bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
        bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
        // Get the number of buttons and POV Hat switchessupported by this vJoy device
        int nButtons = joystick.GetVJDButtonNumber(id);
        int ContPovNumber = joystick.GetVJDContPovNumber(id);
        int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

        // Print results
        Console.WriteLine("\nvJoy Device {0} capabilities:", id);
        Console.WriteLine("Numner of buttons\t\t{0}", nButtons);
        Console.WriteLine("Numner of Continuous POVs\t{0}", ContPovNumber);
        Console.WriteLine("Numner of Descrete POVs\t\t{0}", DiscPovNumber);
        Console.WriteLine("Axis X\t\t{0}", AxisX ? "Yes" : "No");
        Console.WriteLine("Axis Y\t\t{0}", AxisX ? "Yes" : "No");
        Console.WriteLine("Axis Z\t\t{0}", AxisX ? "Yes" : "No");
        Console.WriteLine("Axis Rx\t\t{0}", AxisRX ? "Yes" : "No");
        Console.WriteLine("Axis Rz\t\t{0}", AxisRZ ? "Yes" : "No");

        // Test if DLL matches the driver
        UInt32 DllVer = 0, DrvVer = 0;
        var match = joystick.DriverMatch(ref DllVer, ref DrvVer);
        if (match)
            Console.WriteLine("Version of Driver Matches DLL Version ({0:X})", DllVer);
        else
            Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", DrvVer, DllVer);


        // Acquire the target
        if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
        {
            Console.WriteLine("Failed to acquire vJoy device number {0}.", id);
            return null;
        }
        else
            Console.WriteLine("Acquired: vJoy device number {0}.", id);

        Console.WriteLine("\nGo!");

        int X, Y, Z, ZR, XR;
        uint count = 0;
        long maxval = 0;

        X = 20;
        Y = 30;
        Z = 40;
        XR = 60;
        ZR = 80;

        joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

#if ROBUST
        bool res;
        // Reset this device to default values
        joystick.ResetVJD(id);

        // Feed the device in endless loop
        while (true)
        {
            // Set position of 4 axes
            res = joystick.SetAxis(X, id, HID_USAGES.HID_USAGE_X);
            res = joystick.SetAxis(Y, id, HID_USAGES.HID_USAGE_Y);
            res = joystick.SetAxis(Z, id, HID_USAGES.HID_USAGE_Z);
            res = joystick.SetAxis(XR, id, HID_USAGES.HID_USAGE_RX);
            res = joystick.SetAxis(ZR, id, HID_USAGES.HID_USAGE_RZ);

            // Press/Release Buttons
            res = joystick.SetBtn(true, id, count / 50);
            res = joystick.SetBtn(false, id, 1 + count / 50);

            // If Continuous POV hat switches installed - make them go round
            // For high values - put the switches in neutral state
            if (ContPovNumber > 0)
            {
                if ((count * 70) < 30000)
                {
                    res = joystick.SetContPov(((int)count * 70), id, 1);
                    res = joystick.SetContPov(((int)count * 70) + 2000, id, 2);
                    res = joystick.SetContPov(((int)count * 70) + 4000, id, 3);
                    res = joystick.SetContPov(((int)count * 70) + 6000, id, 4);
                }
                else
                {
                    res = joystick.SetContPov(-1, id, 1);
                    res = joystick.SetContPov(-1, id, 2);
                    res = joystick.SetContPov(-1, id, 3);
                    res = joystick.SetContPov(-1, id, 4);
                }

                ;
            }

            ;

            // If Discrete POV hat switches installed - make them go round
            // From time to time - put the switches in neutral state
            if (DiscPovNumber > 0)
            {
                if (count < 550)
                {
                    joystick.SetDiscPov((((int)count / 20) + 0) % 4, id, 1);
                    joystick.SetDiscPov((((int)count / 20) + 1) % 4, id, 2);
                    joystick.SetDiscPov((((int)count / 20) + 2) % 4, id, 3);
                    joystick.SetDiscPov((((int)count / 20) + 3) % 4, id, 4);
                }
                else
                {
                    joystick.SetDiscPov(-1, id, 1);
                    joystick.SetDiscPov(-1, id, 2);
                    joystick.SetDiscPov(-1, id, 3);
                    joystick.SetDiscPov(-1, id, 4);
                }

                ;
            }

            ;

            System.Threading.Thread.Sleep(20);
            X += 150;
            if (X > maxval) X = 0;
            Y += 250;
            if (Y > maxval) Y = 0;
            Z += 350;
            if (Z > maxval) Z = 0;
            XR += 220;
            if (XR > maxval) XR = 0;
            ZR += 200;
            if (ZR > maxval) ZR = 0;
            count++;

            if (count > 640)
                count = 0;
        } // While (Robust)

#endif // ROBUST
#if EFFICIENT
            byte[] pov = new byte[4];

      while (true)
            {
            iReport.bDevice = (byte)id;
            iReport.AxisX = X;
            iReport.AxisY = Y;
            iReport.AxisZ = Z;
            iReport.AxisZRot = ZR;
            iReport.AxisXRot = XR;

            // Set buttons one by one
            iReport.Buttons = (uint)(0x1 <<  (int)(count / 20));

        if (ContPovNumber>0)
        {
            // Make Continuous POV Hat spin
            iReport.bHats = (count*70);
            iReport.bHatsEx1 = (count*70)+3000;
            iReport.bHatsEx2 = (count*70)+5000;
            iReport.bHatsEx3 = 15000 - (count*70);
            if ((count*70) > 36000)
            {
                iReport.bHats = 0xFFFFFFFF; // Neutral state
                iReport.bHatsEx1 = 0xFFFFFFFF; // Neutral state
                iReport.bHatsEx2 = 0xFFFFFFFF; // Neutral state
                iReport.bHatsEx3 = 0xFFFFFFFF; // Neutral state
            };
        }
        else
        {
            // Make 5-position POV Hat spin

            pov[0] = (byte)(((count / 20) + 0)%4);
            pov[1] = (byte)(((count / 20) + 1) % 4);
            pov[2] = (byte)(((count / 20) + 2) % 4);
            pov[3] = (byte)(((count / 20) + 3) % 4);

            iReport.bHats = (uint)(pov[3]<<12) | (uint)(pov[2]<<8) | (uint)(pov[1]<<4) | (uint)pov[0];
            if ((count) > 550)
                iReport.bHats = 0xFFFFFFFF; // Neutral state
        };

        /*** Feed the driver with the position packet - is fails then wait for input then try to re-acquire device ***/
        if (!joystick.UpdateVJD(id, ref iReport))
        {
            Console.WriteLine("Feeding vJoy device number {0} failed - try to enable device then press enter\n", id);
            Console.ReadKey(true);
            joystick.AcquireVJD(id);
        }

        System.Threading.Thread.Sleep(20);
        count++;
        if (count > 640) count = 0;

        X += 150; if (X > maxval) X = 0;
        Y += 250; if (Y > maxval) Y = 0;
        Z += 350; if (Z > maxval) Z = 0;
        XR += 220; if (XR > maxval) XR = 0;
        ZR += 200; if (ZR > maxval) ZR = 0;

      }; // While

#endif // EFFICIENT

        return joystick;
    } // DemoRun
}