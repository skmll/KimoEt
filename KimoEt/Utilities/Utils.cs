using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace KimoEt.Utililties
{

    public static class Utils
    {

        public static string StringFromTextBox(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(
              // TextPointer to the start of content in the TextBox.
              rtb.Document.ContentStart,
              // TextPointer to the end of content in the TextBox.
              rtb.Document.ContentEnd
            );

            // The Text property on a TextRange object returns a string 
            // representing the plain text content of the TextRange. 
            return textRange.Text.Replace("\r\n", "");
        }

        public static System.Windows.Media.Color ConvertStringToColor(String hex)
        {
            //remove the # at the front
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;

            int start = 0;

            //handle ARGB strings (8 characters long)
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            //convert RGB characters to bytes
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }

        public static void UpdateFontSizeToFit(Button textBox)
        {
            double fontSize = textBox.FontSize;
            FormattedText ft = new FormattedText(textBox.Content as string, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                                             new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                                             fontSize, textBox.Foreground);
            while (textBox.Width < (ft.Width + 10) && fontSize > 2)
            {
                fontSize -= 1;
                ft = new FormattedText(textBox.Content as string, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                                                          new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                                                          fontSize, textBox.Foreground);
            }

            textBox.FontSize = fontSize;
        }

        public static void UpdateFontSizeToFit(TextBox textBox)
        {
            double fontSize = textBox.FontSize;
            FormattedText ft = new FormattedText(textBox.Text as string, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                                             new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                                             fontSize, textBox.Foreground);
            while (textBox.Width < (ft.Width + 10) && fontSize > 2)
            {
                fontSize -= 1;
                ft = new FormattedText(textBox.Text as string, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                                                          new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                                                          fontSize, textBox.Foreground);
            }

            textBox.FontSize = fontSize;
        }

        public static void MakePanelDraggable(UIElement toDrag, Canvas canvas, MainWindow window, DraggableHelper.IOnDragEnded onDragEndedListener)
        {
            new DraggableHelper(toDrag, canvas, window, onDragEndedListener).Start();
        }

        public static System.Drawing.Color GetColorFromRedYellowGreenGradient(double percentage)
        {
            var red = (percentage > 50 ? 1 - 2 * (percentage - 50) / 100.0 : 1.0) * 255;
            var green = (percentage > 50 ? 1.0 : 2 * percentage / 100.0) * 255;
            var blue = 0.0;
            System.Drawing.Color result = System.Drawing.Color.FromArgb((int)red, (int)green, (int)blue);
            return result;
        }

        public static System.Drawing.Color GetForegroundColor(System.Drawing.Color c)
        {
            var brightness = (int)Math.Sqrt(
            c.R * c.R * .241 +
            c.G * c.G * .691 +
            c.B * c.B * .068);

            return brightness > 130 ? System.Drawing.Color.Black : System.Drawing.Color.White;
        }

        public static System.Drawing.Color GetColorForRating(decimal rating)
        {

            var colorsToUse = Settings.Instance.GetChosenRatingsColors();
            List<decimal> keys = Settings.RatingsColorsThresholds;

            if ((rating >= keys[0] && rating < keys[1]))
            {
                return Blend(System.Drawing.Color.FromName(colorsToUse[0]),
                    System.Drawing.Color.FromName(colorsToUse[1]), (double) (1m - (rating / keys[1])));
            }
            else if (rating >= keys[1] && rating < keys[3])
            {
                decimal whatWeWalked = (rating - keys[1]);
                decimal toWalk = keys[2] - keys[1];
                whatWeWalked = whatWeWalked > toWalk ? toWalk : whatWeWalked;
                return Blend(System.Drawing.Color.FromName(colorsToUse[1]),
                    System.Drawing.Color.FromName(colorsToUse[2]), (double)(1m - (whatWeWalked / toWalk) ));
            }
            else
            {
                decimal whatWeWalked = rating - keys[3];
                decimal toWalk = keys[4] - keys[3];
                return Blend(System.Drawing.Color.FromName(colorsToUse[3]),
                    System.Drawing.Color.FromName(colorsToUse[4]), (double)(1m - (whatWeWalked / toWalk)));
            }
        }

        public static System.Drawing.Color Blend(System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static Bitmap ScaleBitmap(Bitmap srcImage, float scaleFactor)
        {
            int newWidth = (int)Math.Round(scaleFactor * srcImage.Width, 0);
            int newHeight = (int)Math.Round(scaleFactor * srcImage.Height, 0);

            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.None;
                gr.InterpolationMode = InterpolationMode.Bicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
            }

            return newImage;
        }

        public static string RemoveNewlines(string original)
        {
            var sb = new StringBuilder(original.Length);

            foreach (char c in original)
                if (c != '\n' && c != '\r' && c != '\t')
                    sb.Append(c);

            return sb.ToString();
        }

        public static System.Windows.Media.Color GetMediaColorFromDrawingColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static void ClearControlFocus(FrameworkElement focusedElement)
        {
            // Move to a parent that can take focus
            FrameworkElement parent = (FrameworkElement)focusedElement.Parent;
            while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
            {
                parent = (FrameworkElement)parent.Parent;
            }

            DependencyObject scope = FocusManager.GetFocusScope(focusedElement);
            FocusManager.SetFocusedElement(scope, parent as IInputElement);
        }

        public static long GetSecondsSinceEpoch()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);

            return  (long)t.TotalMilliseconds;
        }
    }
}
