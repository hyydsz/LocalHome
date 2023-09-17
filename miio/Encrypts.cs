using System;
using System.Security.Cryptography;
using System.Text;

namespace miio
{
    internal class Encrypts
    {
        public static string Encrypt(string toEncrypt, string token)//加密
        {
            // Key = MD5(Token)
            // IV = MD5(Key + Token)

            byte[] keyArray = GetHexMD5(token);
            byte[] ivArray = GetHexMD5(byteToHexStr(keyArray) + token);
            byte[] toEncryptArray = Encoding.ASCII.GetBytes(toEncrypt);

            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Key = keyArray;
            aes.IV = ivArray;

            ICryptoTransform transform = aes.CreateEncryptor();
            byte[] cipherBytes = transform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return BitConverter.ToString(cipherBytes).Replace("-", "");
        }

        public static string Decrypt(string toDecrypt, string token)//解密
        {
            // Key = MD5(Token)
            // IV = MD5(Key + Token)

            byte[] keyArray = GetHexMD5(token);
            byte[] ivArray = GetHexMD5(byteToHexStr(keyArray) + token);
            byte[] toDecryptArray = strToHexByte(toDecrypt);

            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Key = keyArray;
            aes.IV = ivArray;
            ICryptoTransform transform = aes.CreateDecryptor();

            try
            {
                byte[] cipherBytes = transform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);
                return Encoding.ASCII.GetString(cipherBytes);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static byte[] GetHexMD5(string myHexString)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = strToHexByte(myHexString);
            byte[] targetData = md5.ComputeHash(fromData);
            return targetData;
        }

        public static byte[] strToHexByte(string hexString)
        {
            hexString = hexString.Replace("\n", "");
            if (hexString.Length % 2 != 0)
            {
                hexString += " ";
            }

            byte[] returnBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return returnBytes;
        }

        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
    }
}
