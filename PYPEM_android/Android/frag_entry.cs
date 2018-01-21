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
    public class frag_entry : Fragment, View.IOnClickListener
    {
        public TextView tvID { get; private set; }
        public TextView tvPK { get; private set; }

        public string tvIDs { get; set; }
        public string tvPKs { get; set; }
        public int ContactID { get; set; }
        public Contact MyContact { get; set; }

        public delegate void OnDeleteDelegate();
        public OnDeleteDelegate DeleteCallback;
        public MainActivity.OnClickContactDelegate ContactClick;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            //text sinit
            View view = inflater.Inflate(Resource.Layout.fragment_entry, container, false);
            this.tvID = (TextView)view.FindViewById(Resource.Id.tvID);
            this.tvPK = (TextView)view.FindViewById(Resource.Id.tvPK);

            this.tvID.Text = this.tvIDs;
            this.tvPK.Text = this.tvPKs;
            
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            using (ImageButton btn = (ImageButton)view.FindViewById(Resource.Id.imContactEditID))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "edit";
            }

            using (ImageButton btn = (ImageButton)view.FindViewById(Resource.Id.imContactDelete))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "delete";
            }

            using (ImageButton btn = (ImageButton)view.FindViewById(Resource.Id.imContactCopyPK))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "copy";
            }

            using (ImageButton btn = (ImageButton)view.FindViewById(Resource.Id.imContactShare))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "share";
            }

            using (RelativeLayout btn = (RelativeLayout)view.FindViewById(Resource.Id.rlContactEntry))
            {
                btn.SetOnClickListener(this);
                btn.Tag = "open";
            }
        }

        public void OnClick(View v)
        {
            if (v.Tag.ToString() == "edit")
                this.OnEditClick();
            else if (v.Tag.ToString() == "delete")
                this.OnDeleteClick();
            else if (v.Tag.ToString() == "copy")
                this.OnCopyClick();
            else if (v.Tag.ToString() == "share")
                this.OnShareClick();
            else if (v.Tag.ToString() == "open")
                this.ContactClick(this.MyContact);
        }

        private void OnEditClick()
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this.Context);

            EditText input = new EditText(this.Context);
            input.Text = this.tvIDs.Remove(0, this.tvIDs.IndexOf(':') + 1);
            input.TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;
            input.SetSingleLine();

            builder.SetView(input);

            builder.SetTitle("EDIT NAME/ALIAS")
                   .SetMessage("Enter the contact's name/alias")
                   .SetPositiveButton("DONE", delegate
                   {
                       Contact newContact = MainActivity.myPypem.MyContacts.GetContactByID(this.ContactID);
                       newContact.Name = input.Text;

                       MainActivity.myPypem.MyContacts.EditContact(newContact);
                   })
                   .SetNegativeButton("CANCEL", delegate { });

            builder.Create().Show();
        }

        private void OnDeleteClick()
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this.Context);

            builder.SetTitle("DELETE CONTACT?")
                   .SetMessage("Are you sure you want to delete this contact?")
                   .SetPositiveButton("DELETE", delegate {
                       MainActivity.myPypem.MyContacts.DeleteContact(this.ContactID);
                       this.DeleteCallback();
                   })
                   .SetNegativeButton("CANCEL", delegate { });

            builder.Create().Show();
        }

        private void OnCopyClick()
        {
            ClipboardManager clipboard = (ClipboardManager)Activity.GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText(this.tvIDs.Remove(0, this.tvIDs.IndexOf(':') + 1), this.tvPKs.Remove(0, this.tvPKs.IndexOf(':') + 1));
            clipboard.PrimaryClip = clip;
        }

        private void OnShareClick()
        {
            String shareBody = "PYPEM:" + this.tvIDs.Remove(0, this.tvIDs.IndexOf(':') + 1) + ":" + this.tvPKs.Remove(0, this.tvPKs.IndexOf(':') + 1);

            Intent sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.SetType("text/plain");
            sharingIntent.PutExtra(Intent.ExtraSubject, "PYPEM CONTACT - " + this.tvIDs);
            sharingIntent.PutExtra(Intent.ExtraText, shareBody);
            StartActivity(Intent.CreateChooser(sharingIntent, "Share contact."));
        }
    }
}