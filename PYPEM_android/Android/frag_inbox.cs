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
    public class frag_inbox : Fragment
    {
        public MainActivity.OnOpenMailDelegate OnOpenMail;
        

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            MainActivity.myPypem.MyContacts.LoadContacts();

            // Create your fragment here
            MainActivity.OnRefreshInbox = this.RefreshEmailList;
            this.RefreshEmailList();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            View view = inflater.Inflate(Resource.Layout.fragment_inbox, container, false);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            
        }

        public void RefreshEmailList()
        {
            this.ClearEmailList();

            MainActivity.myPypem.MyInbox.LoadEmails();
            FragmentTransaction ft = ChildFragmentManager.BeginTransaction();
            List<eMail> emails = MainActivity.myPypem.MyInbox.GetEmails();

            int index = -1;
            foreach (eMail email in emails)
            {
                Contact contactFrom = MainActivity.myPypem.MyContacts.GetContactByPK(email.FromPK);
                Contact contactTo = MainActivity.myPypem.MyContacts.GetContactByPK(email.ToPK);

                string id = "fragment_" + (++index).ToString();
                frag_inbox_entry fragment = new frag_inbox_entry();
                fragment.sFrom = contactFrom.Name ?? email.FromPK;
                fragment.sTo = contactTo.Name ?? email.ToPK;
                fragment.sTime = email.UTCstamp.ToLocalTime().ToString("f");
                fragment.sTitle = email.Title;
                fragment.DeleteCallback = this.DeleteCallback;
                fragment.myEmail = email;

                fragment.OpenMail = this.OpenMail;

                ft.Add(Resource.Id.llEmailList, fragment, id);
                ft.AddToBackStack(id);
            }

            ft.Commit();
        }

        private void OpenMail(eMail email)
        {
            this.OnOpenMail(email);
        }

        private void DeleteCallback()
        {
            this.RefreshEmailList();
        }

        private void ClearEmailList()
        {
            
            ChildFragmentManager.PopBackStack(null, PopBackStackFlags.Inclusive);
        }
    }
}