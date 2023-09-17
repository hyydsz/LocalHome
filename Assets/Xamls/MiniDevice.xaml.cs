using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using miio;
using Mi.Assets.Devices;
using System.Windows.Media;

namespace Mi.Assets.Xamls
{
    public partial class MiniDevice : UserControl
    {
        public Device device;

        public MiniDevice(Device _device)
        {
            InitializeComponent();

            device = _device;

            // 检查设备是不是失效
            Task.Run(() =>
            {
                try
                {
                    if (device.is_device_lose())
                    {
                        lose.Dispatcher.BeginInvoke(() =>
                        {
                            lose.Visibility = Visibility.Visible;
                        });
                    }
                }
                catch
                {
                    lose.Dispatcher.BeginInvoke(() =>
                    {
                        lose.Visibility = Visibility.Visible;
                    });
                }

                Dispatcher.BeginInvoke(() =>
                {
                    device_name.Text = device.Name;
                });
            });

            if (device.model == null) return;

            if (device.model.Contains("light.lamp"))
            {
                // 台灯图案
                icon.Data = Geometry.Parse(App.getIconBykey("light.lamp"));
            }
            else if (device.model.Contains("插座"))
            {
                icon.Data = Geometry.Parse(App.getIconBykey("Socket"));
            }
            else if (device.model.Contains("Screen"))
            {
                icon.Data = Geometry.Parse(App.getIconBykey("Screen"));
            }
            // 更多支持设备
        }

        // 按下打开控制界面
        private void Click(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.IsFrameMoveing || MainWindow.IsFrameUp) return;

            if(device.model ==  null) return;

            if (device.model.Contains("light.lamp"))
            {
                MainWindow.ActionsetControllContent(new LightLamp(device).Content);
                MainWindow.ActionFrameMove(true, -800, 0, true);
            }

            else if (device.model.Contains("插座"))
            {
                MainWindow.ActionsetControllContent(new Socket(device).Content);
                MainWindow.ActionFrameMove(true, -800, 0, true);
            }

            else if (device.model.Contains("Screen"))
            {
                MainWindow.ActionsetControllContent(new Screen(device).Content);
                MainWindow.ActionFrameMove(true, -800, 0, true);
            }
            // 更多支持设备
        }
    }
}
