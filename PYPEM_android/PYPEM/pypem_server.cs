using System;
using System.Net;
using System.IO;
using System.Net.Http;
using static pypem_android.PYPEM_logic;
using System.Text;

namespace pypem_android
{
    public class pypem_server
    {
        public pypem_server()
        {
            
        }
        
        public string UploadMail(string server, eMail eMail)
        {
            try
            {
                string server_pk = this.GetServerPK(server);
                string secret = this.GetSecretUpload(server);

                string data = MainActivity.myPypem.SendEmailServerHeader(eMail.ToPK, server_pk, eMail.Content, eMail.Title, true);
                //string data = MainActivity.myPypem.SendEmail(eMail.ToPK, eMail.Content, eMail.Title, true);

                return this.UploadEmailRaw(server, WebUtility.UrlEncode(secret), data);
            }
            catch (Exception e) { return null; }
        }

        public string DownloadMail(string server)
        {
            try
            {
                string server_pk = this.GetServerPK(server);
                string secret = this.GetSecretDownload(server);
                
                string result = this.DownloadEmailRaw(server, WebUtility.UrlEncode(secret));
                if (result != null)
                    this.ConfirmEmailDownload(server, WebUtility.UrlEncode(secret));
                        
                return result;
            }
            catch (Exception e)
            {
                return "error";
            }
        }

        public string ConfirmEmailDownload(string server, string secret)
        {
            string result = "";
            string serviceUrl = string.Format("{0}/success?secret={1}", server, secret);

            HttpClient client = new HttpClient();
            Uri uri = new Uri(serviceUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string serverResponse = "";
            try
            {
                HttpResponseMessage response = client.SendAsync(request).Result;

                HttpContent content = response.Content;
                serverResponse = content.ReadAsStringAsync().Result;

                if (serverResponse.IndexOf("no more") != -1)
                    return "no more";
                else if (serverResponse.IndexOf("more") != -1)
                    return "more";
            }
            catch { return null; }

            return result;
        }

        public string GetSecretDownload(string server)
        {
            string result = "";
            string serviceUrl = string.Format("{0}/mailbox_secret?pk={1}", server, Java.Net.URLEncoder.Encode(MainActivity.myPypem.GetPublicKey(), "utf-8"));

            HttpClient client = new HttpClient();
            Uri uri = new Uri(serviceUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string serverResponse = "";
            try
            {
                HttpResponseMessage response = client.SendAsync(request).Result;

                HttpContent content = response.Content;
                serverResponse = content.ReadAsStringAsync().Result;

                string rawSecret = this.ParseRawDownloadSecretFromResponse(serverResponse);
                result = MainActivity.myPypem.DecryptAESwithRSA(rawSecret);
            }
            catch { return null; }

            return result;
        }

        public string GetSecretUpload(string server)
        {
            string result = "";
            string serviceUrl = string.Format("{0}/email_secret?pk={1}", server, Java.Net.URLEncoder.Encode(MainActivity.myPypem.GetPublicKey(), "utf-8"));

            HttpClient client = new HttpClient();
            Uri uri = new Uri(serviceUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string serverResponse = "";
            try
            {
                HttpResponseMessage response = client.SendAsync(request).Result;

                HttpContent content = response.Content;
                serverResponse = content.ReadAsStringAsync().Result;

                string rawSecret = this.ParseRawUploadSecretFromResponse(serverResponse);
                result = MainActivity.myPypem.DecryptAESwithRSA(rawSecret);
            }
            catch { return null; }
            
            return result;
        }

        //

        private string ParseRawDownloadSecretFromResponse(string response)
        {
            response = response.Replace("\n", string.Empty).Replace("\r", string.Empty);

            int start = response.IndexOf("<GetEmailSecretDownloadResult>") + ("<GetEmailSecretDownloadResult>").Length;
            int end = response.IndexOf("</GetEmailSecretDownloadResult>");

            return response.Substring(start, end - start);
        }

        private string ParseRawUploadSecretFromResponse(string response)
        {
            response = response.Replace("\n", string.Empty).Replace("\r", string.Empty);

            int start = response.IndexOf("<GetEmailSecretUploadResult>") + ("<GetEmailSecretUploadResult>").Length;
            int end = response.IndexOf("</GetEmailSecretUploadResult>");

            return response.Substring(start, end - start);
        }

        public string GetServerPK(string server)
        {
            string result = "";
            string serviceUrl = string.Format("{0}/server_key", server);

            HttpClient client = new HttpClient();
            Uri uri = new Uri(serviceUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string serverResponse = "";
            try
            {
                HttpResponseMessage response = client.SendAsync(request).Result;

                HttpContent content = response.Content;
                serverResponse = content.ReadAsStringAsync().Result;
                result = this.ParseRawPKFromResponse(serverResponse);
            }
            catch { }

            return result;
        }

        private string ParseRawPKFromResponse(string response)
        {
            response = response.Replace("\n", string.Empty).Replace("\r", string.Empty);

            int start = response.IndexOf("<GetServerPublicKeyResult>") + ("<GetServerPublicKeyResult>").Length;
            int end = response.IndexOf("</GetServerPublicKeyResult>");

            return response.Substring(start, end - start);
        }

        private string DownloadEmailRaw(string server, string secret)
        {
            string serviceUrl = string.Format("{0}/email/mailbox?secret={1}", server, secret);
            WebRequest request = WebRequest.Create(serviceUrl);
            request.Method = "GET";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream fileStream = response.GetResponseStream();

            StreamReader tmp = new StreamReader(fileStream);

            string result = tmp.ReadToEnd();

            if (result == "")
                return null;

            return result;
        }
        
        private string UploadEmailRaw(string server, string secret, string email_raw)
        {
            string serviceUrl = string.Format("{0}/email/upload?secret={1}", server, secret);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUrl);
            request.Method = "POST";

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(email_raw));

            Stream requestStream = request.GetRequestStream();
            stream.CopyTo(requestStream);
            
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseText = reader.ReadToEnd();

            return responseText;
        }
    }
}