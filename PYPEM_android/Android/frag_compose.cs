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
    public class frag_compose : Fragment
    {
        public string Content { get; set; }
        public string Title { get; set; }
        public string ToPK { get; set; }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_compose, container, false);

            ((EditText)view.FindViewById(Resource.Id.edContent)).Text = Content ?? "";
            ((EditText)view.FindViewById(Resource.Id.edEmailTitle)).Text = Title ?? "";
            ((EditText)view.FindViewById(Resource.Id.edEmail)).Text = ToPK ?? "";

            // Use this to return your custom view for this Fragment
            return view;

            //return base.OnCreateView(inflater, container, savedInstanceState);
        }
    }
}