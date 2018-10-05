using KimoEt.ProcessWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KimoEt.EternalSpecifics
{
    public class DraftScreen : Screen
    {
        private static readonly int CARD_NAME_WIDTH = 161;
        private static readonly int CARD_NAME_HEIGHT = 14;

        private static readonly int FIRST_CARD_RELATIVE_X = 400;
        private static readonly int FIRST_CARD_RELATIVE_Y = 242;

        private static readonly int CARD_NAME_X_DIFF = 223;
        private static readonly int CARD_NAME_Y_DIFF = 315;

        private static readonly int[] ROWS = { 1, 2, 3 };
        private static readonly int[] COLUMNS = { 1, 2, 3, 4 };

        private static DraftScreen instance = null;
        private static readonly object padlock = new object();

        public static DraftScreen Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DraftScreen();
                    }
                    return instance;
                }
            }
        }

        private DraftScreen() { }

        public override List<CardNameLocation> GetAllCardNameLocations()
        {
            return GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);
        }

        private List<CardNameLocation> GetAllCardNameLocationsForRowsColumns(int[] rows, int[] columns)
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

        public override List<CardNameLocation> GetCardNameLocationsNotAvailable(int draftPickNumber)
        {
            var picksAfterNewPack = (draftPickNumber % 12) - 1;
            if (picksAfterNewPack < 0)
            {
                picksAfterNewPack = 11;
            }

            List<CardNameLocation> all = GetAllCardNameLocationsForRowsColumns(ROWS, COLUMNS);

            return all.GetRange(all.Count - picksAfterNewPack, picksAfterNewPack);
        }

        public override List<CardNameLocation> GetCardNameLocationsAvailable(int draftPickNumber)
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

        public override RECT GetRectForCard(int row, int column)
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
                relativeYTop += (row - 1);
            }

            int relativeXRight = relativeXLeft + CARD_NAME_WIDTH;
            int relativeYBottom = relativeYTop + CARD_NAME_HEIGHT;

            return new RECT(
                relativeXLeft,
                relativeYTop,
                relativeXRight,
                relativeYBottom
                );
        }
        
        public override RECT GetRectForPickNumber(bool isDoubleDigits = false)
        {
            if (!isDoubleDigits)
            {
                return new RECT(
                    1076,
                    1001, 
                    1250,
                    1031
                    );
            }
            return new RECT(
                1060,
                1001,
                1250,
                1031
                );
        }

        public override string GetExceptionPickNumber(string readPickNumber)
        {
            return readPickNumber;
        }

        public override string PickNumberStartsWith()
        {
            return "Card";
        }

        public override bool IsForge()
        {
            return false;
        }
    }
}
