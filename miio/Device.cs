using Newtonsoft.Json;
using System;
using System.Text.Json.Nodes;
using tuya;

namespace miio
{
    public class Device
    {
        public const int Type_None = 0;
        public const int Type_Mihome = 1;
        public const int Type_Tuya = 2;
        public const int Type_Screen = 3;

        public int device_Type = Type_None;

        // 账号
        public string account = string.Empty;
        public string password = string.Empty;

        // 设备名称
        public string Name = string.Empty;
        // 设备IP地址
        public string LocalAddress = string.Empty;
        // 设备型号
        public string model = string.Empty;
        // 设备端口
        public int LocalPort = 54321;
        // 设备ID
        public string DeviceID = string.Empty;

        public IntPtr DeviceHandle = new IntPtr();

        // 涂鸦
        public string ClientID = string.Empty;
        public string ClientSecret = string.Empty;

        // 设备Token
        public string Token = string.Empty;

        // 设备是否断开连接
        public bool disconnect = false;

        [JsonConstructor]
        public Device(int type = Type_None)
        {
            this.device_Type = type;    
        }

        public Device(IntPtr Handle, string Name, string model, string DeviceID, int type = Type_Screen)
        {
            this.Name = Name;
            this.model = model;
            this.DeviceID = DeviceID;

            this.DeviceHandle = Handle;
        }

        public Device(string LocalAddress, string DeviceID, string Token, string model, string Name, int type = Type_Tuya, int LocalPort = 54321)
        {
            this.LocalAddress = LocalAddress;
            this.DeviceID = DeviceID;
            this.Token = Token;
            this.model = model;
            this.Name = Name;
            this.LocalPort = LocalPort;

            this.device_Type = type;
        }

        public Device(string m_ClientID, string m_ClientSecret, string m_DeviceID, int type = Type_Mihome)
        {
            ClientID = m_ClientID;
            ClientSecret = m_ClientSecret;
            DeviceID = m_DeviceID;

            device_Type = type;
        }

        // 使用 作者:xcray github开源miio代码

        public DeviceMessage SendCommand(string command, string param = null, int timeout = 2000)
        {
            switch (device_Type)
            {
                case Type_Mihome:
                    return Discover.MihomeDeviceSendMessage(LocalAddress, LocalPort, Token, timeout, command, param);

                case Type_Tuya:
                    return TuyaDevice.SendMessage(ClientID, ClientSecret, DeviceID, command);
            }

            return null;
        }

        public int get_device_id()
        {
            switch (device_Type)
            {
                case Type_Mihome:
                    return Convert.ToInt32(DeviceID, 16);

                case Type_Tuya:
                    return Convert.ToInt32(DeviceID);

                case Type_Screen:
                    return Convert.ToInt32(DeviceID);
            }

            return 0;
        }

        public string get_info()
        {
            switch (device_Type)
            {
                case Type_Mihome:
                    return SendCommand("miIO.info").value;

                case Type_Tuya:
                    return TuyaDevice.GetDeviceInfo(ClientID, ClientSecret, DeviceID).value;
            }

            return "null";
        }

        public bool is_device_lose()
        {
            switch (device_Type)
            {
                case Type_Mihome:
                    disconnect = !SendCommand("miIO.info").success;
                    return disconnect;

                case Type_Tuya:
                    DeviceMessage device = TuyaDevice.GetDeviceInfo(ClientID, ClientSecret, DeviceID);
                    disconnect = !device.success;

                    if(disconnect) return true;

                    try
                    {
                        JsonObject json = JsonNode.Parse(device.value).AsObject();

                        disconnect = !Convert.ToBoolean(json["result"]["online"].ToString());
                        Name = json["result"]["name"].ToString();
                        model = json["result"]["category_name"].ToString();
                    }
                    catch
                    {
                        disconnect = false;
                        return disconnect;   
                    }

                    return disconnect;
            }

            return false;
        }

        public string get_model()
        {
            switch (device_Type)
            {
                case Type_Mihome:
                    string request = SendCommand("miIO.info").value;
                    try
                    {
                        JsonObject json = JsonNode.Parse(request).AsObject();
                        return json["result"]["model"].ToString();
                    }
                    catch { }

                    break;

                case Type_Tuya:
                    return model;
            }

            return "";
        }

        public int get_device_type()
        {
            return device_Type;
        }
    }
}
