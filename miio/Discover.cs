using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace miio
{
    public class Discover
    {
        private static int id = 0;

        public static Message Run_Discover(IPEndPoint point, int timeout = 5000, bool searchToken = false)
        {
            // 发送hello packet
            byte[] helobytes = Encrypts.strToHexByte("21310020ffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
            var s = new UdpClient();
            s.Client.SendTimeout = timeout;
            s.Client.ReceiveTimeout = timeout;

            Message message = new Message();
            message.success = false;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    s.Send(helobytes, point);
                }
                catch
                {
                    s.Close();

                    return message;
                }
            }

            while (true)
            {
                try
                {

                    byte[] data = s.Receive(ref point);

                    if (data == null) break;

                    // Magic number 0 -- 1 16bit
                    // Packet length 2 -- 3 16bit
                    // Unknown1 4 -- 7 32bit
                    // Device ID 8 -- 11 32bit
                    // Stamp 12 -- 15 32bit
                    // MD5 checksum

                    // 从第8位开始 截取4位 为Device ID

                    string recmes = Encrypts.byteToHexStr(data);

                    if (searchToken)
                    {
                        if (recmes.Substring(32).StartsWith("FFFFFFFFFFFFFFFFFFFFF") || recmes.Substring(32).StartsWith("000000000000000000000"))
                        {
                            Debug.WriteLine("获取自带Token失败");
                            message.find_token = null;

                            return message;
                        }
                        else
                        {
                            message.find_token = recmes.Substring(32, 32);
                        }
                    }

                    message.stamp = int.Parse(recmes.Substring(24, 8), System.Globalization.NumberStyles.HexNumber);
                    message.device_id = recmes.Substring(16, 8);
                    message.success = true;

                    s.Close();

                    return message;
                }
                catch
                {
                    return message;
                }
            }

            return message;
        }

        public static DeviceMessage MihomeDeviceSendMessage(string address, int Port, string Token, int timeout, string command, string param)
        {
            DeviceMessage deviceMessage = new DeviceMessage();
            deviceMessage.success = false;

            IPEndPoint point = new IPEndPoint(IPAddress.Parse(address), Port);

            Message message = Run_Discover(point, searchToken: Token == null);
            if (message.success == false)
            {
                deviceMessage.value = "发送请求失败";
                return deviceMessage;
            }

            if (Token == null)
            {
                if (message.find_token != null)
                {
                    Token = message.find_token;
                }
                else
                {
                    deviceMessage.value = "未找到任何token";

                    return deviceMessage;
                }
            }

            var s = new UdpClient();
            s.Client.SendTimeout = timeout;
            s.Client.ReceiveTimeout = timeout;

            //文档有错，应为2131+包长+00000000+DID+stamp+token，然后是加密串，stamp每次从设备获取后+1
            string stamp = Convert.ToString(message.stamp + 1, 16).PadLeft(8, '0').ToUpper();
            string eneStr = Encrypts.Encrypt(Create_Request(command, param), Token);
            string packlen = Convert.ToString(eneStr.Length / 2 + 32, 16).PadLeft(4, '0');
            string strHeader = "2131" + packlen + "00000000" + message.device_id + stamp;
            string md5sum = Encrypts.byteToHexStr(Encrypts.GetHexMD5(strHeader + Token + eneStr));

            byte[] m_buffer = Encrypts.strToHexByte(strHeader + md5sum + eneStr);

            try
            {
                s.Send(m_buffer, point);
            }
            catch
            {
                s.Close();

                deviceMessage.value = "发送超时";

                return deviceMessage;
            }
            // 发送
            try
            {
                byte[] back = s.Receive(ref point);
                string recmes = Encrypts.byteToHexStr(back);

                s.Close();

                deviceMessage.value = Encrypts.Decrypt(recmes.Substring(64), Token);
                deviceMessage.success = true;

                return deviceMessage;
            }
            catch
            {
                s.Close();

                deviceMessage.value = "连接超时";

                return deviceMessage;
            }
        }

        private static string Create_Request(string command, string param)
        {
            StringBuilder request = new StringBuilder();

            request.Append("{\"id\": ");
            request.Append(get_id() + ", \"method\": \"" + command + "\"");

            if (param != null)
            {
                request.Append(", \"params\": ");
                request.Append(param);
            }

            request.Append("}");

            return request.ToString();
        }

        private static int get_id()
        {
            id++;
            if (id >= 9999) id = 0;

            return id;
        }
    }

    [Serializable]
    public class Message
    {
        public string device_id;
        public bool success;
        public int stamp;
        public string find_token;
    }

    public class DeviceMessage
    {
        public string value;
        public bool success;
    }
}
