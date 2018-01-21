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
    class SHA
    {
        public byte[] GetHash(string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);

            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] hashValue = mySHA256.ComputeHash(data);

            return hashValue;
        }

        public string GetSHAString(byte[] data)
        {
            string result = "";
            for (int i = 0; i < data.Length; i++)
                result += String.Format("{0:X2}", data[i]);

            return result;
        }
    }
}