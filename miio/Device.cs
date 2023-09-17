using System;
using System.Text.Json.Nodes;
using tuya;

namespace miio
{
    [Serializable]
    public class Device
    {
        public Device_Type device_Type;

        // 设备名称
        public string Name = string.Empty;
        // 设备IP地址
        public string address = string.Empty;
        // 设备型号
        public string model = string.Empty;
        // 设备端口
        public int Port = 54321;
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

        public Device(IntPtr Handle, string Name, string model, string DeviceID, Device_Type type = Device_Type.Screen)
        {
            this.Name = Name;
            this.model = model;
            this.DeviceID = DeviceID;

            this.DeviceHandle = Handle;
        }

        public Device(string ip, string DeviceID, string token, Device_Type type = Device_Type.Mihome, int port = 54321)
        {
            this.address = ip;
            this.Token = token;
            this.Port = port;
            this.DeviceID = DeviceID;

            this.device_Type = type;
        }

        public Device(string m_ClientID, string m_ClientSecret, string m_DeviceID, Device_Type type = Device_Type.Tuya)
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
                case Device_Type.Mihome:
                    return Discover.MihomeDeviceSendMessage(address, Port, Token, timeout, command, param);

                case Device_Type.Tuya:
                    return TuyaDevice.SendMessage(ClientID, ClientSecret, DeviceID, command);
            }

            return null;
        }

        public int get_device_id()
        {
            switch (device_Type)
            {
                case Device_Type.Mihome:
                    return Convert.ToInt32(DeviceID, 16);

                case Device_Type.Tuya:
                    return Convert.ToInt32(DeviceID);

                case Device_Type.Screen:
                    return Convert.ToInt32(DeviceID);
            }

            return 0;
        }

        public string get_info()
        {
            switch (device_Type)
            {
                case Device_Type.Mihome:
                    return SendCommand("miIO.info").value;

                case Device_Type.Tuya:
                    return TuyaDevice.GetDeviceInfo(ClientID, ClientSecret, DeviceID).value;
            }

            return "null";
        }

        public bool is_device_lose()
        {
            switch (device_Type)
            {
                case Device_Type.Mihome:
                    disconnect = !SendCommand("miIO.info").success;
                    return disconnect;

                case Device_Type.Tuya:
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
                case Device_Type.Mihome:
                    string request = SendCommand("miIO.info").value;
                    try
                    {
                        JsonObject json = JsonNode.Parse(request).AsObject();
                        return json["result"]["model"].ToString();
                    }
                    catch { }

                    break;

                case Device_Type.Tuya:
                    return model;
            }

            return "";
        }

        public Device_Type get_device_type()
        {
            return device_Type;
        }

        public enum Device_Type
        {
            Mihome = 0x01,
            Tuya = 0x02,
            Screen = 0x03
        }
    }
}
