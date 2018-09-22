using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KimoEtUpdater
{
    public class Updater
    {
        public static readonly decimal VERSION = 1.1m;
        static ManualResetEvent finishedEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.WriteLine("current verison= " + VERSION);
            Console.WriteLine("new version= " + args[0]);

            string fileDownloadUrl = string.Format(@"https://github.com/skmll/KimoEt/releases/download/v{0}/KimoEt_v{0}.zip", args[0]);

            Console.WriteLine("url= " + fileDownloadUrl);

            string fileName = string.Format(@"KimoEt_v{0}NEW.zip", args[0]);
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
                file.ExtractToFile(completeFileName, true);
            }

            archive.Dispose();
        }

        async public static void Update(int mainProcessId)
        {
            var client = new GitHubClient(new ProductHeaderValue("kimoEt"));

            int timeout = 5000;
            var task = client.Repository.Release.GetAll("skmll", "KimoEt");
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                return;
            }

            // task completed within timeout
            var releases = await task;
            var latest = releases[0];

            string versionString = latest.TagName.Replace("v", "");
            decimal latestVersion = decimal.Parse(versionString, CultureInfo.InvariantCulture);

            if(latestVersion <= VERSION)
            {
                return;
            }
            
            Process myapp = new Process();
            myapp.StartInfo.FileName = Environment.CurrentDirectory + "\\KimoEtUpdater.exe";
            myapp.StartInfo.Arguments = versionString;
            myapp.Start();

            Process.GetProcessById(mainProcessId).Kill();
        }
    }
}
