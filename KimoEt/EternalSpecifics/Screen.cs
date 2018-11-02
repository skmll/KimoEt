using KimoEt.ProcessWindow;
using System.Collections.Generic;

namespace KimoEt.EternalSpecifics
{
    public abstract class Screen
    {
        public abstract List<CardNameLocation> GetAllCardNameLocations();

        public abstract List<CardNameLocation> GetCardNameLocationsNotAvailable(int draftPickNumber);

        public abstract List<CardNameLocation> GetCardNameLocationsAvailable(int draftPickNumber);

        public abstract RECT GetRectForCard(int row, int column);

        public abstract RECT GetRectForPickNumber(bool isDoubleDigits = false);

        public abstract string GetExceptionPickNumber(string readPickNumber);

        public abstract string PickNumberStartsWith();

        public abstract bool IsForge();
    }
}
