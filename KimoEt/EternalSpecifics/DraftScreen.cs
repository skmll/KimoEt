using KimoEt.ProcessWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KimoEt.EternalSpecifics
{
    static class DraftScreen
    {
        private static readonly int FIRST_CARD_RELATIVE_X = 400;
        private static readonly int FIRST_CARD_RELATIVE_Y = 280;

        private static readonly int CARD_NAME_WIDTH = 161;
        private static readonly int CARD_NAME_HEIGHT = 14;

        private static readonly int CARD_NAME_X_DIFF = 223;
        private static readonly int CARD_NAME_Y_DIFF = 315;

        private static readonly int[] ROWS = { 1, 2, 3 };
        private static readonly int[] COLUMNS = { 1, 2, 3, 4 };

        public static List<CardNameLocation> GetAllCardNameLocations()
        {
            return GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);
        }

        private static List<CardNameLocation> GetAllCardNameLocationsForRowsColumns(int[] rows, int[] columns)
        {
            List<CardNameLocation> locations = new List<CardNameLocation>();

            foreach (int row in rows)
            {
                foreach (int column in columns)
                {
                    locations.Add(new CardNameLocation(row, column, GetRectForCard(row, column)));
                }
            }

            return locations;
        }

        public static List<CardNameLocation> GetCardNameLocationsNotAvailable(int draftPickNumber)
        {
            var picksAfterNewPack = (draftPickNumber % 12) - 1;
            if (picksAfterNewPack < 0)
            {
                picksAfterNewPack = 11;
            }

            List<CardNameLocation> all = GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);

            return all.GetRange(all.Count - picksAfterNewPack, picksAfterNewPack);
        }

        public static List<CardNameLocation> GetCardNameLocationsAvailable(int draftPickNumber)
        {
            if (draftPickNumber == -1)
            {
                return GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);
            }
            var picksAfterNewPack = (draftPickNumber % 12) - 1;
            if (picksAfterNewPack < 0)
            {
                picksAfterNewPack = 11;
            }
            List<CardNameLocation> all = GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);
            all.RemoveRange(all.Count - picksAfterNewPack, picksAfterNewPack);

            return all;
        }

        public static RECT GetRectForCard(int row, int column)
        {
            int mathRow = row - 1;
            int mathColumn = column - 1;

            int relativeXLeft = FIRST_CARD_RELATIVE_X + mathColumn * CARD_NAME_X_DIFF;
            if (column == 4)
            {
                relativeXLeft++;
            }
            int relativeYTop = FIRST_CARD_RELATIVE_Y + mathRow * CARD_NAME_Y_DIFF;
            if (row == 3 || row == 2)
            {
                relativeYTop+= (row -1);
            }

            int relativeXRight = relativeXLeft + CARD_NAME_WIDTH;
            int relativeYBottom = relativeYTop + CARD_NAME_HEIGHT;

            return new RECT(relativeXLeft, relativeYTop, relativeXRight, relativeYBottom);
        }
        
        public static RECT GetRectForPickNumber(bool isDoubleDigits = false)
        {
            if (!isDoubleDigits)
            {
                return new RECT(1076, 1040, 1250, 1070);
            }
            return new RECT(1060, 1040, 1250, 1070);
        }

        public static string GetExceptionPickNumber(string readPickNumber)
        {
            //if ("i10f48".Equals(readPickNumber))
            //{
            //    return "1 of 48";
            //}

            return readPickNumber;
        }

        public class CardNameLocation
        {
            public CardNameLocation(int row, int column, RECT rect)
            {
                Row = row;
                Column = column;
                Rect = rect;
            }

            public int Row { get; set; }
            public int Column { get; set; }
            public RECT Rect { get; set; }

            public override bool Equals(object obj)
            {
                var location = obj as CardNameLocation;
                return location != null &&
                       Row == location.Row &&
                       Column == location.Column;
            }

            public override int GetHashCode()
            {
                var hashCode = 240067226;
                hashCode = hashCode * -1521134295 + Row.GetHashCode();
                hashCode = hashCode * -1521134295 + Column.GetHashCode();
                return hashCode;
            }

            public override string ToString()
            {
                return $"{Row}x{Column}";
            }

            public static CardNameLocation FromString(string str)
            {
                string[] strs = str.Split('x');

                int row = Convert.ToInt32(strs[0]);
                int column = Convert.ToInt32(strs[1]);

                return new CardNameLocation(row, column, GetRectForCard(row, column));
            }
        }
    }
}
