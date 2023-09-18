using Mi;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace miio
{
    public class XiaomiExtractor
    {
        private string _username;
        private string _password;
        private string _agent;
        private string _sign;
        private string _device_id;

        private string _ssecurity;
        private string userId;
        private string _cUserId;
        private string _passToken;
        private string _location;
        private string _code;

        private string _serviceToken;

        private CookieContainer _cookies;

        public XiaomiExtractor(string username, string password)
        {
            this._username = username;
            this._password = password;
        }

        public OutputDevice GetExtractorDevice()
        {
            if (_username == string.Empty || _password == string.Empty) 
                return default(OutputDevice);

            OutputDevice outputDevice = new OutputDevice();
            outputDevice.success = false;

            generate_agent();
            generate_device_id();

            _cookies = new CookieContainer();
            _cookies.Add(new Cookie("sdkVersion", "accountsdk-18.8.15", "/", "mi.com"));
            _cookies.Add(new Cookie("sdkVersion", "accountsdk-18.8.15", "/", "xiaomi.com"));
            _cookies.Add(new Cookie("deviceId", _device_id, "/", "mi.com"));
            _cookies.Add(new Cookie("deviceId", _device_id, "/", "xiaomi.com"));

            if (login_step_1() != null)
            {
                if (login_step_2() != null)
                {
                    if (login_step_3())
                    {
                        string current_server = "cn";

                        List<(long, long)> hh = new List<(long, long)>();

                        List<ExtractorDevice> extractors = new List<ExtractorDevice>();

                        try
                        {
                            JObject homes = get_homes(current_server);
                            if (homes != null)
                            {
                                foreach (var h in homes["result"]["homelist"])
                                {
                                    hh.Add((
                                        long.Parse(h["id"].ToString()),
                                        long.Parse(userId)
                                    ));
                                }
                            }

                            foreach (var home in hh)
                            {
                                JObject devices = get_devices(current_server, home.Item1, home.Item2);
                                if (devices != null)
                                {
                                    if (devices["result"]["device_info"] != null && devices["result"]["device_info"].Count() > 0)
                                    {
                                        foreach (JObject device in devices["result"]["device_info"])
                                        {
                                            ExtractorDevice extractorDevice = new ExtractorDevice();

                                            if (device.ContainsKey("name"))
                                            {
                                                extractorDevice.Name = device["name"].ToString();
                                            }

                                            if (device.ContainsKey("mac"))
                                            {
                                                extractorDevice.Mac = device["mac"].ToString();
                                            }

                                            if (device.ContainsKey("localip"))
                                            {
                                                extractorDevice.Ip = device["localip"].ToString();
                                            }

                                            if (device.ContainsKey("token"))
                                            {
                                                extractorDevice.Token = device["token"].ToString();
                                            }

                                            if (device.ContainsKey("model"))
                                            {
                                                extractorDevice.Model = device["model"].ToString();
                                            }

                                            if (device.ContainsKey("did"))
                                            {
                                                extractorDevice.Did = device["did"].ToString();
                                            }

                                            extractors.Add(extractorDevice);

                                            outputDevice.success = true;
                                        }
                                    }
                                    else
                                    {
                                        // 没有设备
                                        continue;
                                    }
                                }
                            }
                        }
                        catch 
                        {
                            outputDevice.success = false;
                            outputDevice.message = App.getStringbyKey("Read_Error");
                            return outputDevice;
                        }

                        outputDevice.devices = extractors;
                        return outputDevice;
                    }
                    else
                    {
                        outputDevice.message = App.getStringbyKey("Cant_Read_Token");
                        return outputDevice;
                    }
                }
                else
                {
                    outputDevice.message = App.getStringbyKey("Account_or_Password_Error");
                    return outputDevice;
                }
            }

            return outputDevice;
        } 

        private JObject get_devices(string country, long home_id, long owner_id)
        {
            string url = get_api_url(country) + "/v2/home/home_device_list";
            JObject param = new JObject()
            {
                new JProperty("data", new JObject()
                {
                    new JProperty("home_owner", owner_id),
                    new JProperty("home_id", home_id),
                    new JProperty("limit", 200),
                    new JProperty("get_split_device", true),
                    new JProperty("support_smart_home", true)
                })
            };

            return execute_api_call_encrypted(url, param);
        }

        private JObject get_homes(string country)
        {
            string url = get_api_url(country) + "/v2/homeroom/gethome";
            JObject param = new JObject()
            {
                new JProperty("data", "{\"fg\": true, \"fetch_share\": true, \"fetch_share_dev\": true, \"limit\": 300, \"app_ver\": 7}")
            };

            return execute_api_call_encrypted(url, param);
        }

        private JObject execute_api_call_encrypted(string url, JObject param)
        {
            HttpWebRequest request = get_current_request(url, "POST");
            request.Accept = "identity";

            request.Headers.Add("x-xiaomi-protocal-flag-cli", "PROTOCAL-HTTP2");
            request.Headers.Add("MIOT-ENCRYPT-ALGORITHM", "ENCRYPT-RC4");

            _cookies.Add(new Cookie("userId", userId, "/", "mi.com"));
            _cookies.Add(new Cookie("yetAnotherServiceToken", _serviceToken, "/", "mi.com"));
            _cookies.Add(new Cookie("serviceToken", _serviceToken, "/", "mi.com"));
            _cookies.Add(new Cookie("locale", "zh_CN", "/", "mi.com"));
            _cookies.Add(new Cookie("timezone", "GMT+08:00", "/", "mi.com"));
            _cookies.Add(new Cookie("is_daylight", "1", "/", "mi.com"));
            _cookies.Add(new Cookie("dst_offset", "3600000", "/", "mi.com"));
            _cookies.Add(new Cookie("channel", "MI_APP_STORE", "/", "mi.com"));

            double millis = Math.Round((double)DateTimeOffset.Now.ToUnixTimeSeconds() * 1000);
            string nonce = generate_nonce(millis);
            string signed_nonce = this.signed_nonce(nonce);
            JObject new_param = generate_enc_params(url, "POST", signed_nonce, nonce, param);

            byte[] data = Encoding.UTF8.GetBytes(ToFormUrlEncoded(new_param));

            try
            {
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string decode = decrypt_rc4(this.signed_nonce(new_param["_nonce"].ToString()), streamReader.ReadToEnd());
                    return to_json(decode);
                }
            }
            catch { }

            return null;
        }

        private string ToFormUrlEncoded(JObject json)
        {
            var keyValuePairs = new List<string>();
            foreach (var pair in json)
            {
                keyValuePairs.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value.ToString())}");
            }

            return string.Join("&", keyValuePairs);
        }

        private string generate_enc_signature(string url, string method, string signed_nonce, JObject param)
        {
            List<string> signature_params = new List<string>
            {
                method.ToUpper(), url.Split(new string[] {"com"}, StringSplitOptions.None)[1].Replace("/app/", "/")
            };

            foreach (var json in param)
            {
                signature_params.Add($"{json.Key}={json.Value}");
            }

            signature_params.Add(signed_nonce);

            string signature_string = string.Join("&", signature_params);

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(signature_string));
                return Base64.ToBase64String(hash);
            }
        }

        private JObject generate_enc_params(string url, string method, string signed_nonce, string nonce , JObject param)
        {
            param["rc4_hash__"] = generate_enc_signature(url, method, signed_nonce, param);
            foreach (var json in param)
            {
                param[json.Key] = encrypt_rc4(signed_nonce, json.Value.ToString());
            }

            param["signature"] = generate_enc_signature(url, method, signed_nonce, param);
            param["ssecurity"] = _ssecurity;
            param["_nonce"] = nonce;

            return param;
        }

        private string encrypt_rc4(string password, string payload)
        {
            byte[] passwordBytes = Base64.Decode(password);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            RC4Engine rc4 = new RC4Engine();
            rc4.Init(true, new KeyParameter(passwordBytes));

            byte[] Array = new byte[1024];
            rc4.ProcessBytes(Array, 0, Array.Length, new byte[1024], 0);

            byte[] encryptedData = new byte[payloadBytes.Length];
            rc4.ProcessBytes(payloadBytes, 0, payloadBytes.Length, encryptedData, 0);

            return Base64.ToBase64String(encryptedData);
        }

        private string decrypt_rc4(string password, string payload)
        {
            byte[] passwordBytes = Base64.Decode(password);
            byte[] payloadBytes = Base64.Decode(payload);

            RC4Engine rc4 = new RC4Engine();
            rc4.Init(false, new KeyParameter(passwordBytes));

            byte[] Array = new byte[1024];
            rc4.ProcessBytes(Array, 0, Array.Length, new byte[1024], 0);

            byte[] decryptedData = new byte[payloadBytes.Length];
            rc4.ProcessBytes(payloadBytes, 0, payloadBytes.Length, decryptedData, 0);

            return Encoding.UTF8.GetString(decryptedData);
        }

        private string generate_nonce(double millis)
        {
            byte[] randomBytes = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            byte[] timestampBytes = BitConverter.GetBytes((int)millis / 60000);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }

            byte[] nonceBytes = randomBytes.Concat(timestampBytes).ToArray();

            return Convert.ToBase64String(nonceBytes, Base64FormattingOptions.None);
        }

        private string signed_nonce(string nonce)
        {
            byte[] ssecurityBytes = Base64.Decode(_ssecurity);
            byte[] nonceBytes = Base64.Decode(nonce);

            byte[] combinedBytes = ssecurityBytes.Concat(nonceBytes).ToArray();

            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(combinedBytes);
            }

            return Base64.ToBase64String(hash);
        }

        private string get_api_url(string country)
        {
            if (country == "cn")
            {
                country = "";
            }
            else
            {
                country += ".";
            }

            return $"https://{country}api.io.mi.com/app";
        }

        private void generate_agent()
        {
            string agent_id = string.Empty;

            Random random = new Random();

            for (int i = 0;i < 13;i++)
            {
                agent_id += random.Next(65, 69);
            }

            _agent = $"Android-7.1.1-1.0.0-ONEPLUS A3010-136-{agent_id} APP/xiaomi.smarthome APPV/62830";
        }

        private void generate_device_id()
        {
            string device_id = string.Empty;

            Random random = new Random();
            
            for (int i = 0;i < 6;i++)
            {
                device_id += random.Next(97, 122);
            }

            _device_id = device_id;
        }

        private JObject login_step_1()
        {
            string url = "https://account.xiaomi.com/pass/serviceLogin?sid=xiaomiio&_json=true";
            try
            {
                HttpWebRequest request = get_current_request(url, "GET");

                _cookies.Add(new Cookie("userId", this._username, "/", "xiaomi.com"));

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader streamReader = new StreamReader(stream);

                    JObject json = to_json(streamReader.ReadToEnd().Substring(11));
                    _sign = json["_sign"].ToString();

                    return json;
                }
            }
            catch { }

            return null;
        }

        private JObject login_step_2()
        {
            string url = "https://account.xiaomi.com/pass/serviceLoginAuth2";

            HttpWebRequest request = get_current_request(url, "POST");

            string postData = $"sid=xiaomiio&" +
                                          $"hash={this._password}&" +
                                          $"callback=https://sts.api.io.mi.com/sts&" +
                                          $"qs=%3Fsid%3Dxiaomiio%26_json%3Dtrue&" +
                                          $"user={this._username}&" +
                                          $"_sign={_sign}&" +
                                          $"_json=true";

            try
            {
                using (Stream stream = request.GetRequestStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes(postData);
                    stream.Write(data, 0, data.Length);

                    stream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                JObject json;
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader streamReader = new StreamReader(responseStream);
                    json = to_json(streamReader.ReadToEnd().Substring(11));
                }

                if (json.ContainsKey("ssecurity") && json["ssecurity"].ToString().Length > 4)
                {
                    _ssecurity = json["ssecurity"].ToString();
                    userId = json["userId"].ToString();
                    _cUserId = json["cUserId"].ToString();
                    _passToken = json["passToken"].ToString();
                    _location = json["location"].ToString();
                    _code = json["code"].ToString();
                }
                else {
                    return null;
                }

                return json;
            }
            catch { }

            return null;
        }

        private bool login_step_3()
        {
            HttpWebRequest request = get_current_request(_location, "GET");

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                foreach (Cookie cookie in response.Cookies)
                {
                    if (cookie.Name  == "serviceToken")
                    {
                        _serviceToken = cookie.Value;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private HttpWebRequest get_current_request(string url, string method)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = method;

            request.UserAgent = _agent;
            request.ContentType = "application/x-www-form-urlencoded";

            request.CookieContainer = _cookies;

            return request;
        }

        public static string get_MD5(string context)
        {
            using (MD5 md5  = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(context));

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString().ToUpper();
            }
        }

        private JObject to_json(string context)
        {
            return JObject.Parse(context);
        }
    }

    public struct ExtractorDevice
    {
        public string Name;
        public string Mac;
        public string Ip;
        public string Token;
        public string Model;
        public string Did;
    }

    public struct OutputDevice
    {
        public bool success;
        public string message;
        public List<ExtractorDevice> devices;
    }
}
