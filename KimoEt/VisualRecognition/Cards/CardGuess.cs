using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KimoEt.ReviewDatabase.ReviewDataSource;

namespace KimoEt.VisualRecognition.Cards
{
    class CardGuess
    {
        public static float UNCLEAR_CERTAINTY = -1.0f;

        public CardReview Review { get; set; }
        public float Certainty { get; set; }

        public override string ToString()
        {
            return $"{Review.ToString()} \nw/ {(int)(Certainty * 100)} ";
        }
    }
}
