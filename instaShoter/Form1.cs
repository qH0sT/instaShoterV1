using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace instaShoter
{
    public partial class Form1 : Form      //SagoistCoding//
    {
        private HttpClient client = null;
        Dictionary<string, string> postlar = new Dictionary<string, string>();
        public Form1()
        {
            InitializeComponent();
            cookieJar = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = cookieJar
            };
            client = new HttpClient(handler, true);
        }
        private void cerezleriCek()
        {
            cerezler = "";
            foreach (Cookie cookie in cookieJar.GetCookies(new Uri("https://www.instagram.com/")))
            {
            
                    if (cookie.Name == "csrftoken")
                    {
                        csrftoken = cookie.Value;

                    }
                
                cerezler += cookie.Name + "=" + cookie.Value + ";";
            }
        }
        string csrftoken = "";
        string userID = ""; //lazım olur diye userid yi almıştım
        string cerezler = "";
        CookieContainer cookieJar;
        private async Task<bool> instaGiris()
        {
                    
            client.DefaultRequestHeaders.Add("referer", "https://www.google.com/");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
            HttpResponseMessage resultat = await client.GetAsync("https://www.instagram.com/");
            cerezleriCek();
            //MessageBox.Show(cerezler);       
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            client.DefaultRequestHeaders.Add("origin", "https://www.instagram.com");
            client.DefaultRequestHeaders.Remove("referer");
            client.DefaultRequestHeaders.Add("referer", "https://www.instagram.com");
            client.DefaultRequestHeaders.Add("x-csrftoken", csrftoken);
            client.DefaultRequestHeaders.Add("cookie", cerezler);           
            //client.DefaultRequestHeaders.Add("x-instagram-ajax", "c83c20a25204");
            //client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            Dictionary<string, string> datas = new Dictionary<string, string>
             {
             { "username", textBox1.Text },
             { "enc_password", textBox2.Text },
             { "queryParams","{}"},
             { "optIntoOneTap","false"}
            };
            FormUrlEncodedContent content = new FormUrlEncodedContent(datas);
            HttpResponseMessage response = await client.PostAsync("https://www.instagram.com/accounts/login/ajax/", content);
            string responseString = await response.Content.ReadAsStringAsync();
            try
            {
                dynamic attriubutes = JObject.Parse(responseString);
                userID = attriubutes.userId.ToString();
                if (attriubutes.status.ToString() == "ok")
                {
                    tabControl1.SelectedIndex = 0;
                    listBox1.Items.Add(formatLog("Hesaba başarılı bir şeklilde giriş yapıldı."));
                    return true;
                }
                else
                {
                    listBox1.Items.Add(formatLog("Hesaba giriş yapılamadı: ") + responseString);
                    return false;
                }
            }
            catch (Exception ex) { listBox1.Items.Add(formatLog("Hesaba giriş yapılamadı: ") + ex.Message); return false; }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
           if(await instaGiris())
            {
                dynamic attriubutes = JObject.Parse(new WebClient().DownloadString("https://www.instagram.com/"+textBox1.Text+"/?__a=1"));
                pictureBox1.ImageLocation = attriubutes["graphql"]["user"]["profile_pic_url"].ToString();
                textBox4.Text = attriubutes["graphql"]["user"]["biography"].ToString();
                label7.Text = "Takipçi\n"+attriubutes["graphql"]["user"]["edge_followed_by"]["count"].ToString();
                label9.Text = "Takip\n" + attriubutes["graphql"]["user"]["edge_follow"]["count"].ToString();
                label10.Text = "Gönderi\n" + attriubutes["graphql"]["user"]["edge_owner_to_timeline_media"]["count"].ToString();
                MessageBox.Show("Hesaba Giriş yapıldı.","instaShoter V1.0", 0, (MessageBoxIcon)64);
            }
        }
        private string mainPagePostsJSON()
        {
            string content = File.ReadAllText("page.html");
            content = content.Substring(content.IndexOf("'feed',{"))
           .Replace("'feed',{", "");
            content = content.Replace(content.Substring(content.IndexOf("}}}]}}}")), "");
            return ("{"+content + "}}}]}}}");
        }
        private void postlarıEkle()
        {
            postlar.Clear();
            var attriubutes = JObject.Parse(mainPagePostsJSON());
            foreach (dynamic nods in attriubutes["user"]["edge_web_feed_timeline"]["edges"])
            {
                try
                {
                    postlar.Add(nods["node"]["id"].ToString(), nods["node"]["taken_at_timestamp"].ToString());
                }
                catch (Exception) { }
            }

        }
        public static DateTime unixToNow(double unixSeconds)
        {

            DateTime dtime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtime = dtime.AddSeconds(unixSeconds).ToLocalTime();
            return dtime;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            listBox1.Items.Add(formatLog("BAŞLADI"));
        }
        private string formatLog(string text)
        {
            return ("[" + DateTime.Now.ToString("HH:mm") + "]" + text);
        }
        private int totalSeconds(string input)
        {
            DateTime dt = DateTime.Now;
            DateTime postdate = unixToNow(Convert.ToDouble(input));
            var t = dt - postdate;
            return int.Parse((t.TotalSeconds.ToString()).Split(',')[0]);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            listBox1.Items.Add(formatLog("DURDURULDU"));
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Enabled = ((checkBox1.Checked) ? false : true);
        }
        List<string> yorumYapilmis = new List<string>();
        private async void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            cerezleriCek();
            client.DefaultRequestHeaders.Add("cookie", cerezler);
            client.DefaultRequestHeaders.Remove("referer");
            client.DefaultRequestHeaders.Add("referer", "https://www.instagram.com/login/");
            HttpResponseMessage resultatMainPage = await client.GetAsync("https://www.instagram.com/");
            File.WriteAllText("page.html", await resultatMainPage.Content.ReadAsStringAsync()); //bir şeye baktığım için dosya olarak kaydetmiştim.
            await Task.Delay(75);
            postlarıEkle();
            cerezleriCek();
            client.DefaultRequestHeaders.Remove("origin");
            client.DefaultRequestHeaders.Remove("referer");
            client.DefaultRequestHeaders.Remove("x-csrftoken");
            client.DefaultRequestHeaders.Remove("user-agent");
            client.DefaultRequestHeaders.Remove("cookie");
            //client.DefaultRequestHeaders.Add("host", "www.instagram.com");
            client.DefaultRequestHeaders.Add("cookie", cerezler);
            //client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
            client.DefaultRequestHeaders.Add("x-csrftoken", csrftoken);
            client.DefaultRequestHeaders.Add("x-instagram-ajax", "c83c20a25204");
            client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            Dictionary<string, string> datas = new Dictionary<string, string>
             {
             { "comment_text", textBox3.Text },
             { "replied_to_comment_id", "" }
            };
            FormUrlEncodedContent content = default(FormUrlEncodedContent);
            HttpResponseMessage mesaj = default(HttpResponseMessage);
            foreach (KeyValuePair<string, string> post in postlar.ToList())
            {
                content = new FormUrlEncodedContent(datas);
                if (yorumYapilmis.Contains(post.Key) == false)
                {
                    if (checkBox1.Checked)
                    {
                        mesaj = await client.PostAsync("https://www.instagram.com/web/comments/" + post.Key + "/add/", content);
                        listBox1.Items.Add(formatLog("Yorum gönderildi, sunucu yanıtı: "+mesaj.StatusCode.ToString() + " " + await mesaj.Content.ReadAsStringAsync()));
                        yorumYapilmis.Add(post.Key);
                    }
                    else
                    {
                        if (totalSeconds(post.Value) <= ((int)numericUpDown1.Value))
                        {
                            mesaj = await client.PostAsync("https://www.instagram.com/web/comments/" + post.Key + "/add/", content);
                           listBox1.Items.Add(formatLog("Yorum gönderildi, sunucu yanıtı: " + mesaj.StatusCode.ToString() + " " + await mesaj.Content.ReadAsStringAsync()));
                           yorumYapilmis.Add(post.Key);
                        }
                    }
                   
                }         
            }
            timer1.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog op = new OpenFileDialog() {
              Multiselect = false, Filter = "Metin Belgesi (*.txt)| *.txt",
              Title = "Hesap bilgilerinin olduğu metin belgesini seçiniz.."})
            {
                if(op.ShowDialog() == DialogResult.OK)
                {
                    string[] cimbiz = File.ReadAllText(op.FileName).Split(' ');
                    textBox1.Text = cimbiz[0];
                    textBox2.Text = cimbiz[1];
                }
            }
        }
    }
}
