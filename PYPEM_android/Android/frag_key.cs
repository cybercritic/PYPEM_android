using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Interop;

namespace pypem_android
{
    public class frag_key : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            View v = inflater.Inflate(Resource.Layout.fragment_key, container, false);

            TextView tvPublic = (TextView)v.FindViewById(Resource.Id.tvPublicKey);
            tvPublic.Text = MainActivity.myPypem.GetPublicKey();

            //return base.OnCreateView(inflater, container, savedInstanceState);

            return v;
        }

        
    }
}