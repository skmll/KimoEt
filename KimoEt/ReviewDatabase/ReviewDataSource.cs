using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private static readonly int ratingIndex = 1;
        private static readonly int splashableIndex = 2;
        private static readonly int conditionalIndex = 3;

        public Dictionary<CardName, Dictionary<Team, CardReview>> cardReviewsByNameByTeam = new Dictionary<CardName, Dictionary<Team, CardReview>>();

        public class CardName
        {
            public string name;

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
            foreach (var key in cardReviewsByNameByTeam.Keys)
            {
                bool tdcHas = cardReviewsByNameByTeam[key].ContainsKey(Team.TDC);
                bool sunyveilHas = cardReviewsByNameByTeam[key].ContainsKey(Team.SUNYVEIL);
                if (tdcHas != sunyveilHas)
                {
                    if (!tdcHas)
                    {
                        var review = new TdcCardReview()
                        {
                            Name = key.name,
                            Pack = "0",
                            AverageRating = cardReviewsByNameByTeam[key][Team.SUNYVEIL].AverageRatingForColor().ToString("0.0"),
                        };

                        review.RatingsByReviewer.Add("Flash", "");
                        review.RatingsByReviewer.Add("Drifter", "");
                        review.RatingsByReviewer.Add("Mgallop", "");
                        review.RatingsByReviewer.Add("Isomorphic", "");
                        review.CommentsByReviewer.Add("Flash", "");
                        review.CommentsByReviewer.Add("Drifter", "");
                        review.CommentsByReviewer.Add("Mgallop", "");
                        review.CommentsByReviewer.Add("Isomorphic", "");

                        cardReviewsByNameByTeam[key][Team.TDC] = review;
                    }
                    else
                    {
                        var review = new SunyveilCardReview()
                        {
                            Name = key.name,
                            AverageRating = SunyveilCardReview.AverageRatingStringFromFloat(cardReviewsByNameByTeam[key][Team.TDC].AverageRatingForColor()),
                            Splashable = false,
                            Conditional = false,
                        };

                        cardReviewsByNameByTeam[key][Team.SUNYVEIL] = review;
                    }
                }
            }

            foreach (var key in cardReviewsByNameByTeam.Keys)
            {
                bool tdcHas = cardReviewsByNameByTeam[key].ContainsKey(Team.TDC);
                bool sunyveilHas = cardReviewsByNameByTeam[key].ContainsKey(Team.SUNYVEIL);
                if (tdcHas != sunyveilHas)
                {
                    Console.WriteLine($"Found key \"{key.name}\" on TDC " + tdcHas + " on Sunyveil " + sunyveilHas);
                }
            }
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
            LoadTDCsReviews();
            LoadSunyveilsReviews();
        }

        private void LoadTDCsReviews()
        {
            //Card,Pack,Flash,Drifter,Mgallop,Isomorphic,Average,Flash,Drifter,Mgallop,Isomorphic
            foreach (var line in File.ReadAllLines(@"tierLists/TDC.csv").Skip(1).Skip(1))
            {
                var review = new TdcCardReview();
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

                if (!cardReviewsByNameByTeam.ContainsKey(new CardName(review.Name)))
                {
                    cardReviewsByNameByTeam[new CardName(review.Name)] = new Dictionary<Team, CardReview>();
                }
                cardReviewsByNameByTeam[new CardName(review.Name)][Team.TDC] = review;
            }
        }

        private void LoadSunyveilsReviews()
        {
            //each line=
            //cardname, rating, splashable, conditional
            foreach (var line in File.ReadAllLines(@"tierLists/Sunyveil.csv").Skip(1))
            {
                var review = new SunyveilCardReview();
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
                    else if (i == ratingIndex + deltaIndex)
                    {
                        review.AverageRating = finalColumn;
                    }
                    else if (i == splashableIndex + deltaIndex)
                    {
                        review.Splashable = finalColumn.Equals("TRUE");
                    }
                    else if (i == conditionalIndex + deltaIndex)
                    {
                        review.Conditional = finalColumn.Equals("TRUE");
                    }

                    deltaIndex += (numberOfIndexJumps - 1);
                    i += numberOfIndexJumps;
                }

                if (!cardReviewsByNameByTeam.ContainsKey(new CardName(review.Name)))
                {
                    cardReviewsByNameByTeam[new CardName(review.Name)] = new Dictionary<Team, CardReview>();
                }
                cardReviewsByNameByTeam[new CardName(review.Name)][Team.SUNYVEIL] = review;
            }
        }
    }
}
