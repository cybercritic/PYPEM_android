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
using static pypem_android.PYPEM_logic;

namespace pypem_android
{
    public class frag_inbox_entry : Fragment, View.IOnClickListener
    {
        public delegate void OnDeleteDelegate();
        public OnDeleteDelegate DeleteCallback;

        public MainActivity.OnOpenMailDelegate OpenMail;
        
        public string sFrom { get; set; }
        public string sTo { get; set; }
        public string sTime { get; set; }
        public string sTitle { get; set; }
        public eMail myEmail { get; set; }

        private TextView tvDateTime;
        private TextView tvFrom;
        private TextView tvTitle;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            

            // Create your fragment here
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            using (ImageButton btn = (ImageButton)view.FindViewById(Resource.Id.imInboxEmailDelete))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "delete";
            }

            using (RelativeLayout btn = (RelativeLayout)view.FindViewById(Resource.Id.rlInboxEntry))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "open";
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_inbox_entry, container, false); 
            

            this.tvDateTime = (TextView)view.FindViewById(Resource.Id.tvInboxEmailTime);
            this.tvFrom = (TextView)view.FindViewById(Resource.Id.tvInboxEmailFrom);
            this.tvTitle = (TextView)view.FindViewById(Resource.Id.tvInboxEmailTitle);

            this.tvDateTime.Text = this.sTime;
            this.tvTitle.Text = this.sTitle;

            if (this.myEmail.FromPK == MainActivity.myPypem.GetPublicKey() && this.myEmail.ToPK == MainActivity.myPypem.GetPublicKey())
            {
                this.tvDateTime.Text = this.tvDateTime.Text.Insert(0, "[SR] ");
                this.tvFrom.Text = "FromTo: yourself";
            }
            else if (this.myEmail.ToPK == MainActivity.myPypem.GetPublicKey())
            {
                this.tvDateTime.Text = this.tvDateTime.Text.Insert(0, "[R] ");
                this.tvFrom.Text = "From: " + this.sFrom;
            }
            else if (this.myEmail.FromPK == MainActivity.myPypem.GetPublicKey())
            {
                this.tvDateTime.Text = this.tvDateTime.Text.Insert(0, "[S] ");
                this.tvFrom.Text = "To: " + this.sTo;
            }
            else
                this.tvDateTime.Text = this.tvDateTime.Text.Insert(0, "[INVALID] ");

            return view;
        }


        public void OnClick(View v)
        {
            if (v.Tag.ToString() == "delete")
                this.OnDeleteClick();
            else if (v.Tag.ToString() == "open")
                this.OpenMail(this.myEmail);
        }

        private void OnDeleteClick()
        {
            MainActivity.myPypem.MyInbox.DeleteEmail(this.myEmail);
            this.DeleteCallback();
        }
    }
}