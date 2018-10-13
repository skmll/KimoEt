using KimoEt.ReviewDatabase;
using System.Collections.Generic;

namespace KimoEt.VisualRecognition.Cards
{
    static class RecognitionUtils
    {
        public static HashSet<CardGuess> FindBestCardReviewMatches(string stringResult)
        {
            //Console.WriteLine($"{stringResult}, finding..");
            var bestResult = int.MaxValue;
            HashSet<CardGuess> bestResultCardNames = new HashSet<CardGuess>();

            foreach (var cardName in ReviewDataSource.Instance.cardReviewsByNameByTeam.Keys)
            {
                var totalDiffCount = LevenshteinDistance.Compute(cardName.name, stringResult);

                if (totalDiffCount == 0)
                {
                    //Console.WriteLine("bestResultDiffCount= " + 0);
                    //Console.WriteLine($"{cardReview.Name}, bestResultDiffCount= " + 0);
                    return new HashSet<CardGuess> { new CardGuess {
                        ReviewByTeam = ReviewDataSource.Instance.cardReviewsByNameByTeam[cardName],
                        DiffCount = 0,
                        Certainty = 1
                    } };
                }

                if (totalDiffCount < bestResult)
                {
                    bestResult = totalDiffCount;
                    bestResultCardNames.Clear();
                    bestResultCardNames.Add(new CardGuess
                    {
                        ReviewByTeam = ReviewDataSource.Instance.cardReviewsByNameByTeam[cardName],
                        DiffCount = totalDiffCount,
                        Certainty = CalculateGuessCertainty(totalDiffCount)
                    });
                    //Console.WriteLine($"{cardReview.Name}, newBest= " + bestResult);
                }
                else if (totalDiffCount == bestResult)
                {
                    //Console.WriteLine($"{cardReview.Name}, equalBest= " + bestResult);
                    bestResultCardNames.Add(new CardGuess
                    {
                        ReviewByTeam = ReviewDataSource.Instance.cardReviewsByNameByTeam[cardName],
                        DiffCount = totalDiffCount,
                        Certainty = CalculateGuessCertainty(totalDiffCount)
                    });
                }

            }
            //Console.WriteLine("bestResultDiffCount= " + bestResult);
            return bestResultCardNames;
        }

        public static float CalculateGuessCertainty(int totalDiffCount)
        {
            // 0 = 1f
            // 7 = 0.1f
            float percentage = 1 - (float)totalDiffCount * 0.128571f;
            if (percentage < 0.1f)
                percentage = 0.1f;

            return percentage;
        }
    }
}
