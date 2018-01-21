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
using System.Xml;
using System.IO;

namespace pypem_android
{
    public struct Contact
    {
        public string PublicKey { get; set; }
        public string Name { get; set; }
        public string MyContactSecret { get; set; }
        public string HisContactSecret { get; set; }
        public int ID { get; set; }
    }

    public class Contacts
    {
        private List<Contact> myContacts = new List<Contact>();

        public delegate void OnContactAdded();
        public OnContactAdded RefreshContacts;

        public Contacts()
        {
            
        }

        public void AddContact(Contact contact)
        {
            this.myContacts.Add(contact);
            this.SaveContacts();

            try { this.RefreshContacts(); }
            catch { }
        }

        public void EditContact(Contact contact)
        {
            this.myContacts[this.myContacts.FindIndex(p => p.ID == contact.ID)] = contact;
            this.SaveContacts();
            this.RefreshContacts();
        }

        public void DeleteContact(int id)
        {
            this.myContacts.RemoveAll(p => p.ID == id);

            this.SaveContacts();
        }

        public Contact GetContactByID(int id)
        {
            return this.myContacts.Find(p => p.ID == id);
        }

        public Contact GetContactByPK(string pk)
        {
            return this.myContacts.Find(p => p.PublicKey == pk);
        }

        public List<Contact> GetContacts()
        {
            return this.myContacts;
        }

        public void SaveContacts()
        {
            string content = this.SaveXML();

            MainActivity.myPypem.SaveFile("contacts.pypem", content, true);
        }

        public void LoadContacts()
        {
            try
            {
                string content = MainActivity.myPypem.LoadFile("contacts.pypem", true);

                this.myContacts = this.LoadXML(content);
            }
            catch { }
        }

        private List<Contact> LoadXML(string content)
        {
            List<Contact> result = new List<Contact>();
            
            XmlReader reader = XmlReader.Create(new StringReader(content));

            int ID = 0;
            while (reader.Read())
            {
                if (reader.IsStartElement("contacts"))
                {
                    if (!reader.IsEmptyElement)
                        while (reader.Read())
                        {
                            if (reader.IsStartElement("contact"))
                            {
                                Contact contact = new Contact();
                                contact.PublicKey = reader.GetAttribute("public_key");
                                contact.Name = reader.GetAttribute("name");
                                contact.MyContactSecret = reader.GetAttribute("my_secret");
                                contact.HisContactSecret = reader.GetAttribute("his_secret");
                                contact.ID = ID++;
                                reader.Read();

                                result.Add(contact);
                            }
                            else
                                break;
                        }
                }
            }

            return result;
        }

        private string SaveXML()
        {
            if (this.myContacts == null)
                return null;

            StringBuilder sBuilder = new StringBuilder();

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;

            XmlWriter writer = XmlWriter.Create(sBuilder, xmlSettings);

            writer.WriteStartDocument();
            writer.WriteStartElement("pypem_contacts_list");

            writer.WriteStartElement("contacts");
            foreach (Contact contact in this.myContacts)
            {
                writer.WriteStartElement("contact");
                writer.WriteAttributeString("public_key", contact.PublicKey.ToString());
                writer.WriteAttributeString("name", contact.Name.ToString());
                writer.WriteAttributeString("my_secret", contact.MyContactSecret.ToString());
                writer.WriteAttributeString("his_secret", contact.HisContactSecret.ToString());
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