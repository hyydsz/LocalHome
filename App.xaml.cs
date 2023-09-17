using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Mi
{
    public partial class App : Application
    {
        public static List<string> Support_List = new List<string>() { "light.lamp" };

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Process RuningInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] Processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (Process process in Processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    if (process.MainModule.FileName == currentProcess.MainModule.FileName)
                    {
                        return process;
                    }
                }
            }

            return null;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Process process = RuningInstance();

            if (process != null)
            {
                SetForegroundWindow(process.MainWindowHandle);
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                base.OnStartup(e);
            }
        }

        public static bool MiDeviceIsSupport(string model)
        {
            foreach (string device in  Support_List)
            {
                if (model.Contains(device)) { return true; }
            }

            return false;
        }

        public static void changeLanguage(string language)
        {
            string position = $"Assets/Lang/{language}.xaml";
            Current.Resources.MergedDictionaries[1].Source = new Uri(position, UriKind.Relative);
        }

        public static string getIconBykey(string key)
        {
            ResourceDictionary resource = Current.Resources.MergedDictionaries[0];
            IDictionaryEnumerator enumerator = resource.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Key.Equals(key))
                {
                    return enumerator.Value.ToString();
                }
            }

            return string.Empty;
        }


        public static string getStringbyKey(string key)
        {
            ResourceDictionary resource = Current.Resources.MergedDictionaries[1];
            IDictionaryEnumerator enumerator = resource.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Key.ToString() == key)
                {
                    return enumerator.Value.ToString();
                }
            }

            return string.Empty;
        }
    }
}
