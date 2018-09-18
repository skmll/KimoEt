using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Tesseract;
using KimoEt.VisualRecognition.Cards;


namespace KimoEt.VisualRecognition.Cards
{
    class FirstTests
    {
        private static bool haveCardsBeenLoaded = false;
        private static List<string> cardNames = new List<string>();

        public static void DoTesseractTesting()
        {
            Dictionary<int, string> wantedById = new Dictionary<int, string>
            {
                { 1, "Mask Maker" },
                { 2, "Silverwing Augmentor" },
                { 3, "Temple Standard" },
                { 4, "Consuming flames" },
                { 5, "Enraged Araktodon" },
                { 6, "Journey Guide" },
                { 7, "Guard Dog" },
                { 8, "Lethrai Target Caller" },
                { 9, "Silverwing Purgeleader" },
                { 10, "Peacekeeper's Helm" },
                { 11, "Crownwatch Recruiter" },
                { 12, "Subterranean Sentry" }
            };

            LoadCardNames();

            for (int i = 1; i < 13; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine("____________________________________________");

                //var stringSuffix = i > 8 ? "" : "_150";
                //var stringSuffix = "_150";
                var stringSuffix = "";
                var testImage = @"testImages/" + i + stringSuffix + ".jpg";
                var scaledTempFile = @"temp/" + i + "_150.jpg";

                try
                {
                    using (var engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default))
                    {
                        Bitmap bitmap = ScaleBitmap(new Bitmap(testImage), 1.5f);

                        Directory.CreateDirectory("temp");
                        bitmap.Save(scaledTempFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                        using (var img = Pix.LoadFromFile(scaledTempFile))
                        {
                            using (var page = engine.Process(img))
                            {
                                var found = page.GetText();
                                Console.WriteLine("FOUND = " + found);
                                var stringResult = RemoveNewlines(found);
                                var wanted = wantedById[i];

                                Console.WriteLine($"wanted= {wanted}, but got = { stringResult }");

                                foreach (var cardNameBest in FindBestCardNameMatches(stringResult))
                                {
                                    Console.WriteLine($"wanted= {wanted}, got possibility = { cardNameBest }");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error= {ex.Message}");
                }
                finally
                {
                    //File.Delete(scaledTempFile);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine($"____________________ {elapsedMs}ms ________________________");
                }
            }
        }

        private static Bitmap ScaleBitmap(Bitmap srcImage, float scaleFactor)
        {
            int newWidth = (int)Math.Round(scaleFactor * srcImage.Width, 0);
            int newHeight = (int)Math.Round(scaleFactor * srcImage.Height, 0);

            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.None;
                gr.InterpolationMode = InterpolationMode.Bicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
            }

            return newImage;
        }

        private static List<string> FindBestCardNameMatches(string stringResult)
        {
            var bestResult = int.MaxValue;
            List<string> bestResultCardNames = new List<string>();

            foreach (var cardName in cardNames)
            {


                var totalDiffCount = LevenshteinDistance.Compute(cardName, stringResult);

                if (totalDiffCount == 0)
                {
                    Console.WriteLine("bestResultDiffCount= " + 0);
                    return new List<string> { cardName };
                }

                if (totalDiffCount < bestResult)
                {
                    bestResult = totalDiffCount;
                    bestResultCardNames.Clear();
                    bestResultCardNames.Add(cardName);
                    //Console.WriteLine("totalDiffCount < bestResult, = " + bestResult);
                    //Console.WriteLine("totalDiffCount < bestResult, cardName= " + cardName);
                }
                else if (totalDiffCount == bestResult)
                {
                    //Console.WriteLine("totalDiffCount == bestResult, = " + bestResult);
                    //Console.WriteLine("totalDiffCount == bestResult, cardName= " + cardName);
                    bestResultCardNames.Add(cardName);
                }

            }
            Console.WriteLine("bestResultDiffCount= " + bestResult);
            return bestResultCardNames;
        }

        private static void LoadCardNames()
        {
            if (haveCardsBeenLoaded)
            {
                return;

            }
            haveCardsBeenLoaded = true;

            foreach (var line in File.ReadAllLines(@"tierLists/tierList.csv").Skip(1).Skip(1))
            {
                var columns = line.Split(',');
                var cardName = columns[0];
                if (cardName.StartsWith("\""))
                {
                    cardName += "," + columns[1];
                    cardName = cardName.Replace("\"", "");
                }
                cardNames.Add(cardName);
            }

            foreach (var item in cardNames)
            {
                Console.WriteLine("card= " + item);
            }

        }

        public static string RemoveNewlines(string original)
        {
            var sb = new StringBuilder(original.Length);

            foreach (char c in original)
                if (c != '\n' && c != '\r' && c != '\t')
                    sb.Append(c);

            return sb.ToString();
        }
    }
}
