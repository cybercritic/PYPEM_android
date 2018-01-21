using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Security.Cryptography;

namespace pypem_android
{
    class RSA
    {
        public void Test()
        {
            string message = "Some message.";

            RSAParameters publicKey, privateKey;
            GenerateKeys(out publicKey,out privateKey);

            byte[] encrypted = RSA.EncyptRSA(Encoding.UTF8.GetBytes(message), publicKey);
            byte[] decrypted = RSA.DecryptRSA(encrypted, privateKey);

            Console.WriteLine(BitConverter.ToString(encrypted));
            Console.WriteLine(Encoding.UTF8.GetString(decrypted));
        }

        public static byte[] SignData(byte[] data, RSAParameters privateKey)
        {
            byte[] result;

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(privateKey);

                result = rsa.SignData(data, new SHA256CryptoServiceProvider());

                //bool test = RSA.VerifyDataHash(data, result, privateKey);
            }

            return result;
        }

        public static bool VerifyDataHash(byte[] data, byte[] signedHash, RSAParameters publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(publicKey);

                return rsa.VerifyData(data, new SHA256CryptoServiceProvider(), signedHash);
            }
        }

        public void GenerateKeys(out RSAParameters publicKey,out RSAParameters privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;

                publicKey = rsa.ExportParameters(false);
                privateKey = rsa.ExportParameters(true);
            }
        }

        public static byte[] EncyptRSA(byte[] data, RSAParameters publicKey)
        {
            byte[] result;

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(publicKey);

                result = rsa.Encrypt(data, true);
            }

            return result;
        }

        public static byte[] DecryptRSA(byte[] data, RSAParameters privateKey)
        {
            byte[] result;

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(privateKey);

                result = rsa.Decrypt(data, true);
            }

            return result;
        }

    }
}