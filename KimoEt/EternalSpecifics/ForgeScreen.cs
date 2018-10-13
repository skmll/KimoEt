using System.Collections.Generic;
using KimoEt.ProcessWindow;

namespace KimoEt.EternalSpecifics
{
    public class ForgeScreen : Screen
    {
        private static readonly int CARD_NAME_WIDTH = 175;
        private static readonly int CARD_NAME_HEIGHT = 17;

        private static readonly int FIRST_CARD_RELATIVE_X = 448;
        private static readonly int FIRST_CARD_RELATIVE_Y = 522 - 39;

        private static readonly int CARD_NAME_X_DIFF = 282;
        private static readonly int CARD_NAME_Y_DIFF = 0;

        private static readonly int[] ROWS = { 1 };
        private static readonly int[] COLUMNS = { 1, 2, 3 };

        private static ForgeScreen instance = null;
        private static readonly object padlock = new object();

        public static ForgeScreen Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ForgeScreen();
                    }
                    return instance;
                }
            }
        }

        private ForgeScreen() { }

        public override RECT GetRectForPickNumber(bool isDoubleDigits = false)
        {
            if (!isDoubleDigits)
            {
                return new RECT(
                    743,
                    707 - 39,
                    900,
                    736 - 39
                );
            }

            return new RECT(
                733,
                707 - 39,
                910,
                736 - 39
            );
        }

        public override RECT GetRectForCard(int row, int column)
        {
            //we can ignore the row
            int mathColumn = column - 1;
            int left = FIRST_CARD_RELATIVE_X + mathColumn * CARD_NAME_X_DIFF;

            return new RECT(left, FIRST_CARD_RELATIVE_Y, left + CARD_NAME_WIDTH, FIRST_CARD_RELATIVE_Y + CARD_NAME_HEIGHT);
        }

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
            return new List<CardNameLocation>();
        }

        public override List<CardNameLocation> GetCardNameLocationsAvailable(int draftPickNumber)
        {
            return GetAllCardNameLocations();
        }

        public override string GetExceptionPickNumber(string readPickNumber)
        {
            //no exceptions for now
            return readPickNumber;
        }

        public override string PickNumberStartsWith()
        {
            return "Pick";
        }

        public override bool IsForge()
        {
            return true;
        }
    }
}
