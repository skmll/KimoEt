using KimoEt.ReviewDatabase;
using KimoEt.VisualRecognition.Cards;
using System.Collections.Generic;
using System.Linq;

namespace KimoEt.Utilities
{
    public static class CardReviewForUiUtils
    {
        public static float GetRatingForColor(CardGuess cardGuess)
        {
            if (Settings.Instance.TierListMode == Settings.USE_SUNYVEIL_TIER_LIST)
            {
                return cardGuess.ReviewByTeam[Team.SUNYVEIL].AverageRatingForColor();
            }

            if (Settings.Instance.TierListMode == Settings.USE_BOTH_TIER_LIST)
            {
                float sunyveilRating = cardGuess.ReviewByTeam[Team.SUNYVEIL].AverageRatingForColor();
                float tdcRating = cardGuess.ReviewByTeam[Team.TDC].AverageRatingForColor();

                return sunyveilRating * 2 / 5 + tdcRating * 3 / 5;
            }

            //mode is TDC
            return cardGuess.ReviewByTeam[Team.TDC].AverageRatingForColor();
        }

        public static string GetRatingLabel(HashSet<CardGuess> cardGuesses)
        {
            if (Settings.Instance.TierListMode == Settings.USE_SUNYVEIL_TIER_LIST)
            {
                return cardGuesses.Last().ReviewByTeam[Team.SUNYVEIL].ToString() + (cardGuesses.Count > 1 ? "*" : "");
            }

            if (Settings.Instance.TierListMode == Settings.USE_BOTH_TIER_LIST)
            {
                float sunyveilRating = cardGuesses.Last().ReviewByTeam[Team.SUNYVEIL].AverageRatingForColor();
                float tdcRating = cardGuesses.Last().ReviewByTeam[Team.TDC].AverageRatingForColor();
                float avgRating = sunyveilRating * 2 / 5 + tdcRating * 3 / 5;

                return $"{cardGuesses.Last().ReviewByTeam[Team.TDC].Name}: {avgRating}" + (cardGuesses.Count > 1 ? "*" : "");
            }

            //mode is TDC
            return cardGuesses.Last().ReviewByTeam[Team.TDC].ToString() + (cardGuesses.Count > 1 ? "*" : "");
        }

        public static string GetCommentsText(CardGuess cardGuess)
        {
            if (Settings.Instance.TierListMode == Settings.USE_SUNYVEIL_TIER_LIST)
            {
                return cardGuess.ReviewByTeam[Team.SUNYVEIL].GetCommentsText();
            }

            if (Settings.Instance.TierListMode == Settings.USE_BOTH_TIER_LIST)
            {
                string sunyveil = cardGuess.ReviewByTeam[Team.SUNYVEIL].GetCommentsText();
                string tdc = cardGuess.ReviewByTeam[Team.TDC].GetCommentsText();

                return "TDC:" + "\n" + tdc + "\n" + sunyveil;
            }

            //mode is TDC
            return cardGuess.ReviewByTeam[Team.TDC].GetCommentsText();
        }
    }
}
