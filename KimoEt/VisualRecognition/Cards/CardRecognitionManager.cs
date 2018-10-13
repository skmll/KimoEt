using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Tesseract;
using KimoEt.VisualRecognition.Cards;
using KimoEt.Utililties;
using KimoEt.EternalSpecifics;

namespace KimoEt.VisualRecognition
{
    class CardRecognitionManager
    {
        private static CardRecognitionManager instance = null;
        private static readonly object padlock = new object();

        CardRecognitionManager()
        {
        }

        public static CardRecognitionManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new CardRecognitionManager();
                    }
                    return instance;
                }
            }
        }

        public HashSet<CardGuess> ReadCardName(CardNameLocation nameLocation)
        {
            return new CardRecognitionTask(nameLocation).Recognize();
        }

        public string ReadPickNumber(Bitmap bitmap)
        {
            var scaledTempFile = @"temp/pickNumber.jpg";
            try
            {
                using (var engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default))
                {
                    Bitmap scaledBitmap = Utils.ScaleBitmap(bitmap, 1.59f);

                    Directory.CreateDirectory("temp");
                    scaledBitmap.Save(scaledTempFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                    using (var img = Pix.LoadFromFile(scaledTempFile))
                    {
                        using (var page = engine.Process(img))
                        {
                            var found = page.GetText();
                            var stringResult = Utils.RemoveNewlines(found);

                            return stringResult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error= {ex.StackTrace}");
            }
            finally
            {
                //File.Delete(scaledTempFile);
            }
            return null;
        }
    }
}
