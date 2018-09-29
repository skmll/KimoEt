using KimoEt.ReviewDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KimoEt.ReviewDatabase.ReviewDataSource;

namespace KimoEt.VisualRecognition.Cards
{
    public class CardGuess
    {
        public static float UNCLEAR_CERTAINTY = -1.0f;

        public Dictionary<Team, CardReview> ReviewByTeam { get; set; }
        public int DiffCount { get; set; }
        public float Certainty { get; set; }

        public override string ToString()
        {
            return $"{ReviewByTeam[ReviewByTeam.Keys.First()].ToString()} w/ {(int)(Certainty * 100)} and { DiffCount } differences";
        }

        public override bool Equals(object obj)
        {
            var guess = obj as CardGuess;
            return guess != null &&
                   EqualityComparer<Dictionary<Team, CardReview>>.Default.Equals(ReviewByTeam, guess.ReviewByTeam) &&
                   DiffCount == guess.DiffCount &&
                   Certainty == guess.Certainty;
        }

        public override int GetHashCode()
        {
            var hashCode = 195335044;
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<Team, CardReview>>.Default.GetHashCode(ReviewByTeam);
            hashCode = hashCode * -1521134295 + DiffCount.GetHashCode();
            hashCode = hashCode * -1521134295 + Certainty.GetHashCode();
            return hashCode;
        }
    }
}
