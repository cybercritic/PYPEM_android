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
using static pypem_android.PYPEM_logic;
using System.Xml;
using System.IO;

namespace pypem_android
{
    public class Inbox
    {
        private List<eMail> myEmails = new List<eMail>();
        
        public void AddEmail(eMail? email)
        {
            if (email == null)
                return;

            this.myEmails.Add(email ?? new eMail());

            this.SaveEmails();
        }

        public void SaveEmails()
        {
            string content = this.SaveXML();

            MainActivity.myPypem.SaveFile("inbox.pypem", content, true);
        }

        public void ClearEmails()
        {
            this.myEmails = new List<eMail>();
            this.SaveEmails();
        }

        public void DeleteEmail(eMail email)
        {
            this.myEmails.Remove(email);
            this.SaveEmails();
        }

        public List<eMail> GetEmails()
        {
            return this.myEmails.OrderByDescending(p => p.UTCstamp).ToList();
        }

        public eMail GetLatestEmailbyPK(string pk)
        {
            if (this.myEmails.Exists(p => p.FromPK == pk))
                return this.myEmails.FindAll(p => p.FromPK == pk).OrderByDescending(p => p.UTCstamp).First();
            else
                return new eMail();
        }

        public void LoadEmails()
        {
            try
            {
                string content = MainActivity.myPypem.LoadFile("inbox.pypem", true);

                this.myEmails = this.LoadXML(content);
            }
            catch { }
        }

        private List<eMail> LoadXML(string content)
        {
            List<eMail> result = new List<eMail>();

            XmlReader reader = XmlReader.Create(new StringReader(content));

            int ID = 0;
            while (reader.Read())
            {
                if (reader.IsStartElement("emails"))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement("email"))
                        {
                            eMail email = new eMail();
                            email.FromPK = reader.GetAttribute("from_pk");
                            email.ToPK = reader.GetAttribute("to_pk");
                            email.SenderSecret = reader.GetAttribute("sender_secret");
                            email.RecipientSecret = reader.GetAttribute("recipient_secret");
                            email.Content = reader.GetAttribute("content");
                            email.Title = reader.GetAttribute("title");
                            email.UTCstamp = DateTime.ParseExact(reader.GetAttribute("utc_stamp"), "O", System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
                            email.Signature = reader.GetAttribute("signature");
                            reader.Read();

                            if (reader.IsStartElement("server_data"))
                            {
                                ServerEmail server = new ServerEmail();
                                server.FromPK = reader.GetAttribute("from_pk") ?? "";
                                server.ToPK = reader.GetAttribute("to_pk") ?? "";
                                server.Server = reader.GetAttribute("server") ?? "";
                                if (reader.GetAttribute("utc_stamp") != null)
                                    server.UTCstamp = DateTime.ParseExact(reader.GetAttribute("utc_stamp"), "O", System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();

                                server.Match = true;
                                if (email.FromPK != server.FromPK || email.ToPK != MainActivity.myPypem.GetPublicKey() || email.FromPK != server.FromPK)
                                    server.Match = false;

                                email.Server = server;
                            }

                            result.Add(email);
                        }
                    }
                }
            }

            return result;
        }

        private string SaveXML()
        {
            if (this.myEmails == null)
                return null;

            StringBuilder sBuilder = new StringBuilder();

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;

            XmlWriter writer = XmlWriter.Create(sBuilder, xmlSettings);

            writer.WriteStartDocument();
            writer.WriteStartElement("pypem_email_list");

            writer.WriteStartElement("emails");
            foreach (eMail email in this.myEmails)
            {
                writer.WriteStartElement("email");
                writer.WriteAttributeString("from_pk", email.FromPK.ToString());
                writer.WriteAttributeString("to_pk", email.ToPK.ToString());
                writer.WriteAttributeString("sender_secret", email.SenderSecret != null ? email.SenderSecret.ToString() : "");
                writer.WriteAttributeString("recipient_secret", email.RecipientSecret != null ? email.RecipientSecret.ToString() : "");
                writer.WriteAttributeString("content", email.Content.ToString());
                writer.WriteAttributeString("title", email.Title.ToString());
                writer.WriteAttributeString("utc_stamp", email.UTCstamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("signature", email.Signature);

                writer.WriteStartElement("server_data");
                if(email.Server.ServerUsed)
                {
                    writer.WriteAttributeString("from_pk", email.Server.FromPK.ToString());
                    writer.WriteAttributeString("to_pk", email.Server.ToPK.ToString());
                    writer.WriteAttributeString("server", email.Server.Server.ToString());
                    writer.WriteAttributeString("utc_stamp", email.Server.UTCstamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();

            return sBuilder.ToString();
        }
    }
}