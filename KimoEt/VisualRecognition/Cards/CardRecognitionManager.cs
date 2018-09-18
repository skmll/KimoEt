using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using Tesseract;
using KimoEt.VisualRecognition.Cards;
using static KimoEt.EternalSpecifics.DraftScreen;
using KimoEt.ReviewDatabase;
using KimoEt.Utililties;

namespace KimoEt.VisualRecognition
{
    class CardRecognitionManager
    {
        //private List<string> cardNames = new List<string>();

        private static readonly string TEMP_FILE_PATTERN = @"temp/ocrimage_{0}_150.jpg";
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

        public List<CardGuess> ReadCardName(CardNameLocation nameLocation, Bitmap bitmap)
        {
            return ReadCardName(nameLocation, bitmap, 1.59f);
        }

        private List<CardGuess> ReadCardName(CardNameLocation nameLocation, Bitmap bitmap, float scaleFactor)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //Console.WriteLine("____________________________________________");
            List<CardGuess> foundCardReviews = new List<CardGuess>();

            string locationString = $"{nameLocation.Row}x{nameLocation.Column}";
            var scaledTempFile = string.Format(TEMP_FILE_PATTERN, locationString);
            try
            {
                using (var engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default))
                {
                    Bitmap scaledBitmap = Utils.ScaleBitmap(bitmap, scaleFactor);

                    Directory.CreateDirectory("temp");
                    scaledBitmap.Save(scaledTempFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                    using (var img = Pix.LoadFromFile(scaledTempFile))
                    {
                        using (var page = engine.Process(img))
                        {
                            var found = page.GetText();
                            Console.WriteLine("ORC directly = " + found);
                            var stringResult = Utils.RemoveNewlines(found);

                            if (string.IsNullOrWhiteSpace(stringResult) && scaleFactor < 1.62f)
                            {
                                Console.WriteLine("ORC retrying with more scale");
                                return ReadCardName(nameLocation, bitmap, scaleFactor + 0.01f);
                            }

                            foundCardReviews = FindBestCardReviewMatches(stringResult);

                            if (foundCardReviews[0].Certainty < 0.5f && scaleFactor < 1.62f)
                            {
                                Console.WriteLine("ORC retrying with more scale");
                                return ReadCardName(nameLocation, bitmap, scaleFactor + 0.01f);
                            }

                            foreach (var cardReview in foundCardReviews)
                            {
                                if (!string.IsNullOrWhiteSpace(stringResult))
                                {
                                    Console.WriteLine($"{locationString}, OCR= {stringResult}, got possibility || { cardReview }");// || \n{cardReview.getStringOfAllComments()}");
                                }
                            }
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
                File.Delete(scaledTempFile);
                //watch.Stop();
                //var elapsedMs = watch.ElapsedMilliseconds;
                //Console.WriteLine($"____________________ {elapsedMs}ms ________________________");
            }
            return foundCardReviews;
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

        private List<CardGuess> FindBestCardReviewMatches(string stringResult)
        {
            //Console.WriteLine($"{stringResult}, finding..");
            var bestResult = int.MaxValue;
            List<CardGuess> bestResultCardNames = new List<CardGuess>();

            foreach (var cardReview in ReviewDataSource.Instance.cardReviewsByName.Values)
            {
                var totalDiffCount = LevenshteinDistance.Compute(cardReview.Name, stringResult);

                if (totalDiffCount == 0)
                {
                    //Console.WriteLine("bestResultDiffCount= " + 0);
                    //Console.WriteLine($"{cardReview.Name}, bestResultDiffCount= " + 0);
                    return new List<CardGuess> { new CardGuess {
                        Review = cardReview,
                        Certainty = 1
                    } };
                }

                if (totalDiffCount < bestResult)
                {
                    bestResult = totalDiffCount;
                    bestResultCardNames.Clear();
                    bestResultCardNames.Add(new CardGuess {
                        Review = cardReview,
                        Certainty = CalculateGuessCertainty(totalDiffCount)
                    });
                    //Console.WriteLine($"{cardReview.Name}, newBest= " + bestResult);
                }
                else if (totalDiffCount == bestResult)
                {
                    //Console.WriteLine($"{cardReview.Name}, equalBest= " + bestResult);
                    bestResultCardNames.Add(new CardGuess
                    {
                        Review = cardReview,
                        Certainty = CalculateGuessCertainty(totalDiffCount)
                    });
                }

            }
            //Console.WriteLine("bestResultDiffCount= " + bestResult);
            return bestResultCardNames;
        }

        private float CalculateGuessCertainty(int totalDiffCount)
        {
            // 0 = 1f
            // 7 = 0.1f
            float percentage = 1 - (float) totalDiffCount * 0.128571f;
            if (percentage < 0.1f)
                percentage = 0.1f;

            return percentage;
        }
    }
}
