using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DDCCI
{
    public class Control
    {
        public const Int32 MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;
        public const Int32 PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;


        public const uint MC_CAPS_NONE = 0x00000000;
        public const uint MC_CAPS_MONITOR_TECHNOLOGY_TYPE = 0x00000001;
        public const uint MC_CAPS_BRIGHTNESS = 0x00000002;
        public const uint MC_CAPS_CONTRAST = 0x00000004;
        public const uint MC_CAPS_COLOR_TEMPERATURE = 0x00000008;
        public const uint MC_CAPS_RED_GREEN_BLUE_GAIN = 0x00000010;
        public const uint MC_CAPS_RED_GREEN_BLUE_DRIVE = 0x00000020;
        public const uint MC_CAPS_DEGAUSS = 0x00000040;
        public const uint MC_CAPS_DISPLAY_AREA_POSITION = 0x00000080;
        public const uint MC_CAPS_DISPLAY_AREA_SIZE = 0x00000100;
        public const uint MC_CAPS_RESTORE_FACTORY_DEFAULTS = 0x00000400;
        public const uint MC_CAPS_RESTORE_FACTORY_COLOR_DEFAULTS = 0x00000800;
        public const uint MC_RESTORE_FACTORY_DEFAULTS_ENABLES_MONITOR_SETTINGS = 0x00001000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MonitorInfo
        {
            public int cbSize = Marshal.SizeOf(typeof(MonitorInfo));
            public Screen rcMonitor;
            public Screen rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorData
        {
            public IntPtr MonitorHandle;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public char[] MonitorDescription;
        }

        #region Win API

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        [DllImport("dxva2.dll", SetLastError = true)]
        private extern static bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        private extern static bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] MonitorData[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private extern static bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        private extern static bool SetMonitorBrightness(IntPtr hMonitor, uint pdwNewBrightness);

        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [Out] MonitorData[] pPhysicalMonitorArray);

        #endregion

        public static MonitorData[] Get_Devices()
        {
            uint MonitorsCount;

            var hwnd = Process.GetCurrentProcess().MainWindowHandle;

            IntPtr CurrentMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            GetNumberOfPhysicalMonitorsFromHMONITOR(CurrentMonitor, out MonitorsCount);

            MonitorData[] MonitorDataArray = new MonitorData[MonitorsCount];

            GetPhysicalMonitorsFromHMONITOR(CurrentMonitor, MonitorsCount, MonitorDataArray);

            return MonitorDataArray;
        }

        public static double GetBrightness(IntPtr MonitorHandle)
        {
            uint dwMinimumBrightness, dwCurrentBrightness, dwMaximumBrightness;
            if (!GetMonitorBrightness(MonitorHandle, out dwMinimumBrightness, out dwCurrentBrightness, out dwMaximumBrightness))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return (dwCurrentBrightness - dwMinimumBrightness) / (double)(dwMaximumBrightness - dwMinimumBrightness);
        }
        
        public static bool SetBrightness(IntPtr MonitorHandle, int Brightness)
        {
            return SetMonitorBrightness(MonitorHandle, (uint) Brightness);
        }

        public static bool DestroyMonitors(MonitorData[] monitors)
        {
            return DestroyPhysicalMonitors((uint)monitors.Length, monitors);
        }
    }
}
