using System;
using System.Collections.Generic;
using System.Globalization;

namespace KimoEt.ReviewDatabase
{
    public abstract class CardReview
    {
        public string Name { get; set; }
        public string AverageRating { get; set; }

        public override bool Equals(object obj)
        {
            var review = obj as CardReview;
            return review != null &&
                   Name == review.Name &&
                   AverageRating == review.AverageRating;
        }

        public override int GetHashCode()
        {
            var hashCode = 1527297976;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AverageRating);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Name}: {AverageRating}";
        }

        public abstract float AverageRatingForColor();
        public abstract string GetCommentsText();
    }

    public class TdcCardReview : CardReview
    {
        public string Pack { get; set; }
        public Dictionary<string, string> RatingsByReviewer { get; set; }
        public Dictionary<string, string> CommentsByReviewer { get; set; }

        public TdcCardReview()
        {
            RatingsByReviewer = new Dictionary<string, string>();
            CommentsByReviewer = new Dictionary<string, string>();
        }

        public string GetStringOfAllComments()
        {
            string comments = "";
            foreach (var reviewer in CommentsByReviewer.Keys)
            {
                string comment = CommentsByReviewer[reviewer];
                if (comment != "")
                {
                    comments += reviewer + ": " + comment + "\n";
                }
            }
            return comments;
        }

        public override string GetCommentsText()
        {
            var commentsText = "";

            Dictionary<string, string> commentsByReviewer = CommentsByReviewer;
            Dictionary<string, string> ratingsByReviewer = RatingsByReviewer;

            foreach (var reviewer in commentsByReviewer.Keys)
            {
                string comment = commentsByReviewer[reviewer];
                commentsText += reviewer.ToUpper() + " (" + ratingsByReviewer[reviewer] + ")";
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    commentsText += ":\n";
                    commentsText += "\"" + comment + "\"\n";
                }
                else
                {
                    commentsText += "\n";
                }
                commentsText += "\n";
            }

            return commentsText.Remove(commentsText.Length - 1);
        }

        public override bool Equals(object obj)
        {
            var review = obj as TdcCardReview;
            return review != null &&
                   base.Equals(obj) &&
                   Pack == review.Pack &&
                   EqualityComparer<Dictionary<string, string>>.Default.Equals(RatingsByReviewer, review.RatingsByReviewer) &&
                   EqualityComparer<Dictionary<string, string>>.Default.Equals(CommentsByReviewer, review.CommentsByReviewer);
        }

        public override int GetHashCode()
        {
            var hashCode = -1995347914;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Pack);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(RatingsByReviewer);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(CommentsByReviewer);
            return hashCode;
        }

        public override float AverageRatingForColor()
        {
            return float.Parse(AverageRating, CultureInfo.InvariantCulture.NumberFormat);
        }
    }

    public class SunyveilCardReview : CardReview
    {
        public bool Splashable { get; set; }
        public bool Conditional { get; set; }

        public static Dictionary<string, float> ratingsForColorByStringRating = new Dictionary<string, float>()
        {
            {"S", 5.0f},
            //{"A+", 4.8f},
            {"A", 4.5f},
            //{"A-", 4.0f},
            {"B+", 4.0f},
            {"B", 3.7f},
            {"B-", 3.3f},
            {"C+", 3.0f},
            {"C", 2.5f},
            {"C-", 2.0f},
            {"D+", 1.5f},
            {"D", 1.0f},
            {"D-", 0.5f},
            {"E", 0.0f},
            {"F", 0.0f}
        };

        public static Dictionary<string, string> commentsByRating = new Dictionary<string, string>()
        {
            {"S", "\"Take this card, and think about moving in to this faction even if you're not in it yet\""},
            {"A", "\"Take this card unless you are far away from playing this faction\""},
            {"B", "\"Solid card for this faction, but not worth derailing your draft\""},
            {"C", "\"Positive filler\""},
            {"D", "\"Negative filler\""},
            {"E", "\"I hope you don't have to play any of these\""},
            {"F", "\"I hope you don't have to play any of these\""}
        };
        
        public static string AverageRatingStringFromFloat(float ratingFloat)
        {
            float leastDiff = float.MaxValue;
            string closestRating = "A";

            foreach (var key in ratingsForColorByStringRating.Keys)
            {
                float diff = Math.Abs(ratingsForColorByStringRating[key] - ratingFloat);
                if (diff <= leastDiff)
                {
                    leastDiff = diff;
                    closestRating = key;
                }
            }

            return closestRating;
        }

        public override float AverageRatingForColor()
        {
            return ratingsForColorByStringRating[AverageRating];
        }

        public override string GetCommentsText()
        {
            var commentsText = "SUNYVEIL ( " + AverageRating + " ) : \n";

            foreach(var rating in commentsByRating.Keys)
            {
                if (AverageRating.StartsWith(rating))
                {
                    //commentsText += "RATING NOTE: \n";
                    commentsText += "- " + commentsByRating[rating];
                    break;
                }
            }

            commentsText += "\n";
            //commentsText += "SPLASHABLE NOTE: \n";
            commentsText += "- \"The card is " + (Splashable ? "" : "NOT ") + "worth splashing\"";
            if (Conditional)
            {
                commentsText += "\n";
                //commentsText += "CONDITIONAL NOTE: \n";
                commentsText += "- \"The card is conditional. The rating is assuming you are well set up to use it\"";
            }

            return commentsText;
        }

        public override bool Equals(object obj)
        {
            var review = obj as SunyveilCardReview;
            return review != null &&
                   base.Equals(obj) &&
                   Splashable == review.Splashable &&
                   Conditional == review.Conditional;
        }

        public override int GetHashCode()
        {
            var hashCode = 104547731;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Splashable.GetHashCode();
            hashCode = hashCode * -1521134295 + Conditional.GetHashCode();
            return hashCode;
        }
    }
}
