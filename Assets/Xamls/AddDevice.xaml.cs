using miio;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using tuya;

namespace Mi.Assets.Xamls
{
    public partial class AddDevice : UserControl
    {
        public bool IsSearchDevice = false;

        private DDCCI.Control.MonitorData[] MonitorDatas;

        public AddDevice()
        {
            MainWindow.ActionFrameMove(true, 500, 60, false);
            Broadcaster.instance.onLanguageChange += onLanguageChange;

            InitializeComponent();
        }

        private void Button_Handle(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            switch (button.Name)
            {
                case "Leave":
                    MainWindow.ActionFrameMove(false, 60, 500, false);
                    break;
                case "Apply":

                    if (IsSearchDevice) return;

                    string Username = username.Text;
                    string Password = password.Text;

                    if(mihome.IsChecked.Value)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            IsSearchDevice = true;

                            try
                            {
                                if (Username == "" || Password == "") return;

                                XiaomiExtractor xiaomi = new XiaomiExtractor(Username, Password);
                                OutputDevice output = xiaomi.GetExtractorDevice();
                                if (output.success)
                                {
                                    MainWindow.saved_data.Devices.RemoveAll(r => r.device_Type == Device.Device_Type.Mihome);
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                         MainWindow.ActionRefreshDevice();
                                    });

                                    foreach (ExtractorDevice device in output.devices)
                                    {
                                        if (device.Ip != null && device.Token != null & device.Model != null)
                                        {
                                            if (!App.MiDeviceIsSupport(device.Model)) continue;

                                            Device device1 = new Device(device.Ip, device.Did, device.Token, Device.Device_Type.Mihome, 54321);
                                            device1.model = device.Model;
                                            device1.Name = device.Name;

                                            MainWindow.saved_data.Devices.Add(device1);

                                            await Dispatcher.InvokeAsync(() =>
                                            {
                                                MainWindow.ActionAddDevice(device1);
                                            });
                                        }
                                    }

                                    await Dispatcher.BeginInvoke(() =>
                                    {
                                        MainWindow.ActionFrameMove(false, 60, 500, false);
                                    });
                                }
                                else
                                {
                                    Error(output.message);
                                }
                            }
                            catch
                            {
                                Error(App.getStringbyKey("Add_Fail_Please_Check_Account"));
                            }

                            IsSearchDevice = false;
                        });

                        break;
                    }

                    else if (tuya.IsChecked.Value)
                    {
                        if (TuyaDevice.GetDeviceInfo(ClientID.Text, ClientSecret.Text, Device_ID.Text).success)
                        {
                            Device device1 = new Device(ClientID.Text, ClientSecret.Text, Device_ID.Text, Device.Device_Type.Tuya);

                            foreach (Device device in MainWindow.saved_data.Devices)
                            {
                                if (device.DeviceID == Device_ID.Text)
                                {
                                    Error(App.getStringbyKey("Add_Fail_Same_DeviceID"));

                                    return;
                                }
                            }

                            device1.is_device_lose();

                            MainWindow.saved_data.Devices.Add(device1);

                            Dispatcher.InvokeAsync(() =>
                            {
                                MainWindow.ActionFrameMove(false, 60, 500, false);
                                MainWindow.ActionAddDevice(device1);
                            });
                        }
                        else
                        {
                            Error(App.getStringbyKey("Add_Fail_Please_Check_Message"));
                        }
                    }

                    else if (Screen.IsChecked.Value)
                    {
                        if (ScreenList.SelectedItems.Count == 0) return;

                        DDCCI.Control.MonitorData data = MonitorDatas[ScreenList.SelectedIndex];

                        Device device1 = new Device(data.MonitorHandle, ScreenList.SelectedItem.ToString(), "Screen", ScreenList.SelectedIndex.ToString());

                        foreach (Device device in MainWindow.saved_data.Devices)
                        {
                            if (device.DeviceHandle.Equals(device1.DeviceHandle))
                            {
                                Error(App.getStringbyKey("Add_Fail_Same_Handle"));

                                return;
                            }
                        }

                        MainWindow.saved_data.Devices.Add(device1);

                        Dispatcher.InvokeAsync(() =>
                        {
                            MainWindow.ActionFrameMove(false, 60, 500, false);
                            MainWindow.ActionAddDevice(device1);
                        });
                    }

                    break;
            }
        }

        private void Error(string message)
        {
            Task.Factory.StartNew(() =>
            {
                Error_Info.Dispatcher.BeginInvoke(() =>
                {
                    Error_Info.Text = message;

                    Error_Info.Visibility = Visibility.Visible;
                });

                Thread.Sleep(2000);

                Error_Info.Dispatcher.BeginInvoke(() =>
                {
                    Error_Info.Visibility = Visibility.Hidden;
                });
            });
        }

        private void Choose_Handle(object sender, RoutedEventArgs e)
        {
            Screen.IsChecked = false;
            tuya.IsChecked = false;
            mihome.IsChecked = false;

            (sender as ToggleButton).IsChecked = true;

            if(tuya.IsChecked.Value)
            {
                Mihome_Info.Visibility = Visibility.Collapsed;
                Tuya_Info.Visibility = Visibility.Visible;
                Screen_Info.Visibility = Visibility.Collapsed;
            }

            if (mihome.IsChecked.Value)
            {
                Mihome_Info.Visibility = Visibility.Visible;
                Tuya_Info.Visibility = Visibility.Collapsed;
                Screen_Info.Visibility = Visibility.Collapsed;
            }

            if (Screen.IsChecked.Value)
            {
                Mihome_Info.Visibility = Visibility.Collapsed;
                Tuya_Info.Visibility = Visibility.Collapsed;
                Screen_Info.Visibility = Visibility.Visible;

                ScreenList.Items.Clear();

                MonitorDatas = DDCCI.Control.Get_Devices();

                for(int i = 0;i < MonitorDatas.Length;i++)
                {
                    ScreenList.Items.Add($"{App.getStringbyKey("Text_Screen")} {i}");
                }
            }
        }

        private void onLanguageChange(object? sender, string e)
        {
            if (Screen.IsChecked.Value)
            {
                ScreenList.Items.Clear();

                MonitorDatas = DDCCI.Control.Get_Devices();

                for (int i = 0; i < MonitorDatas.Length; i++)
                {
                    ScreenList.Items.Add($"{App.getStringbyKey("Text_Screen")} {i}");
                }
            }
        }
    }
}
