using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text;
using miio;

namespace tuya
{
    public class TuyaDevice
    {
        private static string token_url = "https://openapi.tuyacn.com/v1.0/token?grant_type=1";
        private static string timestamp;

        private static string get_tuya_Token(string clientid, string clientsecret)
        {
            timestamp = Convert.ToString(DateTime.Now.ToUniversalTime().Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks).Substring(0, 13);

            WebRequest web = WebRequest.Create(token_url);
            web.Headers.Add("client_id", clientid);
            web.Headers.Add("sign", get_sign(clientid, clientsecret, get_StringToSign("GET", null, "", "/v1.0/token?grant_type=1")));
            web.Headers.Add("sign_method", "HMAC-SHA256");
            web.Headers.Add("t", timestamp);

            web.Method = "GET";

            web.Timeout = 5000;

            try
            {
                Stream stream = web.GetResponse().GetResponseStream();

                StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                JsonObject json = JsonNode.Parse(reader.ReadToEnd()).AsObject();

                stream.Close();
                reader.Close();

                return json["result"]["access_token"].ToString();
            }
            catch
            {
                return "";
            }
        }

        public static string get_sign(string clientid, string clientsecret, string StringToSign, string nonce = "", string accessToken = null)
        {
            string str = clientid;

            if (accessToken != null) str += accessToken;

            str += timestamp + nonce + StringToSign;

            using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(clientsecret)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(str));
                return Convert.ToHexString(hash);
            }
        }

        public static string get_StringToSign(string Method, byte[] body, string Header, string URL)
        {
            if (body == null)
            {
                return Method + "\n" + "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" + "\n" + Header + "\n" + URL;
            }

            byte[] hash = SHA256.Create().ComputeHash(body);

            return Method + "\n" + Convert.ToHexString(hash).ToLower() + "\n" + Header + "\n" + URL;
        }

        private static DeviceMessage SendToURL(string clientid, string clientsecret, string URL, string data = null, string method = null)
        {
            DeviceMessage deviceMessage = new DeviceMessage();
            deviceMessage.success = false;

            byte[] m_data = null;
            if (data != null)
            {
                m_data = Encoding.UTF8.GetBytes(data);
            }

            string Token = get_tuya_Token(clientid, clientsecret);

            WebRequest web = WebRequest.Create($"https://openapi.tuyacn.com{URL}");

            if (data != null)
            {
                web.Method = "POST";
            }
            else
            {
                web.Method = "GET";
            }

            if (method != null) web.Method = method;

            web.Headers.Add("client_id", clientid);
            web.Headers.Add("sign", get_sign(clientid, clientsecret, get_StringToSign(web.Method, m_data, "", URL), accessToken: Token));
            web.Headers.Add("sign_method", "HMAC-SHA256");
            web.Headers.Add("t", timestamp);
            web.Headers.Add("access_token", Token);

            web.Timeout = 5000;

            try
            {
                if (m_data != null)
                {
                    web.ContentLength = m_data.Length;

                    using (Stream WriteStream = web.GetRequestStream())
                    {
                        WriteStream.Write(m_data, 0, m_data.Length);
                        WriteStream.Close();
                    }
                }

                Stream stream = web.GetResponse().GetResponseStream();

                StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                deviceMessage.value = reader.ReadToEnd();

                try
                {
                    JsonObject json = JsonNode.Parse(deviceMessage.value).AsObject();
                    deviceMessage.success = Convert.ToBoolean(json["success"].ToString());
                }
                catch { }

                stream.Close();
                reader.Close();

                return deviceMessage;
            }
            catch
            {
                return deviceMessage;
            }
        }

        public static DeviceMessage SendMessage(string clientid, string clientsecret, string device_id, string command)
        {
            return SendToURL(clientid, clientsecret, $"/v1.0/iot-03/devices/{device_id}/commands", command);
        }

        public static DeviceMessage GetDeviceInfo(string clientid, string clientsecret, string device_id)
        {
            return SendToURL(clientid, clientsecret, $"/v1.1/iot-03/devices/{device_id}");
        }

        public static DeviceMessage SetDeviceName(string clientid, string clientsecret, string device_id, string name)
        {
            return SendToURL(clientid, clientsecret, $"/v1.0/iot-03/devices/{device_id}", "{\"name\":\"" + name + "\"}", "PUT");
        }

        public static DeviceMessage GetDeviceStatus(string clientid, string clientsecret, string device_id)
        {
            return SendToURL(clientid, clientsecret, $"/v1.0/iot-03/devices/{device_id}/status");
        }
    }
}
