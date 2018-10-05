using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KimoEt.EternalSpecifics;
using KimoEt.ProcessWindow;
using KimoEt.ReviewDatabase;
using KimoEt.Utililties;
using Tesseract;
using static KimoEt.EternalSpecifics.DraftScreen;

namespace KimoEt.VisualRecognition.Cards
{
    class CardRecognitionTask
    {
        private static readonly string TEMP_FILE_PATTERN = @"temp/ocrimage_{0}_150.jpg";
        private static readonly float INITIAL_SCALE_FACTOR = 1.59f;
        private static readonly float MIN_CERTAINTY = 0.45f;

        private CardNameLocation nameLocation;
        private Bitmap bitmap;

        private float scaleFactor;

        private string locationString;
        private string scaledTempFile;
        private TesseractEngine engine;

        private Dictionary<int, HashSet<CardGuess>> foundCardReviewsByDiffCount = new Dictionary<int, HashSet<CardGuess>>();

        public CardRecognitionTask(CardNameLocation nameLoc)
        {
            nameLocation = nameLoc;

            scaleFactor = INITIAL_SCALE_FACTOR;

            locationString = $"{nameLoc.Row}x{nameLoc.Column}";
            scaledTempFile = string.Format(TEMP_FILE_PATTERN, locationString);
        }

        public HashSet<CardGuess> Recognize()
        {
            bitmap = ProcessWindowManager.Instance.GetWindowAreaBitmap(nameLocation.Rect, true);

            using (engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default))
            {
                for (int i = 0; i < 4; i++)
                {
                    scaleFactor = INITIAL_SCALE_FACTOR + i * 0.01f;
                    if (OnFoundCardReviews(RecognizeForCurrentScalingFactor()))
                    {
                        break;
                    }

                    if (i == 0) continue;

                    scaleFactor = INITIAL_SCALE_FACTOR - i * 0.01f;
                    if (OnFoundCardReviews(RecognizeForCurrentScalingFactor()))
                    {
                        break;
                    }
                }
            }

            int bestDiffCount = int.MaxValue;

            foreach(var key in foundCardReviewsByDiffCount.Keys)
            {
                if (key < bestDiffCount)
                {
                    bestDiffCount = key;
                }
            }

            if (bestDiffCount == int.MaxValue || foundCardReviewsByDiffCount[bestDiffCount].First().Certainty < MIN_CERTAINTY)
            {
                Console.WriteLine("We didn't find any good match for " + locationString + ", returning null");
                return null;
            }

            foreach (var review in foundCardReviewsByDiffCount[bestDiffCount])
            {
                Console.WriteLine($"DiffCount= {bestDiffCount} , card = {review}");
            }

            return foundCardReviewsByDiffCount[bestDiffCount];
        }

        private bool OnFoundCardReviews(HashSet<CardGuess> foundCardReviews)
        {
            if (foundCardReviews.Count > 0)
            {
                int diffCount = foundCardReviews.First().DiffCount;

                HashSet<CardGuess> existingGuessesWithSameDiffCount = new HashSet<CardGuess>();
                if (foundCardReviewsByDiffCount.TryGetValue(diffCount, out existingGuessesWithSameDiffCount))
                {
                    existingGuessesWithSameDiffCount.UnionWith(foundCardReviews);
                    foundCardReviewsByDiffCount[diffCount] = existingGuessesWithSameDiffCount;
                } else
                {
                    foundCardReviewsByDiffCount[diffCount] = foundCardReviews;
                }

                foreach (var cardReview in foundCardReviews)
                {
                    Console.WriteLine($"{locationString}, got possibility || { cardReview }");
                }

                if (diffCount == 0)
                {
                    //we found perfection, signal it so we can break out of looping
                    return true;
                }
            }

            return false;
        }

        private HashSet<CardGuess> RecognizeForCurrentScalingFactor()
        {
            try
            {
                Bitmap scaledBitmap = Utils.ScaleBitmap(bitmap, scaleFactor);

                Directory.CreateDirectory("temp");
                scaledBitmap.Save(scaledTempFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                using (var img = Pix.LoadFromFile(scaledTempFile))
                {
                    using (var page = engine.Process(img))
                    {
                        var found = page.GetText();
                        var stringResult = Utils.RemoveNewlines(found);
                        Console.WriteLine($"OCR = { stringResult } , scale= {scaleFactor}");

                        if (string.IsNullOrWhiteSpace(stringResult))
                        {
                            Console.WriteLine("ORC found nothing for scale= " + scaleFactor);
                            return new HashSet<CardGuess>();
                        }

                        return RecognitionUtils.FindBestCardReviewMatches(stringResult);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error= {ex.StackTrace}");
                return new HashSet<CardGuess>();
            }
            finally
            {
                //File.Delete(scaledTempFile);
            }
        }
    }
}
