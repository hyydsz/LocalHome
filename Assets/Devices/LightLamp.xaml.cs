using miio;
using System;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Mi.Assets.Devices
{
    public partial class LightLamp : UserControl
    {
        public Device device;

        public Storyboard hide;
        public bool IsSending = false, IsInit = false;

        public LightLamp(Device _device)
        {
            device = _device;

            InitializeComponent();

            editName.Text = device.Name;

            hide = Resources["Hide"] as Storyboard;
            hide.Completed += Hide_Completed;

            if (device.disconnect)
            {
                return;
            }

            Broadcaster.instance.onLanguageChange += onLanguageChange;

            Task.Run(() =>
            {
                DeviceMessage message = device.SendCommand("get_prop", "[\"power\", \"bright\", \"ct\"]");
                if (!message.success)
                {
                    device.disconnect = true;
                    return;
                }

                try
                {
                    JsonObject json = JsonNode.Parse(message.value).AsObject();
                    if (json["result"][0].ToString() == "on")
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            power.IsChecked = true;
                            power.CommandParameter = App.getStringbyKey("has_been_on");

                            Hide.Visibility = Visibility.Hidden;
                        });
                    }

                    Dispatcher.BeginInvoke(() =>
                    {
                        Bright.Value = int.Parse(json["result"][1].ToString());
                        CT.Value = int.Parse(json["result"][2].ToString());
                    });

                    IsInit = true;
                }
                catch
                {
                    device.disconnect = true;
                }
            });
        }

        private void onLanguageChange(object? sender, string e)
        {
             if (power.IsChecked.Value)
            {
                power.CommandParameter = App.getStringbyKey("has_been_on");
            }
             else
            {
                power.CommandParameter = App.getStringbyKey("has_been_off");
            }

            Bright_text.Text = $"{App.getStringbyKey("brightness")} | {(int)Bright.Value} %";
            CT_text.Text = $"{App.getStringbyKey("colortemperature")} | {(int)CT.Value} %";
        }

        private void Hide_Completed(object? sender, EventArgs e)
        {
            if (Hide.Opacity < 0.1)
            {
                Hide.Visibility = Visibility.Hidden;
            }
        }

        private void Button_Handle(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            switch (button.Name)
            {
                case "Leave":
                    MainWindow.ActionFrameMove(false, 0, -800, true);
                    break;

                case "delete":
                    MainWindow.saved_data.Devices.Remove(device);
                    MainWindow.ActionRefreshDevice();

                    MainWindow.ActionFrameMove(false, 0, -800, true);

                    break;

                case "saveName":
                    device.Name = editName.Text;
                    MainWindow.ActionRefreshDevice();

                    break; 
            }
        }

        private void Power_Check(object sender, RoutedEventArgs e)
        {
            if (device.disconnect) return;

            if ((sender as ToggleButton).IsChecked.Value)
            {
                AnimationGo(0.7, 0);

                Task.Run(() =>
                {
                    device.SendCommand("set_power", "[\"on\"]");
                });

                power.CommandParameter = App.getStringbyKey("has_been_on");
            }
            else
            {
                Hide.Visibility = Visibility.Visible;
                AnimationGo(0, 0.7);

                Task.Run(() =>
                {
                    device.SendCommand("set_power", "[\"off\"]");
                });

                power.CommandParameter = App.getStringbyKey("has_been_off");
            }
        }

        // 控制每个界面的动画
        public void AnimationGo(double from, double to)
        {
            DoubleAnimation animation = hide.Children[0] as DoubleAnimation;

            animation.From = from;
            animation.To = to;

            hide.Begin();
        }

        private void MouseUp_Handle(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;

            if (device.disconnect) return;

            switch (slider.Name)
            {
                case "Bright":
                    device.SendCommand("set_bright", "[" + (int)slider.Value + "]");

                    break;

                case "CT":
                    device.SendCommand("set_rgb", "[" + (int)slider.Value + "]");

                    break;
            }
        }

        private void Slider_Handle(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;

            if (!IsInit) return;

            switch (slider.Name)
            {
                case "Bright":
                    Bright_text.Text = $"{App.getStringbyKey("brightness")} | {(int)slider.Value} %";

                    if (IsSending) return;

                    int bright = (int)slider.Value;
                    Task.Run(() =>
                    {
                        IsSending = true;
                        device.SendCommand("set_bright", "[" + bright + "]");
                        IsSending = false;
                    });

                    break;

                case "CT":
                    CT_text.Text = $"{App.getStringbyKey("colortemperature")} | {(int)slider.Value} %";

                    if (IsSending) return;

                    int ct = (int)slider.Value;
                    Task.Run(() =>
                    {
                        IsSending = true;
                        device.SendCommand("set_rgb", "[" + ct + "]");
                        IsSending = false;
                    });

                    break;
            }
        }
    }
}
