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

namespace pypem_android
{
    public class frag_decode : Fragment
    {
        public string DecodeData { get; set; }
        public delegate void OnLoadedDecodeDelegate(View v = null);
        public OnLoadedDecodeDelegate OnLoadedDecode;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_decode, container, false);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            if (this.DecodeData != null)
            {
                ((EditText)view.FindViewById(Resource.Id.edDecryptContent)).Text = this.DecodeData;
                this.OnLoadedDecode(view);
                this.DecodeData = null;
            }
        }
    }
}