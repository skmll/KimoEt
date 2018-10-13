using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KimoEt.ReviewDatabase
{
    public class TierListDownloader
    {
        public static bool isTDCsReady = false;

        public class WebClientEx : WebClient
        {
            public WebClientEx(CookieContainer container)
            {
                this.container = container;
            }

            private readonly CookieContainer container = new CookieContainer();

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest r = base.GetWebRequest(address);
                var request = r as HttpWebRequest;
                if (request != null)
                {
                    request.CookieContainer = container;
                }
                return r;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                WebResponse response = base.GetWebResponse(request, result);
                ReadCookies(response);
                return response;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                WebResponse response = base.GetWebResponse(request);
                ReadCookies(response);
                return response;
            }

            private void ReadCookies(WebResponse r)
            {
                var response = r as HttpWebResponse;
                if (response != null)
                {
                    CookieCollection cookies = response.Cookies;
                    container.Add(cookies);
                }
            }
        }

        public async static void DownloadTDCs()
        {
            Directory.CreateDirectory("tierlists");
            int timeout = 5000;
            var task = SyncDownloadTDCs();
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                Console.WriteLine("Download TDCs timedout, use local instead.");
                isTDCsReady = true;
            } else
            {
                Console.WriteLine("Download TDCs finished.");
                isTDCsReady = true;
            }
        }

        public async static void DownloadSunyveils()
        {
            Directory.CreateDirectory("tierlists");
            int timeout = 5000;
            var task = SyncDownloadSunyveils();
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                Console.WriteLine("Download Sunyveils timedout, use local instead.");
                //isTDCsReady = true;
            } else
            {
                Console.WriteLine("Download Sunyveils finished.");
                //isTDCsReady = true;
            }
        }

        private static Task SyncDownloadTDCs()
        {
            return Task.Run(() =>
            {
                /*
                1. Your Google SpreadSheet document must be set to 'Anyone with the link' can view it
             
                2. To get URL press SHARE (top right corner) on Google SpreeadSheet and copy "Link to share".
              
                3. Now add "&output=csv" parameter to this link
             
                4. Your link will look like:
                    https://docs.google.com/spreadsheets/d/KEY/export?format=csv&id=KEY&gid=0&output=csv
                */
                string url = @"https://docs.google.com/spreadsheets/d/1NH1i_nfPKhXO53uKYgJYICrTx_XSqDC88b2I3e0vsc0/export?format=csv&id=1NH1i_nfPKhXO53uKYgJYICrTx_XSqDC88b2I3e0vsc0&gid=0";

                WebClientEx wc = new WebClientEx(new CookieContainer());
                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:22.0) Gecko/20100101 Firefox/22.0");
                wc.Headers.Add("DNT", "1");
                wc.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                wc.Headers.Add("Accept-Encoding", "deflate");
                wc.Headers.Add("Accept-Language", "en-US,en;q=0.5");

                byte[] dt = wc.DownloadData(url);
                var outputCSVdata = Encoding.UTF8.GetString(dt ?? new byte[] { });

                var tdcCsvCorrected = "";

                using (StringReader reader = new StringReader(outputCSVdata))
                {
                    int i = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Replace("’", "'");
                        if (i == 0 || line.Equals("\""))
                        {
                            tdcCsvCorrected += line;
                        }
                        else if (line.StartsWith("All Banners"))
                        {
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Praxis Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Rakano Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Combrei Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Elysian Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Feln Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Skycrag Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Argenport Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Hooru Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Stonescar Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Xenan Banner");
                        }
                        else if (line.StartsWith("All Crests"))
                        {
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Impulse");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Glory");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Progress");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Wisdom");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Cunning");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Fury");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Vengeance");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Order");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Chaos");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Mystery");
                        }
                        else
                        {
                            tdcCsvCorrected += "\n" + line;
                        }
                        i++;
                    }
                }

                File.WriteAllText(Environment.CurrentDirectory + "\\tierLists\\TDC.csv", tdcCsvCorrected);
            });
        }

        private static Task SyncDownloadSunyveils()
        {
            return Task.Run(() =>
            {
                /*
                1. Your Google SpreadSheet document must be set to 'Anyone with the link' can view it
             
                2. To get URL press SHARE (top right corner) on Google SpreeadSheet and copy "Link to share".
              
                3. Now add "&output=csv" parameter to this link
             
                4. Your link will look like:
                    https://docs.google.com/spreadsheets/d/KEY/export?format=csv&id=KEY&gid=0&output=csv
                */
                string url = @"https://docs.google.com/spreadsheets/d/1zYZSfRlD3ZSx1pW2mtE87FnjNTY9pkpMPz4Tz4PxJzk/export?format=csv&id=1zYZSfRlD3ZSx1pW2mtE87FnjNTY9pkpMPz4Tz4PxJzk&gid=0&output=csv";

                WebClientEx wc = new WebClientEx(new CookieContainer());
                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:22.0) Gecko/20100101 Firefox/22.0");
                wc.Headers.Add("DNT", "1");
                wc.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                wc.Headers.Add("Accept-Encoding", "deflate");
                wc.Headers.Add("Accept-Language", "en-US,en;q=0.5");

                byte[] dt = wc.DownloadData(url);
                var outputCSVdata = Encoding.UTF8.GetString(dt ?? new byte[] { });

                var tdcCsvCorrected = "";

                using (StringReader reader = new StringReader(outputCSVdata))
                {
                    int i = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (i == 0 || line.Equals("\""))
                        {
                            tdcCsvCorrected += line;
                        }
                        else if (line.StartsWith("All Banners"))
                        {
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Praxis Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Rakano Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Combrei Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Elysian Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Feln Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Skycrag Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Argenport Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Hooru Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Stonescar Banner");
                            tdcCsvCorrected += "\n" + line.Replace("All Banners", "Xenan Banner");
                        }
                        else if (line.StartsWith("All Crests"))
                        {
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Impulse");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Glory");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Progress");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Wisdom");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Cunning");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Fury");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Vengeance");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Order");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Chaos");
                            tdcCsvCorrected += "\n" + line.Replace("All Crests", "Crest of Mystery");
                        }
                        else
                        {
                            tdcCsvCorrected += "\n" + line;
                        }
                        i++;
                    }
                }

                File.WriteAllText(Environment.CurrentDirectory + "\\tierLists\\Sunyveil.csv", tdcCsvCorrected);
            });
        }
    }
}
