using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace pypem_android
{
    public class PYPEM_logic
    {
        private RSAParameters publicKey, privateKey;
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private SecureString password = new SecureString();

        public pypem_server MyServer { get; set; }
        public Contacts MyContacts { get; set; }
        public Inbox MyInbox { get; set; }

        public delegate void EmailSecurityWarning(string message);
        public EmailSecurityWarning WarningIssued;

#if DEBUG
        private const string ServerUrl = "http://server.pypem.com:8000";//"http://192.168.8.103:8000";
#else
        private const string ServerUrl = "http://server.pypem.com:8000";
#endif

        //

        public struct eMailServerHeader
        {
            public string FromPK { get; set; }
            public string ToPK { get; set; }
            public DateTime UTCstamp { get; set; }
            public string Data { get; set; }
        }

        public struct eMail
        {
            public string Title;
            public string Content;
            public string ToPK;
            public string FromPK;
            public string SenderSecret;
            public string RecipientSecret;
            public string Signature;
            public bool Padding;
            public bool IncludeFrom;
            public bool IncludeSecret;
            public DateTime UTCstamp;
            public ServerEmail Server;
        }

        public struct ServerEmail
        {
            public bool ServerUsed { get; set; }
            public bool Match { get; set; }
            public string Server { get; set; }
            public string FromPK { get; set; }
            public string ToPK { get; set; }
            public DateTime UTCstamp { get; set; }
        }

        public PYPEM_logic()
        {
            this.MyContacts = new Contacts();
            this.MyInbox = new Inbox();
            this.MyServer = new pypem_server();
        }

        public void SendServerMail(string to_pk, string content, string title, MainActivity.KeyDoneDelegate onDone)
        {
            to_pk = to_pk.Replace("\r", string.Empty).Replace("\n", string.Empty);

            //email part
            eMail email = new eMail();
            email.FromPK = MainActivity.myPypem.GetPublicKey();
            email.ToPK = to_pk;
            email.Padding = false;
            email.IncludeFrom = true;
            email.Content = content;
            email.Title = title;
            email.UTCstamp = DateTime.UtcNow;
            
            Contact toContact = this.MyContacts.GetContactByPK(to_pk);
            if (toContact.MyContactSecret != null && toContact.MyContactSecret.Length != 0)
                email.SenderSecret = toContact.MyContactSecret;

            //make new secret for contact if there is none
            if (email.SenderSecret == null)
            {
                email.SenderSecret = this.MakeRandomSecret();
                toContact.MyContactSecret = email.SenderSecret;
                this.MyContacts.SaveContacts();
            }

            string result = this.MyServer.UploadMail(ServerUrl, email);

            onDone(result, email);
        }

        public void RecieveMailFromServer(MainActivity.KeyDoneDelegate onDone)
        {
            string current = null;
            int many = 0;
            int error = 0;
            do
            {
                current = this.MyServer.DownloadMail(ServerUrl);
                if(current == "error")
                {
                    onDone("error", null);
                    return;
                }

                try
                {
                    PYPEM_logic.eMail? email = MainActivity.myPypem.RecieveEmail(null, MainActivity.myPypem.RecieveEmailServerHeader(current));
                    if (email == null)
                        error++;
                    else
                    {
                        MainActivity.myPypem.MyInbox.AddEmail(email);
                        MainActivity.OnRefreshInbox();
                    }

                    many++;
                }
                catch (Exception e) { }
            }
            while (current != null);

            onDone(string.Format("success:{0}:{1}", many, error), new eMail());
        }
        
        public async Task CreateKeyFile(string password, MainActivity.KeyDoneDelegate onDone)
        {
            if (password == null || password.Length < 12)
                throw new Exception("Invalid password.");

            this.password.Clear();
            this.password = this.ToSecureString(password);

            RSA myRSA = new RSA();
            myRSA.GenerateKeys(out publicKey,out privateKey);

            RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(2048);
            crypt.ImportParameters(publicKey);
            crypt.ImportParameters(privateKey);

            string contents = crypt.ToXmlString(true);
            contents = contents.Insert(0, "<decrypt_check />");

            this.SaveKeyFile(contents);

            onDone(null, null);
        }

        private void SaveKeyFile(string data)
        {
            this.SaveFile("keyfile_00.pypem", data, true);
        }

        public bool LoadKeyFile(string password)
        {
            this.password.Clear();
            this.password = this.ToSecureString(password);

            string content = "";
            try { content = this.LoadFile("keyfile_00.pypem", true); }
            catch { return false; }
            
            if (content.Substring(0, ("<decrypt_check />").Length) != "<decrypt_check />")
                return false;

            content = content.Remove(0, ("<decrypt_check />").Length);

            RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(2048);
            crypt.FromXmlString(content);

            publicKey = crypt.ExportParameters(false);
            privateKey = crypt.ExportParameters(true);
            
            return true;
        }
        
        public void SaveFile(string filename, string data, bool encrypt)
        {
            if(encrypt)
                data = AES.EncryptStringAES(data, this.FromSecureString(this.password));
       
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!Directory.Exists(documents))
                throw new Exception("No documents folder.");

            string final = Path.Combine(documents, filename);

            File.WriteAllText(final, data);
        }

        public bool KeyFileExists()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!Directory.Exists(documents))
                throw new Exception("No documents folder.");

            string final = Path.Combine(documents, "keyfile_00.pypem");

            return File.Exists(final);
        }

        public string LoadFile(string filename, bool decrypt)
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!Directory.Exists(documents))
                throw new Exception("No documents folder.");

            string final = Path.Combine(documents, filename);

            return decrypt ? AES.DecryptStringAES(File.ReadAllText(final), this.FromSecureString(this.password)) : File.ReadAllText(final);
        }

        public string GetPublicKey()
        {
            return Convert.ToBase64String(publicKey.Exponent) + ":" + Convert.ToBase64String(publicKey.Modulus);
        }

        public RSAParameters SetPublicKey(string modulus, string exponent)
        {
            RSAParameters result = new RSAParameters();
            result.Modulus = Convert.FromBase64String(modulus);
            result.Exponent = Convert.FromBase64String(exponent);
            
            return result;
        }

        public string GetHEXfromBYTE(byte[] data)
        {
            string result = "";
            for (int i = 0; i < data.Length; i++)
                result += String.Format("{0:X2}", data[i]);

            return result;
        }

        /// <summary>
        /// make email structure and encrypt it with recipient public key
        /// </summary>
        /// <param name="to_pk">public key of recipient exp:mod</param>
        /// <param name="content">content of email</param>
        /// <returns></returns>
        public string SendEmail(string to_pk, string content, string title, bool stripTo)
        {
            to_pk = to_pk.Replace("\r", string.Empty).Replace("\n", string.Empty);

            //prepare email structure
            eMail email = new eMail();
            email.FromPK = this.GetPublicKey();
            email.ToPK = to_pk;
            email.Padding = false;
            email.IncludeFrom = true;
            email.Content = content;
            email.Title = title;
            email.UTCstamp = DateTime.UtcNow;

            Contact toContact = this.MyContacts.GetContactByPK(to_pk);
            if (toContact.MyContactSecret != null && toContact.MyContactSecret.Length != 0)
                email.SenderSecret = toContact.MyContactSecret;

            //make new secret for contact if there is none
            if (email.SenderSecret == null)
            {
                email.SenderSecret = this.MakeRandomSecret();
                toContact.MyContactSecret = email.SenderSecret;
                this.MyContacts.SaveContacts();
            }

            email.Signature = this.GetSignedHash(email);

            //encrypt email
            string result = this.GetEncryptedEmailString(email, stripTo);
            return result;
        }

        public string MakeRandomSecret()
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            byte[] secretBytes = new byte[256];
            for (int i = 0; i < 256; i++)
                secretBytes[i] = (byte)(rnd.Next(256));

            return Convert.ToBase64String(secretBytes);
        }

        public eMail? RecieveEmail(string encrypted, eMailServerHeader? header = null)
        {
            eMail result = new eMail();
            if (encrypted == null)
                encrypted = header?.Data;

            string aesKeyCrypt = encrypted.Substring(0, encrypted.LastIndexOf('|'));
            if (aesKeyCrypt.IndexOf('|') != -1)
                aesKeyCrypt = aesKeyCrypt.Remove(0, aesKeyCrypt.IndexOf('|') + 1);

            string message = encrypted.Remove(0, encrypted.LastIndexOf('|') + 1);

            string aesKey = Convert.ToBase64String(RSA.DecryptRSA(Convert.FromBase64String(aesKeyCrypt), this.privateKey));
            string emailXML = AES.DecryptStringAES(message, aesKey);

            result = this.ReadEmailXML(emailXML);

            Contact fromContact = this.MyContacts.GetContactByPK(result.FromPK);
            if (fromContact.HisContactSecret == null || fromContact.HisContactSecret.Length == 0)
            {
                fromContact.HisContactSecret = result.SenderSecret;
                this.MyContacts.SaveContacts();
            }
            //TODO:Secrets don't match
            else if (fromContact.HisContactSecret != result.SenderSecret)
                ;

            if (header != null)
            {
                result.Server.FromPK = header?.FromPK;
                result.Server.ToPK = header?.ToPK;
                result.Server.UTCstamp = header?.UTCstamp ?? DateTime.UtcNow;
                result.Server.ServerUsed = true;
                result.Server.Server = ServerUrl;
            }

            if (result.FromPK != result.Server.FromPK)
            {
                //error = "This email's from adresses don't match to the server.";
                return null;
            }

            //signature doesn't match
            bool signature = this.CheckSignedHash(result);
            if (!signature)
            {
                //error = "This email's signature does not match.";
                return null;
            }

            return result;
        }


        public string SendEmailServerHeader(string to_pk, string server_pk, string content, string title, bool stripTo)
        {
            //prepare email structure
            eMail email = new eMail();
            email.FromPK = this.GetPublicKey();
            email.ToPK = to_pk;
            email.Padding = false;
            email.IncludeFrom = true;
            email.Content = content;
            email.Title = title;
            email.UTCstamp = DateTime.UtcNow;

            Contact toContact = this.MyContacts.GetContactByPK(to_pk);
            if (toContact.MyContactSecret != null && toContact.MyContactSecret.Length != 0)
                email.SenderSecret = toContact.MyContactSecret;

            //make new secret for contact if there is none
            if (email.SenderSecret == null)
            {
                email.SenderSecret = this.MakeRandomSecret();
                toContact.MyContactSecret = email.SenderSecret;
                this.MyContacts.SaveContacts();
            }

            email.Signature = this.GetSignedHash(email);

            //encrypt email
            string emailEncrypted = this.GetEncryptedEmailString(email, stripTo);
         
            //make server header
            eMailServerHeader header = new eMailServerHeader();
            header.FromPK = email.FromPK;
            header.ToPK = email.ToPK;
            header.Data = emailEncrypted;

            string headerString = this.MakeEmailServerHeaderXML(header);
            string headerEncryoted = this.GetEncryptedEmailStringHeader(headerString, server_pk);

            return headerEncryoted;
        }

        private string GetSignedHash(eMail eMail)
        {
            string dataStr = eMail.Title + eMail.FromPK + eMail.ToPK + eMail.Content + eMail.UTCstamp.Ticks.ToString("N0");
            byte[] data = Encoding.UTF8.GetBytes(dataStr);
            byte[] hash = RSA.SignData(data, this.privateKey);

            return Convert.ToBase64String(hash);
        }

        private bool CheckSignedHash(eMail eMail)
        {
            string dataStr = eMail.Title + eMail.FromPK + eMail.ToPK + eMail.Content + eMail.UTCstamp.Ticks.ToString("N0");
            byte[] data = Encoding.UTF8.GetBytes(dataStr);
            byte[] signature = Convert.FromBase64String(eMail.Signature);

            string exponent = eMail.FromPK.Substring(0, eMail.FromPK.IndexOf(':'));
            string modulus = eMail.FromPK.Remove(0, eMail.FromPK.IndexOf(':') + 1);

            RSAParameters from_pk = this.SetPublicKey(modulus, exponent);

            return RSA.VerifyDataHash(data, signature, from_pk);
        }

        public string GetEncryptedEmailStringHeader(string data, string to_pk_s)
        {
            string exponent = to_pk_s.Substring(0, to_pk_s.IndexOf(':'));
            string modulus = to_pk_s.Remove(0, to_pk_s.IndexOf(':') + 1);

            RSAParameters to_pk = this.SetPublicKey(modulus, exponent);
            string emailXML = data;
            string emailRSA = this.EncryptEmail(emailXML, to_pk);

            return emailRSA;
        }

        public eMailServerHeader RecieveEmailServerHeader(string encrypted)
        {
            string aesKeyCrypt = encrypted.Substring(0, encrypted.LastIndexOf('|'));
            if (aesKeyCrypt.IndexOf('|') != -1)
                aesKeyCrypt = aesKeyCrypt.Remove(0, aesKeyCrypt.IndexOf('|') + 1);

            string message = encrypted.Remove(0, encrypted.LastIndexOf('|') + 1);

            string aesKey = Convert.ToBase64String(RSA.DecryptRSA(Convert.FromBase64String(aesKeyCrypt), this.privateKey));
            string headerXML = AES.DecryptStringAES(message, aesKey);

            PYPEM_logic.eMailServerHeader header = this.ReadEmailServerHeaderXML(headerXML);

            return header;
        }

        public string GetEncryptedEmailString(eMail email, bool stripTo)
        {
            string exponent = email.ToPK.Substring(0, email.ToPK.IndexOf(':'));
            string modulus = email.ToPK.Remove(0, email.ToPK.IndexOf(':') + 1);

            RSAParameters to_pk = this.SetPublicKey(modulus, exponent);
            string emailXML = this.MakeEmailXML(email);
            string emailRSA = this.EncryptEmail(emailXML, to_pk);

            if(!stripTo)
                emailRSA = emailRSA.Insert(0, string.Format("{0}:{1}|", exponent, modulus));

            return emailRSA;
        }
        
        public string EncryptEmail(string email, RSAParameters to_pk)
        {
            string result = "";

            this.Randomize();

            byte[] secret = new byte[200];
            for (int i = 0; i < 200; i++)
                secret[i] = (byte)(rnd.Next(256));
            string aesSecret = Convert.ToBase64String(secret);

            //encrypt AES key with RSA
            byte[] encryptedAES = RSA.EncyptRSA(secret, to_pk);
            result = Convert.ToBase64String(encryptedAES) + "|";

            //encrypt message with AES
            result += AES.EncryptStringAES(email, aesSecret);
            
            return result;
        }

        public void Randomize()
        {
            int offset = rnd.Next((int)(DateTime.UtcNow.Ticks % 65536)) + 256;
            for (int i = 0; i < offset; i++)
                this.rnd.Next(i * 16);
        }

        public string DecryptAESwithRSA(string data)
        {
            string result = "";

            byte[] decrypted = RSA.DecryptRSA(Convert.FromBase64String(data), this.privateKey);
            result = Convert.ToBase64String(decrypted);

            return result;
        }

        public string MakeEmailServerHeaderXML(eMailServerHeader email)
        {
            StringBuilder sBuilder = new StringBuilder();

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;

            XmlWriter writer = XmlWriter.Create(sBuilder, xmlSettings);

            writer.WriteStartDocument();
            writer.WriteStartElement("pypem_email_server");

            writer.WriteStartElement("meta");
            writer.WriteAttributeString("from_pk", email.FromPK);
            writer.WriteAttributeString("to_pk", email.ToPK);
            writer.WriteAttributeString("utc_stamp", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("data", email.Data);
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();

            return sBuilder.ToString();
        }

        public eMailServerHeader ReadEmailServerHeaderXML(string content)
        {
            eMailServerHeader result = new eMailServerHeader();

            XmlReader reader = XmlReader.Create(new StringReader(content));

            while (reader.Read())
            {
                if (reader.IsStartElement("pypem_email_server"))
                {
                    if (!reader.IsEmptyElement)
                        while (reader.Read())
                        {
                            if (reader.IsStartElement("meta"))
                            {
                                result.FromPK = reader.GetAttribute("from_pk");
                                result.ToPK = reader.GetAttribute("to_pk");
                                result.UTCstamp = DateTime.ParseExact(reader.GetAttribute("utc_stamp") ?? DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture), "O", System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
                                result.Data = reader.GetAttribute("data");
                                reader.Read();
                            }
                            else
                                break;
                        }
                }
            }

            return result;
        }

        public string MakeEmailXML(eMail email)
        {
            StringBuilder sBuilder = new StringBuilder();

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;

            XmlWriter writer = XmlWriter.Create(sBuilder, xmlSettings);

            writer.WriteStartDocument();
            writer.WriteStartElement("pypem_email");

            writer.WriteStartElement("email");

            writer.WriteStartElement("meta");
            writer.WriteAttributeString("from_pk", MainActivity.myPypem.GetPublicKey());
            writer.WriteAttributeString("to_pk", email.ToPK);
            writer.WriteAttributeString("sender_secret", email.SenderSecret);
            writer.WriteAttributeString("recipient_secret", email.RecipientSecret);
            writer.WriteAttributeString("title", email.Title);
            writer.WriteAttributeString("utc_stamp", email.UTCstamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("signature", email.Signature);
            writer.WriteEndElement();

            writer.WriteStartElement("content");
            writer.WriteAttributeString("message", email.Content);
            writer.WriteEndElement();

            string padding = "";
            if (email.Padding)
            {
                writer.Flush();
                int len = sBuilder.ToString().Length;
                while (len % 256 != 0)
                {
                    padding += "X";
                    len++;
                }
            }
            
            writer.WriteStartElement("padding");
            writer.WriteAttributeString("padding", padding);
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();

            return sBuilder.ToString();
        }

        public eMail ReadEmailXML(string content)
        {
            eMail result = new eMail();

            XmlReader reader = XmlReader.Create(new StringReader(content));

            while (reader.Read())
            {
                if (reader.IsStartElement("email"))
                {
                    if (!reader.IsEmptyElement)
                        while (reader.Read())
                        {
                            if (reader.IsStartElement("meta"))
                            {
                                result.FromPK = reader.GetAttribute("from_pk");
                                result.ToPK = reader.GetAttribute("to_pk");
                                result.SenderSecret = reader.GetAttribute("sender_secret");
                                result.RecipientSecret = reader.GetAttribute("recipient_secret");
                                result.Title = reader.GetAttribute("title");
                                result.UTCstamp = DateTime.ParseExact(reader.GetAttribute("utc_stamp") ?? DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),"O", System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
                                result.Signature = reader.GetAttribute("signature");
                                reader.Read();
                            }
                            else if (reader.IsStartElement("content"))
                            {
                                result.Content = reader.GetAttribute("message");
                                reader.Read();
                            }
                            else
                                break;
                        }
                }
            }

            return result;
        }

        private SecureString ToSecureString(string data)
        {
            SecureString result = new SecureString();

            foreach (char c in data)
                result.AppendChar(c);

            return result;
        }

        private string FromSecureString(SecureString data)
        {
            IntPtr bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(data);
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr);
        }
    }
}