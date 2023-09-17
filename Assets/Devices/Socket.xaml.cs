using miio;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using tuya;

namespace Mi.Assets.Devices
{
    public partial class Socket : UserControl
    {
        Device device;

        private bool Close = false;

        public Socket(Device _device)
        {
            device = _device;

            InitializeComponent();

            editName.Text = device.Name;

            Task.Run(() =>
            {
                DeviceMessage message = TuyaDevice.GetDeviceStatus(device.ClientID, device.ClientSecret, device.DeviceID);
                if (!message.success)
                {
                    device.disconnect = true;

                    return;
                }

                try
                {
                    JsonObject json = JsonNode.Parse(message.value).AsObject();
                    // switch_1
                    if (json["result"][0]["value"].ToString() == "true")
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            power.IsChecked = true;
                        });
                    }
                }
                catch
                {
                    device.disconnect = true;
                }
            });

            Task.Run(() =>
            {
                bool clicked = false;
                bool m_clicked = false;

                while (Close == false)
                {
                    DeviceMessage message = TuyaDevice.GetDeviceStatus(device.ClientID, device.ClientSecret, device.DeviceID);
                    if (message.success)
                    {
                        try
                        {
                            JsonObject json = JsonNode.Parse(message.value).AsObject();
                            m_clicked = Convert.ToBoolean(json["result"][0]["value"].ToString());

                            if(m_clicked != clicked)
                            {
                                clicked = m_clicked;
                                Dispatcher.Invoke(() =>
                                {
                                    power.IsChecked = m_clicked;
                                });
                            }
                        }
                        catch { }
                    }

                    Thread.Sleep(2000);
                }

            });
        }

        private void Button_Handle(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            switch (button.Name)
            {
                case "Leave":
                    Close = true;
                    MainWindow.ActionFrameMove(false, 0, -800, true);
                    break;

                case "delete":
                    MainWindow.saved_data.Devices.Remove(device);
                    MainWindow.ActionRefreshDevice();

                    Close = true;
                    MainWindow.ActionFrameMove(false, 0, -800, true);

                    break;

                case "saveName":
                    TuyaDevice.SetDeviceName(device.ClientID, device.ClientSecret, device.DeviceID, editName.Text);
                    Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        Dispatcher.BeginInvoke(() =>
                        {
                            MainWindow.ActionRefreshDevice();
                        });
                    });

                    break;
            }
        }

        private void power_Click(object sender, RoutedEventArgs e)
        {
            if (device.disconnect) return;

            JsonObject json = new JsonObject()
            {
                {"commands", new JsonArray()
                {
                    new JsonObject()
{                       {"code", "switch_1"},
                        {"value", power.IsChecked.Value }
                    } } }
            };

            device.SendCommand(json.ToString());
        }
    }
}
