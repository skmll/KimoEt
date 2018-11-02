using Octokit;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace KimoEtUpdater
{
    public class Updater
    {
        static ManualResetEvent finishedEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.WriteLine("current verison= " + args[0]);
            Console.WriteLine("new version= " + args[1]);

            string fileDownloadUrl = args[2];

            Console.WriteLine("url= " + fileDownloadUrl);

            string fileName = string.Format(@"KimoEt_v{0}NEW.zip", args[1]);
            var parent = new FileInfo(Environment.CurrentDirectory).Directory;
            string fullPath = parent.FullName + "\\" + fileName;

            Console.WriteLine("downloading to= " + fullPath);

            Task.Run(async () =>
            {
                WebClient webClient = new WebClient();

                await webClient.DownloadFileTaskAsync(new Uri(fileDownloadUrl), fullPath);

                Console.WriteLine("download finished, unzipping to= " + parent);
                
                ExtractToDirectory(ZipFile.OpenRead(fullPath), parent.FullName);
                File.Delete(fullPath);

                Console.WriteLine("unzipping finished!");

                finishedEvent.Set();
            });

            finishedEvent.WaitOne();

            Process myapp = new Process();
            myapp.StartInfo.FileName = Environment.CurrentDirectory + "\\KimoEt.exe";
            myapp.Start();
        }

        public static void ExtractToDirectory(ZipArchive archive, string destinationDirectoryName)
        {
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                try
                {
                    file.ExtractToFile(completeFileName, true);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            archive.Dispose();
        }

        async public static void Update(decimal version, int mainProcessId)
        {
            //check new releases
            var client = new GitHubClient(new ProductHeaderValue("kimoEt"));

            int timeout = 5000;
            var task = client.Repository.Release.GetAll("skmll", "KimoEt");
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                Console.WriteLine("Request to github timed out");
                return;
            }

            // task completed within timeout
            var releases = await task;
            var latest = releases[0];

            string versionString = latest.TagName.Replace("v", "");
            decimal latestVersion = decimal.Parse(versionString, CultureInfo.InvariantCulture);

            string downloadUrl = GetDownloadUrl(latest);

            if (latestVersion <= version)
            {
                Console.WriteLine("No new version found: local= " + version + " remote= " + latestVersion);
                return;
            }

            Console.WriteLine("Found new version: local= " + version + " remote= " + latestVersion);
            await CheckForUpdatesToUpdater(latest);

            //start Updater process
            Console.WriteLine("Starting updater");
            Process myapp = new Process();
            myapp.StartInfo.FileName = Environment.CurrentDirectory + "\\KimoEtUpdater.exe";
            myapp.StartInfo.Arguments = version.ToString(CultureInfo.InvariantCulture) + " " + versionString + " " + downloadUrl;
            myapp.Start();

            Console.WriteLine("Stopping KimoEt.exe");
            //kill main process so that we can copy new files
            Process.GetProcessById(mainProcessId).Kill();
        }

        private static async Task CheckForUpdatesToUpdater(Release latest)
        {
            string downloadUrl = null;
            foreach (var asset in latest.Assets)
            {
                if (asset.Name.ToLower().Contains("updater"))
                {
                    downloadUrl = asset.BrowserDownloadUrl;
                }
            }
            if (downloadUrl != null)
            {
                Console.WriteLine("Update to updater found = " + downloadUrl);

                string fileName = "Updater.zip";
                var parent = new FileInfo(Environment.CurrentDirectory).Directory;
                string fullPath = parent.FullName + "\\" + fileName;

                Console.WriteLine("downloading to= " + fullPath);

                WebClient webClient = new WebClient();

                await webClient.DownloadFileTaskAsync(new Uri(downloadUrl), fullPath);

                Console.WriteLine("download finished, unzipping to= " + parent);
                
                ExtractToDirectory(ZipFile.OpenRead(fullPath), parent.FullName);
                File.Delete(fullPath);

                Console.WriteLine("unzipping finished!");
            }
        }

        private static string GetDownloadUrl(Release latest)
        {
            string downloadUrl = null;

            foreach (var asset in latest.Assets)
            {
                if (asset.Name.ToLower().Contains("kimoet_" + latest.TagName + ".zip"))
                {
                    Console.WriteLine("KimoEt was downloaded " + asset.DownloadCount + " times! Keep it up :)");
                    downloadUrl = asset.BrowserDownloadUrl;
                }
            }

            return downloadUrl;
        }
    }
}
