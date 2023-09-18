using Mi.Assets;
using Mi.Assets.Xamls;
using miio;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            Read_To_File();

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
                    Read_To_File();
                    RefreshDevice();
                    break;

                case "DeleteAll":
                    saved_data.Devices.Clear();
                    RefreshDevice();
                    Read_To_File();
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
            Storyboard UpAnimation;

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

        private void Read_To_File()
        {
            // 读取保存的信息
            if (File.Exists(@"C:\ProgramData\LocalHome\Devices.bin"))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(@"C:\ProgramData\LocalHome\Devices.bin", FileMode.Open, FileAccess.Read))
                    using (MemoryStream read = new MemoryStream())
                    {
                        fileStream.CopyTo(read);
                        byte[] data = read.ToArray();

                        using (Aes aes = Aes.Create())
                        {
                            aes.KeySize = 128;
                            aes.Key = Encoding.UTF8.GetBytes("LocalHomeDataKey");
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            aes.IV = data.Take(16).ToArray();

                            ICryptoTransform cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);

                            using (MemoryStream inputStream = new MemoryStream(data.Skip(16).ToArray()))
                            using (CryptoStream cryptoStream = new CryptoStream(inputStream, cryptoTransform, CryptoStreamMode.Read))
                            using (MemoryStream outputStream = new MemoryStream())
                            {
                                cryptoStream.CopyTo(outputStream);

                                string json_data = Encoding.UTF8.GetString(Base64.Decode(outputStream.ToArray()));
                                saved_data = JsonConvert.DeserializeObject<SavedData>(json_data);

                                ReadFailed.Visibility = Visibility.Collapsed;   
                            }
                        }
                    }
                }
                catch
                {
                    ReadFailed.Visibility = Visibility.Visible;
                }
            }
        }

        public void Write_To_File()
        {
            if (!Directory.Exists(@"C:\ProgramData\LocalHome"))
            {
                Directory.CreateDirectory(@"C:\ProgramData\LocalHome");
            }

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.Key = Encoding.UTF8.GetBytes("LocalHomeDataKey");
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream encryptedStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                {
                    byte[] json_data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(saved_data));

                    cryptoStream.Write(Base64.Encode(json_data));
                    cryptoStream.FlushFinalBlock();

                    using (FileStream file = new FileStream(@"C:\ProgramData\LocalHome\Devices.bin", FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        file.SetLength(0);
                        file.Write(aes.IV.Concat(encryptedStream.ToArray()).ToArray());
                    }
                }
            }
        }

        // 添加设备
        public void Add_Device(Device device)
        {
            Write_To_File();

            DeviceList.Inlines.Add(new MiniDevice(device));
            DeviceList.Inlines.Add(new Border { Height = 100 });
        }

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
    public class SavedData
    {
        public List<Device> Devices;
        public string Language;
    }
}
