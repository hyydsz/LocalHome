using Mi.Assets;
using Mi.Assets.Xamls;
using miio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;   
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Mi
{
    public partial class MainWindow : Window
    {
        public static SavedData saved_data = new SavedData();

        public static bool IsFrameMoveing = false;
        public static bool IsFrameUp = false;

        public static Action<bool, double, double, bool> ActionFrameMove;
        public static Action<object> ActionsetControllContent;
        public static Action ActionRefreshDevice;
        public static Action<Device> ActionAddDevice;

        public MainWindow()
        {
            InitializeComponent();

            ActionFrameMove = FrameMove;
            ActionsetControllContent = setControllContent;
            ActionRefreshDevice = RefreshDevice;
            ActionAddDevice = Add_Device;

            // 读取保存的信息
            if (File.Exists(@"C:\ProgramData\LocalHome\Devices.data"))
            {
                using (FileStream fs = new FileStream(@"C:\ProgramData\LocalHome\Devices.data", FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    saved_data = (SavedData)formatter.Deserialize(fs);
                }
            }

            // 当动画完成后的回调
            (UpFrame.Resources["UpAnimation"] as Storyboard).Completed += FrameCompleted;
            (ControllFrame.Resources["RightAnimation"] as Storyboard).Completed += FrameCompleted;

            if (saved_data.Devices == null)
            {
                saved_data.Devices = new List<Device>();
            }

            if (saved_data.Language == null)
            {
                saved_data.Language = "Chinese";
            }

            foreach (ComboBoxItem item in SelectLanguage.Items)
            {
                if (item.Content.ToString() == saved_data.Language)
                {
                    SelectLanguage.SelectedItem = item;
                    App.changeLanguage(item.Content.ToString());

                    break;
                }
            }

            // 将设备放到控件上
            foreach (Device device in saved_data.Devices)
            {
                DeviceList.Inlines.Add(new MiniDevice(device));
                DeviceList.Inlines.Add(new Border { Height = 100 });
            }
        }

        private void Drag(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Button_Handle(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (IsFrameMoveing || IsFrameUp) return;

            switch (button.Name)
            {
                case "add_Device":
                    UpFrame.Content = new AddDevice();
                    break;

                case "Reload":
                    RefreshDevice();
                    break;

                case "DeleteAll":
                    saved_data.Devices.Clear();
                    RefreshDevice();
                    break;
            }
        }

        private void Title_Button_Handle(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            switch (button.Name)
            {
                case "exit":
                    Application.Current.Shutdown();
                    break;

                case "mini":
                    WindowState = WindowState.Minimized;
                    break;
            }
        }

        // 设置控制页面的Content
        public void setControllContent(object Content)
        {
            ControllFrame.Content = Content;
        }

        // 控制每个界面的动画
        public void FrameMove(bool UpOrDown, double From, double To, bool RightOrUp)
        {
            Storyboard UpAnimation = null;

            if (RightOrUp)
            {
                UpAnimation = ControllFrame.Resources["RightAnimation"] as Storyboard;
            }
            else
            {
                UpAnimation = UpFrame.Resources["UpAnimation"] as Storyboard;
            }

            DoubleAnimation animation = UpAnimation.Children[0] as DoubleAnimation;

            animation.From = From;
            animation.To = To;

            IsFrameUp = UpOrDown;

            IsFrameMoveing = true;

            UpAnimation.Begin();
        }

        public void Write_To_File()
        {
            if (!Directory.Exists(@"C:\ProgramData\LocalHome"))
            {
                Directory.CreateDirectory(@"C:\ProgramData\LocalHome");
            }

            using (FileStream fs = new FileStream(@"C:\ProgramData\LocalHome\Devices.data", FileMode.Create, FileAccess.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, saved_data);
            }
        }

        // 添加设备
        public void Add_Device(Device device)
        {
            Write_To_File();

            DeviceList.Inlines.Add(new MiniDevice(device));
            DeviceList.Inlines.Add(new Border { Height = 100 });
        }

        // 移除设备
        public void RefreshDevice()
        {
            Write_To_File();

            DeviceList.Inlines.Clear();
            foreach (Device device in saved_data.Devices)
            {
                DeviceList.Inlines.Add(new MiniDevice(device));
                DeviceList.Inlines.Add(new Border { Height = 100 });
            }
        }

        // 完成回调
        private void FrameCompleted(object? sender, EventArgs e)
        {
            IsFrameMoveing = false;
        }

        private void ChangeLanguage(object sender, SelectionChangedEventArgs e)
        {
            if (SelectLanguage.IsDropDownOpen)
            {
                ComboBoxItem comboBox = SelectLanguage.SelectedItem as ComboBoxItem;
                string language = comboBox.Content.ToString();

                saved_data.Language = language;
                App.changeLanguage(language);
                Broadcaster.instance.LanguageChange(language);

                Write_To_File();
            }
        }
    }

    [Serializable]
    public struct SavedData
    {
        public List<Device> Devices;
        public string Language;
    }
}
