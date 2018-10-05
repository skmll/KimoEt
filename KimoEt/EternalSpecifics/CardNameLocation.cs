using KimoEt.ProcessWindow;
using System;

namespace KimoEt.EternalSpecifics
{
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

        public static CardNameLocation FromString(string str, bool isForge = false)
        {
            string[] strs = str.Split('x');

            int row = Convert.ToInt32(strs[0]);
            int column = Convert.ToInt32(strs[1]);
            
            if (isForge)
            {
                return new CardNameLocation(row, column, ForgeScreen.Instance.GetRectForCard(row, column));
            } 

            return new CardNameLocation(row, column, DraftScreen.Instance.GetRectForCard(row, column));
        }
    }
}
