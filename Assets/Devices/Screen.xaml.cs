using miio;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Mi.Assets.Devices
{
    public partial class Screen : UserControl
    {
        public Device device;

        public bool IsSending = false, IsInit = false;

        public Screen(Device _device)
        {
            device = _device;

            InitializeComponent();

            editName.Text = device.Name;

            Task.Run(() =>
            {
                try
                {
                    device.DeviceHandle = DDCCI.Control.Get_Devices()[device.get_device_id()].MonitorHandle;

                    double bright = DDCCI.Control.GetBrightness(device.DeviceHandle) * 100;

                    // 初始化
                    Bright.Dispatcher.BeginInvoke(() =>
                    {
                        Bright.Value = bright;
                    });

                    IsInit = true;
                }
                catch
                {
                    device.disconnect = true;
                }
            });
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

        private void MouseUp_Handle(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;

            switch (slider.Name)
            {
                case "Bright":
                    DDCCI.Control.SetBrightness(device.DeviceHandle, (int) slider.Value);

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
                    Bright_text.Text = $"{App.getStringbyKey("brightness")} | {(int)slider.Value} % ";

                    if (IsSending) return;

                    int bright = (int)slider.Value;

                    Task.Run(() =>
                    {
                        IsSending = true;
                        DDCCI.Control.SetBrightness(device.DeviceHandle, bright);
                        IsSending = false;
                    });

                    break;
            }
        }
    }
}
