using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KimoEt.ReviewDatabase
{
    class ReviewDataSource
    {
        private static ReviewDataSource instance = null;
        private static readonly object padlock = new object();

        private static readonly int nameIndex = 0;
        private static readonly int packIndex = 1;
        private static readonly int flashRatingIndex = 2;
        private static readonly int drifterRatingIndex = 3;
        private static readonly int mgallopRatingIndex = 4;
        private static readonly int isomorphicRatingIndex = 5;
        private static readonly int averageIndex = 6;
        private static readonly int flashCommentIndex = 7;
        private static readonly int drifterCommentIndex = 8;
        private static readonly int mgallopCommentIndex = 9;
        private static readonly int isomorphicCommentIndex = 10;

        public Dictionary<CardName, CardReview> cardReviewsByName = new Dictionary<CardName, CardReview>();

        public class CardName
        {
            string name;

            public CardName(string name)
            {
                this.name = name;
            }

            public override bool Equals(object obj)
            {
                var cardName = obj as CardName;
                return cardName != null &&
                       this.name.ToLower() == cardName.name.ToLower();
            }

            public override int GetHashCode()
            {
                return 363513814 + EqualityComparer<string>.Default.GetHashCode(name.ToLower());
            }
        }

        private ReviewDataSource()
        {
            LoadCardReviews();
        }

        public static ReviewDataSource Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ReviewDataSource();
                    }
                    return instance;
                }
            }
        }

        private void LoadCardReviews()
        {
            //Card,Pack,Flash,Drifter,Mgallop,Isomorphic,Average,Flash,Drifter,Mgallop,Isomorphic
            foreach (var line in File.ReadAllLines(@"tierLists/TDC.csv").Skip(1).Skip(1))
            {
                var review = new CardReview();
                var columns = line.Split(',');

                int deltaIndex = 0;
                for (int i = 0; i < columns.Length;)
                {
                    //stores the number of index jumps we did on this iteration
                    int numberOfIndexJumps = 1;

                    var finalColumn = columns[i];

                    if (finalColumn.StartsWith("\""))
                    {
                        int quotesOccurence = finalColumn.Split('\"').Length - 1; ;
                        while (quotesOccurence != 2 && (i + numberOfIndexJumps) < columns.Length)
                        {
                            finalColumn += "," + columns[i + numberOfIndexJumps];
                            quotesOccurence = finalColumn.Split('\"').Length - 1;
                            numberOfIndexJumps++;
                        }
                        finalColumn = finalColumn.Replace("\"", "");
                    }

                    if (i == nameIndex)
                    {
                        review.Name = finalColumn;
                    }
                    else if (i == packIndex + deltaIndex)
                    {
                        review.Pack = finalColumn;
                    }
                    else if (i == flashRatingIndex + deltaIndex)
                    {
                        review.RatingsByReviewer.Add("Flash", finalColumn);
                    }
                    else if (i == drifterRatingIndex + deltaIndex)
                    {
                        review.RatingsByReviewer.Add("Drifter", finalColumn);
                    }
                    else if (i == mgallopRatingIndex + deltaIndex)
                    {
                        review.RatingsByReviewer.Add("Mgallop", finalColumn);
                    }
                    else if (i == isomorphicRatingIndex + deltaIndex)
                    {
                        review.RatingsByReviewer.Add("Isomorphic", finalColumn);
                    }
                    else if (i == averageIndex + deltaIndex)
                    {
                        review.AverageRating = finalColumn;
                    }
                    else if (i == flashCommentIndex + deltaIndex)
                    {
                        review.CommentsByReviewer.Add("Flash", finalColumn);
                    }
                    else if (i == drifterCommentIndex + deltaIndex)
                    {
                        review.CommentsByReviewer.Add("Drifter", finalColumn);
                    }
                    else if (i == mgallopCommentIndex + deltaIndex)
                    {
                        review.CommentsByReviewer.Add("Mgallop", finalColumn);
                    }
                    else if (i == isomorphicCommentIndex + deltaIndex)
                    {
                        review.CommentsByReviewer.Add("Isomorphic", finalColumn);
                    }

                    deltaIndex += (numberOfIndexJumps - 1);
                    i += numberOfIndexJumps;
                }
                cardReviewsByName[new CardName(review.Name)] = review;
            }
        }

        public class CardReview
        {
            public string Name { get; set; }
            public string Pack { get; set; }
            public Dictionary<string, string> RatingsByReviewer { get; set; }
            public Dictionary<string, string> CommentsByReviewer { get; set; }
            public string AverageRating { get; set; }

            public CardReview()
            {
                RatingsByReviewer = new Dictionary<string, string>();
                CommentsByReviewer = new Dictionary<string, string>();
            }

            public override bool Equals(object obj)
            {
                var review = obj as CardReview;
                return review != null &&
                       Name == review.Name &&
                       Pack == review.Pack &&
                       EqualityComparer<Dictionary<string, string>>.Default.Equals(RatingsByReviewer, review.RatingsByReviewer) &&
                       EqualityComparer<Dictionary<string, string>>.Default.Equals(CommentsByReviewer, review.CommentsByReviewer) &&
                       AverageRating == review.AverageRating;
            }

            public override int GetHashCode()
            {
                var hashCode = -117006829;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Pack);
                hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(RatingsByReviewer);
                hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(CommentsByReviewer);
                hashCode = hashCode * -1521134295 + AverageRating.GetHashCode();
                return hashCode;
            }

            public override string ToString()
            {
                return $"{Name}: {AverageRating}";
            }

            public string getStringOfAllComments()
            {
                string comments = "";
                foreach(var reviewer in CommentsByReviewer.Keys)
                {
                    string comment = CommentsByReviewer[reviewer];
                    if (comment != "")
                    {
                        comments += reviewer + ": " + comment + "\n";  
                    }
                }
                return comments;
            }
        }
    }
}
