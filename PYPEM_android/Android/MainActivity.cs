using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Java.Interop;
using Android.Support.V7.App;
using System;
using Android.Support.V4.Widget;
using Android.Content;
using Android.Gms.Ads;
using static pypem_android.PYPEM_logic;
using System.Threading;
using System.Threading.Tasks;

//TODO:Link to server and add ads ids
namespace pypem_android
{
    public class MainActivity : AppCompatActivity
    {
        private ListView mDrawerList;
        private DrawerLayout mDrawerLayout;
        private EditText mEditTextEmailCompose;
        private ArrayAdapter<String> mAdapter;
        private AdView mAdView;

        public static PYPEM_logic myPypem = new PYPEM_logic();
        
        public delegate void OnOpenMailDelegate(eMail email);
        public delegate void OnClickContactDelegate(Contact contact);
        public delegate void RefreshInboxDelegate();
        public static RefreshInboxDelegate OnRefreshInbox;

        private int PasswordFailCount;

        private bool IsLink = false;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            mDrawerList = (ListView)FindViewById(Resource.Id.navList);
            mDrawerLayout = (DrawerLayout)FindViewById(Resource.Id.drawer_layout);
            this.addDrawerItems();

            mDrawerList.ItemClick += selectDrawerItem;

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.mainToolbar);
            SetSupportActionBar(myToolbar);

            this.PasswordFailCount = 0;
            this.CheckKeyFile();

            this.ClearFragments();

            //app opened with link
            Android.Net.Uri data = Intent.Data;
            if (data != null && data.PathSegments != null && data.PathSegments.Count > 0)
                this.IsLink = true;
            
            frag_inbox newFragment = new frag_inbox();
            newFragment.OnOpenMail = this.OnOpenMailInbox;
                
            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.Commit();

            FragmentManager.ExecutePendingTransactions();

            //show ads
            mAdView = (AdView)FindViewById(Resource.Id.adView);
            AdRequest adRequest = new AdRequest.Builder().Build();
            mAdView.LoadAd(adRequest);

            myPypem.WarningIssued = this.WarningIssued;
            myPypem.Randomize();
        }

        private void ShowEmailFromLink()
        {
            Android.Net.Uri data = Intent.Data;
            string first = data.GetQueryParameter("d");
            
            frag_decode newFragment = new frag_decode();
            newFragment.DecodeData = first;
            newFragment.OnLoadedDecode = OnDecryptEmailClick;

            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.AddToBackStack(null);
            ft.Commit();
        }

        public override void Finish()
        {
            base.Finish();
        }

        private void ClearFragments()
        {
            FragmentManager.PopBackStack(null, PopBackStackFlags.Inclusive);
            FragmentManager.ExecutePendingTransactions();
        }

        private void CheckKeyFile(bool overwrite = false)
        {
            //this.CheckPassword("qwertyuiopas");
            //return;

            if (overwrite || !MainActivity.myPypem.KeyFileExists())
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                EditText input = new EditText(this);
                input.TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;
             
                builder.SetView(input);
                builder.SetCancelable(false);

                builder.SetTitle("CREATE PASSWORD")
                       .SetMessage("This password will protect your private key, it can NOT be recovered if lost and needs to be AT LEAST 12 characters long. (Creating the RSA key will take a while.)")
                       .SetPositiveButton("DONE", delegate { this.CheckPasswordInitial(input.Text); });

                builder.Create().Show();
            }
            else
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                EditText input = new EditText(this);
                input.TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;
               
                builder.SetView(input);
                builder.SetCancelable(false);

                builder.SetTitle("ENTER PASSWORD")
                       .SetMessage("Please enter your password.")
                       .SetPositiveButton("DONE", delegate { this.CheckPassword(input.Text); });

                builder.Create().Show();
            }
        }

        private void CheckPassword(string password)
        {
            if (!MainActivity.myPypem.LoadKeyFile(password))
            {
                if(++this.PasswordFailCount >= 5)
                {
                    Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                    builder.SetCancelable(false);
                    builder.SetTitle("TOO MANY ATTEMPTS")
                           .SetMessage("Would you like to create a new account? You will LOSE DATA associated with the old public key.")
                           .SetPositiveButton("YES", delegate { this.CheckKeyFile(true); })
                           .SetNegativeButton("CANCEL", delegate { this.PasswordFailCount = 0; this.CheckKeyFile(); });

                    builder.Create().Show();
                }
                else
                    this.CheckKeyFile();
            }
            else
            {
                myPypem.MyContacts.LoadContacts();
                myPypem.MyInbox.LoadEmails();
                MainActivity.OnRefreshInbox();

                if (this.IsLink)
                    this.ShowEmailFromLink();
            }
        }

        public delegate void KeyDoneDelegate(string message, eMail? email);
        Android.Support.V7.App.AlertDialog alert;
        private async void CheckPasswordInitial(string password)
        {
            if (password.Length < 12)
                this.CheckKeyFile();
            else
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                ProgressBar input = new ProgressBar(this);
                builder.SetView(input);
                builder.SetCancelable(false);

                alert = builder.SetTitle("MAKING RSA KEY").SetMessage("This might take a while, don't cancel.").Create();
                alert.Show();

                myPypem.MyContacts.LoadContacts();
                myPypem.MyInbox.LoadEmails();

                Task.Factory.StartNew(() => { MainActivity.myPypem.CreateKeyFile(password, this.KeyDone); });
            }
        }

        private void KeyDone(string message, eMail? email)
        {
            alert.Cancel();

            myPypem.MyContacts.SaveContacts();
            myPypem.MyInbox.SaveEmails();

            MainActivity.OnRefreshInbox();
        }

        private void WarningIssued(string message)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            builder.SetCancelable(false);
            builder.SetTitle("SECURITY WARNING")
                   .SetMessage(message + "\n It has been discarded.")
                   .SetPositiveButton("YES", delegate { });
                   
            builder.Create().Show();
        }
        
        private void OnOpenMailInbox(eMail email)
        {
            frag_decode newFragment = new frag_decode();

            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.AddToBackStack(null);
            ft.Commit();

            FragmentManager.ExecutePendingTransactions();

            ///////////////////////////////////////////////////////
            try
            {
                ((EditText)FindViewById(Resource.Id.edDecryptContent)).Text = email.Content;
                ((TextView)FindViewById(Resource.Id.tvDecFromPK)).Text = email.FromPK;
                ((TextView)FindViewById(Resource.Id.tvDecryptTitle)).Text = email.Title;

                Contact contact = MainActivity.myPypem.MyContacts.GetContactByPK(email.FromPK);
                ((TextView)FindViewById(Resource.Id.tvDecFromName)).Text = contact.Name ?? "[not in contacts]";

                //MainActivity.myPypem.MyInbox.AddEmail(email);
            }
            catch
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                builder.SetTitle("ERROR")
                       .SetMessage("Something went wrong, the input content is not valid or is corrupt.")
                       .SetPositiveButton("PROCEED", delegate { });

                builder.Create().Show();
            }
        }

        private void selectDrawerItem(object sender, AdapterView.ItemClickEventArgs e)
        {
            myPypem.Randomize();

            switch (e.Position)
            {
                case 0:
                    {
                        frag_inbox newFragment = new frag_inbox();
                        newFragment.OnOpenMail = this.OnOpenMailInbox;

                        var ft = FragmentManager.BeginTransaction();
                        ft.Replace(Resource.Id.fragment_container, newFragment);
                        ft.AddToBackStack(null);
                        ft.Commit();
                        
                        mDrawerLayout.CloseDrawer((int)GravityFlags.Left);

                        return;
                    }

                case 1:
                    {
                        frag_compose newFragment = new frag_compose();

                        var ft = FragmentManager.BeginTransaction();
                        ft.Replace(Resource.Id.fragment_container, newFragment);
                        ft.AddToBackStack(null);
                        ft.Commit();

                        mDrawerLayout.CloseDrawer((int)GravityFlags.Left);

                        return;
                    }

                case 2:
                    {
                        frag_decode newFragment = new frag_decode();

                        var ft = FragmentManager.BeginTransaction();
                        ft.Replace(Resource.Id.fragment_container, newFragment);
                        ft.AddToBackStack(null);
                        ft.Commit();

                        mDrawerLayout.CloseDrawer((int)GravityFlags.Left);

                        return;
                    }

                case 3:
                    {
                        frag_key newFragment = new frag_key();
                        
                        var ft = FragmentManager.BeginTransaction();
                        ft.Replace(Resource.Id.fragment_container, newFragment);
                        ft.AddToBackStack(null);
                        ft.Commit();

                        mDrawerLayout.CloseDrawer((int)GravityFlags.Left);

                        return;
                    }

                case 4:
                    {
                        frag_contacts newFragment = new frag_contacts();
                        newFragment.ContactClickMain = this.OnContactClick;

                        var ft = FragmentManager.BeginTransaction();
                        ft.Replace(Resource.Id.fragment_container, newFragment);
                        ft.AddToBackStack(null);
                        ft.Commit();

                        mDrawerLayout.CloseDrawer((int)GravityFlags.Left);

                        return;
                    }

                default:
                    return;
            }
        }

        private void RemoveAllFragments(FragmentManager fragmentManager)
        {
            while (fragmentManager.BackStackEntryCount > 0)
            {
                fragmentManager.PopBackStackImmediate();
            }
        }

        private void addDrawerItems()
        {
            String[] osArray = { "Inbox", "New Mail", "Decode Mail", "Key Actions", "Adress Book" };
            mAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleExpandableListItem1, osArray);
            mDrawerList.Adapter = mAdapter;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);

            MenuInflater inflator = this.MenuInflater;
            inflator.Inflate(Resource.Menu.main_menu, menu);

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_main_setting:
                    mDrawerLayout.OpenDrawer((int)GravityFlags.Left);
                    // User chose the "Settings" item, show the app settings UI...
                    return true;
                    
                default:
                    // If we got here, the user's action was not recognized.
                    // Invoke the superclass to handle it.
                    return base.OnOptionsItemSelected(item);
            }
        }

        //TODO:Open new mail with contact
        public void OnContactClick(Contact contact)
        {
            frag_compose newFragment = new frag_compose();
            newFragment.Content = "";
            newFragment.Title = "";
            newFragment.ToPK = contact.PublicKey;

            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.AddToBackStack(null);
            ft.Commit();

            return;
        }

        [Export("OnCreateRSAKeyClick")]
        public void OnCreateRSAKeyClick(View v)
        {
            var builder = new Android.Support.V7.App.AlertDialog.Builder(v.Context);

            builder.SetTitle("WARNING")
                   .SetMessage("This will replace your current key file with a new one, it's NOT REVERSABLE, you will not be able to access any mail associated with the current key file.")
                   .SetPositiveButton("PROCEED", delegate { this.CheckKeyFile(true); })
                   .SetNegativeButton("STOP", delegate { Console.WriteLine("Abort new key file."); });

            builder.Create().Show();
        }

        [Export("OnCopyKeyToClipboard")]
        public void OnCopyKeyToClipboard(View v)
        {
            this.CopyToClipboard("PYPEM PK", MainActivity.myPypem.GetPublicKey());
        }

        [Export("OnCopyComposedMailToClipboard")]
        public void OnCopyComposedMailToClipboard(View v)
        {
            EditText to_pk = (EditText)FindViewById(Resource.Id.edEmail);
            EditText content = (EditText)FindViewById(Resource.Id.edContent);
            EditText title = (EditText)FindViewById(Resource.Id.edEmailTitle);

            //PYPEM_logic.eMail decryptedMail = MainActivity.pypem.RecieveEmail(encryptedMail);

            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("STRIP INFO")
                       .SetMessage(Android.Text.Html.FromHtml("Remove recipient public key from final message? In most cases you should select <b>NO</b>, that way the recipient can recognize the message is for them when the message is left in the public domain."))
                       .SetPositiveButton("NO", delegate { this.CopyToClipboard("PYPEM", this.SendEmail(to_pk.Text, content.Text, title.Text, false)); })
                       .SetNegativeButton("YES", delegate { this.CopyToClipboard("PYPEM", this.SendEmail(to_pk.Text, content.Text, title.Text, true)); });

            builder.Create().Show();
        }

        [Export("OnShareComposedMail")]
        public void OnShareComposedMail(View v)
        {
            EditText to_pk = (EditText)FindViewById(Resource.Id.edEmail);
            EditText content = (EditText)FindViewById(Resource.Id.edContent);
            EditText title = (EditText)FindViewById(Resource.Id.edEmailTitle);

            if (to_pk.Text == null || to_pk.Text.Length == 0)
                return;

            //PYPEM_logic.eMail decryptedMail = MainActivity.pypem.RecieveEmail(encryptedMail);

            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("STRIP INFO")
                       .SetMessage(Android.Text.Html.FromHtml("Remove recipient public key from final message? In most cases you should select <b>NO</b>, that way the recipient can recognize the message is for them when the message is left in the public domain."))
                       .SetPositiveButton("NO", delegate { this.ShareWarning("http://pypem.com/app?d=" + Java.Net.URLEncoder.Encode(this.SendEmail(to_pk.Text, content.Text, title.Text, true),"utf-8")); })
                       .SetNegativeButton("YES", delegate { this.ShareText("PYPEM EMAIL", this.SendEmail(to_pk.Text, content.Text, title.Text, true), "PYPEM EMAIL"); });

            builder.Create().Show();
        }

        private void ShareWarning(string data)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            builder.SetTitle("WARNING")
                   .SetMessage("The message you are about to share is too long for some apps and might break the message. Share using an app that doesn't have a message length limit.")
                   .SetPositiveButton("PROCEED", delegate { this.ShareText("PYPEM EMAIL", data, "PYPEM EMAIL"); });

            builder.Create().Show();

        }

        private void CopyToClipboard(string label, string text)
        {
            ClipboardManager clipboard = (ClipboardManager)GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText(label ?? "", text ?? "");
            clipboard.PrimaryClip = clip;
        }

        private string SendEmail(string to_pk, string content,string title, bool strip)
        {
            try
            {
                return MainActivity.myPypem.SendEmail(to_pk, content, title, false);
            }
            catch
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                builder.SetTitle("ERROR")
                       .SetMessage("Something went wrong, check that the recipient email/public key is in correct format.")
                       .SetPositiveButton("PROCEED", delegate { });

                builder.Create().Show();
            }

            return null;
        }

        [Export("OnSharePublicKey")]
        public void OnSharePublicKey(View v)
        {
            this.ShareText("PYPEM PK", MainActivity.myPypem.GetPublicKey(), "Share PYPEM email address/public key.");
        }

        private void ShareText(string subject, string content, string title)
        {
            String shareBody = MainActivity.myPypem.GetPublicKey();

            Intent sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.SetType("text/plain");
            sharingIntent.PutExtra(Intent.ExtraSubject, subject);
            sharingIntent.PutExtra(Intent.ExtraText, content);
            StartActivity(Intent.CreateChooser(sharingIntent, title));
        }

        [Export("OnPastePublicKeyCompose")]
        public void OnPastePublicKeyCompose(View v)
        {
            mEditTextEmailCompose = (EditText)FindViewById(Resource.Id.edEmail);

            ClipboardManager clipboard = (ClipboardManager)GetSystemService(Context.ClipboardService);
            String copiedText = clipboard.Text;

            mEditTextEmailCompose.Text = copiedText;
        }

        [Export("OnDecryptEmailClick")]
        public void OnDecryptEmailClick(View v)
        {
            string data = ((EditText)FindViewById(Resource.Id.edDecryptContent)).Text;

            try
            {
                PYPEM_logic.eMail email = MainActivity.myPypem.RecieveEmail(data) ?? new eMail();
                ((EditText)FindViewById(Resource.Id.edDecryptContent)).Text = email.Content;
                ((TextView)FindViewById(Resource.Id.tvDecFromPK)).Text = email.FromPK;
                ((TextView)FindViewById(Resource.Id.tvDecryptTitle)).Text = email.Title;

                Contact contact = MainActivity.myPypem.MyContacts.GetContactByPK(email.FromPK);
                ((TextView)FindViewById(Resource.Id.tvDecFromName)).Text = contact.Name ?? "[not in contacts]";

                MainActivity.myPypem.MyInbox.AddEmail(email);
            }
            catch
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                builder.SetTitle("ERROR")
                       .SetMessage("Something went wrong, the input content is not valid or is corrupt.")
                       .SetPositiveButton("PROCEED", delegate { });

                builder.Create().Show();
            }
        }

        [Export("OnForwardEmailClick")]
        public void OnForwardEmailClick(View v)
        {
            frag_compose newFragment = new frag_compose();
            newFragment.Content = ((EditText)FindViewById(Resource.Id.edDecryptContent)).Text;
            newFragment.Content += "\n--------\n";
            newFragment.Title = ((TextView)FindViewById(Resource.Id.tvDecryptTitle)).Text;

            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.AddToBackStack(null);
            ft.Commit();

            return;
        }

        [Export("OnReplyEmailClick")]
        public void OnReplyEmailClick(View v)
        {
            frag_compose newFragment = new frag_compose();
            newFragment.Content = ((EditText)FindViewById(Resource.Id.edDecryptContent)).Text;
            newFragment.Content += "\n--------\n";
            newFragment.Title = ((TextView)FindViewById(Resource.Id.tvDecryptTitle)).Text;
            newFragment.ToPK = ((TextView)FindViewById(Resource.Id.tvDecFromPK)).Text;
            
            var ft = FragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.fragment_container, newFragment);
            ft.AddToBackStack(null);
            ft.Commit();

            return;
        }

        [Export("OnAddContactEmailClick")]
        public void OnAddContactEmailClick(View v)
        {
            this.ContactCreateHelper(((TextView)FindViewById(Resource.Id.tvDecFromPK)).Text, v);
        }


        [Export("OnAddNewContact")]
        public void OnAddNewContact(View v)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            EditText input = new EditText(this);
            input.TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;
            input.SetSingleLine();

            builder.SetView(input);

            builder.SetTitle("CONTACT PUBLIC KEY")
                   .SetMessage("Enter the contact's Public Key")
                   .SetPositiveButton("DONE", delegate { this.ContactCreateHelper(input.Text, v); })
                   .SetNegativeButton("CANCEL", delegate { });

            builder.Create().Show();
        }

        [Export("OnSenEmailToServer")]
        public void OnSenEmailToServer(View v)
        {
            EditText to_pk = (EditText)FindViewById(Resource.Id.edEmail);
            EditText content = (EditText)FindViewById(Resource.Id.edContent);
            EditText title = (EditText)FindViewById(Resource.Id.edEmailTitle);

            //show progress
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                ProgressBar input = new ProgressBar(this);
                builder.SetView(input);
               
                alert = builder.SetTitle("SENDING").SetMessage("Sending email.").Create();
                alert.Show();

                myPypem.MyContacts.LoadContacts();
                myPypem.MyInbox.LoadEmails();

                Task.Factory.StartNew(() => { MainActivity.myPypem.SendServerMail(to_pk.Text, content.Text, title.Text, this.UploadDone); });
            }

            
            //Task.Factory.StartNew(() => { MainActivity.pypem.TestUpload(); });
        }

        [Export("OnDownloadEmailFromServer")]
        public void OnDownloadEmailFromServer(View v)
        {
            //show progress
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                ProgressBar input = new ProgressBar(this);
                builder.SetView(input);

                alert = builder.SetTitle("RECIEVING").SetMessage("Recieving email.").Create();
                alert.Show();

               // myPypem.MyContacts.LoadContacts();
                //myPypem.MyInbox.LoadEmails();

                Task.Factory.StartNew(() => { MainActivity.myPypem.RecieveMailFromServer(this.DownloadDone); });
            }


            //Task.Factory.StartNew(() => { MainActivity.pypem.TestUpload(); });
        }

        private void DownloadDone(string message, eMail? email)
        {
            alert.Cancel();

            RunOnUiThread(() => this.ShowDownloadDone(message));
        }

        private void UploadDone(string message, eMail? email)
        {
            alert.Cancel();

            if (email != null)
            {
                MainActivity.myPypem.MyInbox.AddEmail(email);
                MainActivity.OnRefreshInbox();
            }

            RunOnUiThread(() => this.ShowEmailDone(message));
        }

        private void ShowDownloadDone(string message)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            if (message == null)
                builder.SetTitle("ERROR")
                       .SetMessage("Something went wrong, the email couldn't be retrieved.")
                       .SetPositiveButton("PROCEED", delegate { });
            else if (message.IndexOf("success") != -1)
                builder.SetTitle("INFO")
                       .SetMessage(string.Format("{0} email/s recieved from server.\n{1} email/s discarded due to inconsistencies.", message.Split(':')[1].ToString(), message.Split(':')[2].ToString()))
                       .SetPositiveButton("PROCEED", delegate { });
            else if (message.IndexOf("error") != -1)
                builder.SetTitle("ERROR")
                       .SetMessage("Server sent an error: " + message)
                       .SetPositiveButton("PROCEED", delegate { });

            builder.Create().Show();
        }

        private void ShowEmailDone(string message)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            if (message == null)
                builder.SetTitle("ERROR")
                       .SetMessage("Something went wrong, the email couldn't be sent.")
                       .SetPositiveButton("PROCEED", delegate { });
            else if (message.IndexOf("success") != -1)
                builder.SetTitle("INFO")
                       .SetMessage("Email sent.")
                       .SetPositiveButton("PROCEED", delegate { });
            else if (message.IndexOf("error") != -1)
                builder.SetTitle("ERROR")
                       .SetMessage("Server sent an error: " + message)
                       .SetPositiveButton("PROCEED", delegate { });

            builder.Create().Show();
        }

        private void ContactCreateHelper(string PK, View v)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);

            EditText input = new EditText(this);
            input.TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;
            input.SetSingleLine();

            builder.SetView(input);

            builder.SetTitle("CONTACT NAME/ALIAS")
                   .SetMessage("Enter the contact's name/alias")
                   .SetPositiveButton("DONE", delegate 
                   {
                       Contact newContact = new Contact() { Name = input.Text, PublicKey = PK, MyContactSecret = myPypem.MakeRandomSecret() };
                       
                       eMail mail = myPypem.MyInbox.GetLatestEmailbyPK(PK);
                       if (mail.SenderSecret != null)
                           newContact.HisContactSecret = mail.SenderSecret;
                       else
                           newContact.HisContactSecret = "";

                       myPypem.MyContacts.AddContact(newContact);
                   })
                   .SetNegativeButton("CANCEL", delegate { });

            builder.Create().Show();
        }

        [Export("onTestButtonClick")]
        public void onTestButtonClick(View v)
        {
            PYPEM_logic pypem = new PYPEM_logic();
            //pypem.CreateKeyFile("test0123456789012");

            pypem.LoadKeyFile("test0123456789012");
        }
    }
}

