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
    public class frag_contacts : Fragment
    {
        public MainActivity.OnClickContactDelegate ContactClickMain;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            MainActivity.myPypem.MyContacts.RefreshContacts = this.RefreshContactList;
            this.RefreshContactList();

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            View v = inflater.Inflate(Resource.Layout.fragment_contacts, container, false);

            return v;
            //return base.OnCreateView(inflater, container, savedInstanceState);
        }

        public void RefreshContactList()
        {
            this.ClearContactList();

            MainActivity.myPypem.MyContacts.LoadContacts();
            FragmentTransaction ft = ChildFragmentManager.BeginTransaction();
            List<Contact> contacts = MainActivity.myPypem.MyContacts.GetContacts();

            int index = -1;
            foreach (Contact contact in contacts)
            {
                string id = "fragment_" + (++index).ToString();
                frag_entry fragment = new frag_entry();
                fragment.tvIDs = string.Format("ID:{0}", contact.Name);
                fragment.tvPKs = string.Format("PK:{0}", contact.PublicKey);
                fragment.MyContact = contact;
                fragment.ContactID = contact.ID;
                fragment.DeleteCallback = this.DeleteCallback;
                fragment.ContactClick = this.ContactClickMain;

                ft.Add(Resource.Id.llContactsList, fragment, id);
                ft.AddToBackStack(id);
            }
            
            ft.Commit();
        }
        
        private void DeleteCallback()
        {
            this.RefreshContactList();
        }

        private void ClearContactList()
        {
            /*ChildFragmentManager.ExecutePendingTransactions();

            while (ChildFragmentManager.BackStackEntryCount > 0)
                ChildFragmentManager.PopBackStackImmediate();

            ChildFragmentManager.ExecutePendingTransactions();*/

            ChildFragmentManager.PopBackStack(null, PopBackStackFlags.Inclusive);
        }

        [Export("OnTestAButtonClick")]
        public void OnTestAButtonClick(View v)
        {
            
        }
    }
}