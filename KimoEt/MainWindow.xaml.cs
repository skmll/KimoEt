using KimoEt.EternalSpecifics;
using KimoEt.ProcessWindow;
using KimoEt.ReviewDatabase;
using KimoEt.UI;
using KimoEt.Utililties;
using KimoEt.VisualRecognition;
using KimoEt.VisualRecognition.Cards;
using KimoEtUpdater;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static KimoEt.EternalSpecifics.DraftScreen;
using static KimoEt.ReviewDatabase.ReviewDataSource;

namespace KimoEt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, DraggableHelper.IOnDragEnded
    {
        private bool isPaused = true;
        private int draftPickNumber = -1;
        private bool isDoubleDigitsPickNumber = false;
        private bool isWindowSetup = false;
        private Dictionary<CardNameLocation, List<CardGuess>> currentCardGuessesByLocation = new Dictionary<CardNameLocation, List<CardGuess>>();
        public static SolidColorBrush backgroundBrush = new SolidColorBrush(Utils.ConvertStringToColor("#1E1E1E")) { Opacity = 0.7f };
        public static string VERSION_STRING = Updater.VERSION.ToString(CultureInfo.InvariantCulture);

        public static double ScaleFactorX;
        public static double ScaleFactorY;

        public MainWindow()
        {
            InitializeComponent();

            ProcessWindowManager.Instance.BindLocationToThisWindow(this);
            Settings.LoadSettingsFromDisk();
            TierListDownloader.DownloadTDCs();

            if (Settings.Instance.LastVersionUsed < Updater.VERSION)
            {
                DialogWindowManager.ShowInitialWarning(HolderCanvas);
                Settings.Instance.SetNewLastVersionUsed(Updater.VERSION);
            }

            KimoEtTitle.Text = "KimoEt - v" + VERSION_STRING;
            FontFamily = new FontFamily("Segoe UI");

            Utils.MakePanelDraggable(CanvasBorder, HolderCanvas, this, this);
            Canvas.SetLeft(CanvasBorder, Settings.Instance.MenuPoint.X);
            Canvas.SetTop(CanvasBorder, Settings.Instance.MenuPoint.Y);

            ComboboxColorMode.SelectedIndex = Settings.Instance.RatingColorMode;
            CheckForUpdates();

            this.Loaded += (s, e) =>
            {
                Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
                ScaleFactorX = m.M11;
                ScaleFactorY = m.M22;
            };
        }

        private static void CheckForUpdates()
        {
            Task.Run(() =>
            {
                Updater.Update(Process.GetCurrentProcess().Id);
            });
        }

        private void MakeUnmakeColorsRuler(bool updateOnly)
        {
            const string stackPanelName = "panelRatingsColors";

            StackPanel toRemove = FindName(stackPanelName) as StackPanel;
            if (toRemove != null)
            {
                MainCanvas.Children.Remove(toRemove);
                MainCanvas.UnregisterName(stackPanelName);
                if (!updateOnly) { return; }
            }

            if (toRemove == null && updateOnly) { return; }

            StackPanel ratingsColors = new StackPanel()
            {
                Name = stackPanelName,
                Orientation = Orientation.Vertical,
            };
            Canvas.SetLeft(ratingsColors, Settings.Instance.RatingColorsRulerPoint.X);
            Canvas.SetTop(ratingsColors, Settings.Instance.RatingColorsRulerPoint.Y);
            MainCanvas.Children.Add(ratingsColors);
            MainCanvas.RegisterName(ratingsColors.Name, ratingsColors);

            Utils.MakePanelDraggable(ratingsColors, HolderCanvas, this, this);

            for (int i = 0; i <= 50; i++)
            {
                decimal rating = (decimal)i / 10;

                var color = Utils.GetMediaColorFromDrawingColor(Utils.GetColorForRating(rating));

                TextBlock colorTest = new TextBlock()
                {
                    Name = "colorTestI" + i,
                    Text = "Rating:\t" + rating,
                    Width = 80,
                    Height = 20,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                };
                ratingsColors.Children.Add(colorTest);

                colorTest.Background = backgroundBrush;
                colorTest.Foreground = new SolidColorBrush(color);
            }
        }

        private void OnRatingColorClick(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.RatingColorMode != Settings.RATING_COLOR_USER)
            {
                return;
            }

            var senderName = (sender as Button).Name;
            string rating = senderName.Split('_')[1];

            const string stackPanelName = "availableColors";
            if (RemoveControlWithName(SettingsCanvas, stackPanelName))
            {
                return;
            }

            ScrollViewer colorsScrollViewer = new ScrollViewer() { Name = stackPanelName, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            Canvas.SetLeft(colorsScrollViewer, 300);
            Canvas.SetTop(colorsScrollViewer, -100);
            SettingsCanvas.Children.Add(colorsScrollViewer);
            SettingsCanvas.RegisterName(colorsScrollViewer.Name, colorsScrollViewer);

            StackPanel availableColors = new StackPanel() { Orientation = Orientation.Vertical };
            colorsScrollViewer.Content = availableColors;

            List<System.Drawing.Color> colors = Settings.GetAllAvailbleColors();

            foreach (var colorFinal in colors)
            {
                TextBlock colorToChoose = new TextBlock()
                {
                    Name = colorFinal.Name + "I" + rating,
                    Text = colorFinal.Name,
                    Width = 130,
                    Height = 20,
                    Foreground = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(Utils.GetForegroundColor(colorFinal))),
                    Background = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(colorFinal)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                };
                availableColors.Children.Add(colorToChoose);
                colorToChoose.PreviewMouseUp += OnColorChosenForRating;
            }
        }

        public void OnDragEnded(UIElement element)
        {
            double top = Canvas.GetTop(element);
            double left = Canvas.GetLeft(element);

            double translatedX = (element.RenderTransform as TranslateTransform).X;
            double translatedY = (element.RenderTransform as TranslateTransform).Y;

            double realTop = top + translatedY;
            double realLeft = left + translatedX;

            if (element == CanvasBorder)
            {
                Settings.Instance.SetNewMenuPosition(new Point(realLeft, realTop));
            }
            else //rating colors ruler
            {
                Settings.Instance.SetNewRatingColorsRulerPosition(new Point(realLeft, realTop));
            }
        }

        private bool RemoveControlWithName(Panel removeFrom, string stackPanelName)
        {
            FrameworkElement toRemove = FindName(stackPanelName) as FrameworkElement;
            if (toRemove != null)
            {
                removeFrom.Children.Remove(toRemove);
                removeFrom.UnregisterName(stackPanelName);

                return true;
            }

            return false;
        }

        private void OnColorChosenForRating(object sender, MouseButtonEventArgs e)
        {
            var senderName = (sender as TextBlock).Name;
            var ratingString = senderName.Split('I')[1];
            var colorName = senderName.Split('I')[0];
            decimal rating = Convert.ToInt32(ratingString) / 10m;

            (FindName("Rating_" + ratingString) as Button).Background = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(System.Drawing.Color.FromName(colorName)));

            Settings.Instance.SetNewColorForRating(rating, colorName);

            MakeUnmakeColorsRuler(true);
            UpdateCardBestGuessLabelColors();
            RemoveControlWithName(SettingsCanvas, "availableColors");
        }

        private void UpdateCardBestGuessLabelColors()
        {
            foreach (var pair in currentCardGuessesByLocation)
            {
                UpdateCardReview(pair.Key, pair.Value);
            }
        }

        private void SetupCardReviews()
        {
            foreach (var location in DraftScreen.GetAllCardNameLocations())
            {
                MakeCardReviewLabel(location);
            }
        }

        private Storyboard latestStoryboardRefreshButton;
        private void GetAllCardReviews()
        {
            if (isPaused) return;

            foreach (var location in DraftScreen.GetAllCardNameLocations())
            {
                var myTextBox = (Button)this.FindName(GetBestGuessLabelName(location));
                myTextBox.Visibility = Visibility.Hidden;
            }

            var rotateImage = (Image)RefreshBtn.Content;
            rotateImage.RenderTransform = new RotateTransform();
            rotateImage.RenderTransformOrigin = new Point(0.5, 0.5);

            Storyboard storyboard = new Storyboard();
            DoubleAnimation rotateAnimation = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(0.6)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(rotateAnimation, rotateImage);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            storyboard.Children.Add(rotateAnimation);
            latestStoryboardRefreshButton = storyboard;
            storyboard.Begin(this, true);

            foreach (var location in DraftScreen.GetCardNameLocationsAvailable(draftPickNumber))
            {

                Task.Run(() =>
                {
                    var cardGuesses = CardRecognitionManager.Instance.ReadCardName(location, ProcessWindowManager.Instance.GetWindowAreaBitmap(location.Rect, true));

                    if (cardGuesses == null || cardGuesses.Count == 0)
                    {
                        return;
                    }
                    UpdateCardReview(location, cardGuesses);
                });
            }
        }

        private void SetupPickNumber()
        {
            RECT pickRect = DraftScreen.GetRectForPickNumber();

            TextBox txtNumber = FindName("DraftPickNumber") as TextBox;
            if (txtNumber == null)
            {
                txtNumber = new TextBox
                {
                    Name = "DraftPickNumber",
                    Text = "Loading..",
                    Width = 400,
                    Height = 40,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Colors.White),
                    CaretBrush = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    FontSize = 20
                };
                Canvas.SetLeft(txtNumber, pickRect.Left);
                Canvas.SetTop(txtNumber, pickRect.Top - 40);
                MainCanvas.Children.Add(txtNumber);
                MainCanvas.RegisterName(txtNumber.Name, txtNumber);
            }

            Task.Run(() =>
            {
                while (!isPaused)
                {
                    bool isWindowNormal = ProcessWindowManager.Instance.IsWindowStateNormal();
                    if (isWindowNormal && TierListDownloader.isTDCsReady)
                    {
                        var pickNumber = CardRecognitionManager.Instance.ReadPickNumber(ProcessWindowManager.Instance.GetWindowAreaBitmap(DraftScreen.GetRectForPickNumber(isDoubleDigitsPickNumber), false));
                        if (pickNumber == null || !pickNumber.StartsWith("Card"))
                        {
                            isDoubleDigitsPickNumber = !isDoubleDigitsPickNumber;
                            pickNumber = CardRecognitionManager.Instance.ReadPickNumber(ProcessWindowManager.Instance.GetWindowAreaBitmap(DraftScreen.GetRectForPickNumber(isDoubleDigitsPickNumber), false));
                        }

                        if (pickNumber == null) continue;

                        try
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() => OnNewPickNumber(pickNumber, txtNumber)));

                        } catch { /*do nothing*/}
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        private void OnNewPickNumber(string newPickNumber, TextBox txtNumber)
        {
            var textRead = newPickNumber;
            newPickNumber = newPickNumber.Replace("Card", "").Trim();
            newPickNumber = DraftScreen.GetExceptionPickNumber(newPickNumber);

            if (string.IsNullOrWhiteSpace(newPickNumber))
                return;


            var is1Numeric = int.TryParse(newPickNumber[0].ToString(), out int n);
            var is2Numeric = int.TryParse(newPickNumber[1].ToString(), out int n2);
            var firstNumber = is1Numeric ? newPickNumber[0].ToString() : "";
            var secondNumber = is2Numeric ? newPickNumber[1].ToString() : "";
            newPickNumber = firstNumber + secondNumber;

            if (string.IsNullOrEmpty(newPickNumber)) return;

            var newDraftPickNumber = Convert.ToInt32(newPickNumber);

            if (newDraftPickNumber != draftPickNumber)
            {
                draftPickNumber = newDraftPickNumber;

                txtNumber.Text = textRead;

                GetAllCardReviews();
            }
        }

        private void MakeCardReviewLabel(CardNameLocation location)
        {
            Button bestGuessLabel = new Button
            {
                Name = GetBestGuessLabelName(location),
                Style = (Style)Resources["MyButtonStyle"],
                Content = "Loading..",
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 175,
                Height = 30,
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.1f },
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Visibility = Visibility.Hidden,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(bestGuessLabel, location.Rect.Left - 10);
            Canvas.SetTop(bestGuessLabel, location.Rect.Top - 200);
            MainCanvas.Children.Add(bestGuessLabel);
            MainCanvas.RegisterName(bestGuessLabel.Name, bestGuessLabel);
            bestGuessLabel.PreviewMouseDown += OnMainLabelClick;
        }
    

        private void UpdateCardReview(CardNameLocation location, List<CardGuess> cardGuesses)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => InternalUpdateCardReview(location, cardGuesses)));
            }
            catch { /*do nothing*/}
        }

        private void InternalUpdateCardReview(CardNameLocation location, List<CardGuess> cardGuesses)
        {
            currentCardGuessesByLocation[location] = cardGuesses;

            var name = GetBestGuessLabelName(location);
            var myTextBox = (Button)this.FindName(name);
            myTextBox.Visibility = Visibility.Visible;

            float rating = float.Parse(cardGuesses.Last().Review.AverageRating, CultureInfo.InvariantCulture.NumberFormat);
            myTextBox.Background = backgroundBrush;
            myTextBox.Foreground = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(Utils.GetColorForRating((decimal)rating)));

            myTextBox.Content = cardGuesses.Last().Review.ToString() + (cardGuesses.Count > 1 ? "*" : "");
            Utils.UpdateFontSizeToFit(myTextBox);

            if (DraftScreen.GetCardNameLocationsAvailable(draftPickNumber).Last().Equals(location))
            {
                latestStoryboardRefreshButton.Stop(this);
            }
        }

        private void OnMainLabelClick(object sender, RoutedEventArgs e)
        {
            ProcessWindowManager.Instance.ReleaseFocus();
            string name = ((Button)sender).Name;
            var location = GetLocationFromBestGuessLabelName(name);
            List<CardGuess> guesses;
            currentCardGuessesByLocation.TryGetValue(location, out guesses);
            if (guesses == null) return;

            ScrollViewer commentsScrollViewer = this.FindName("canvas" + location.ToString() + "comments") as ScrollViewer;
            if (commentsScrollViewer == null)
            {
                commentsScrollViewer = new ScrollViewer
                {
                    Name = "canvas" + location.ToString() + "comments",
                    Width = 210,
                    Height = 300,
                    Background = backgroundBrush,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    BorderThickness = new Thickness(0)
                };

                Canvas.SetLeft(commentsScrollViewer, location.Rect.Left - 20);
                Canvas.SetTop(commentsScrollViewer, location.Rect.Top - 175);
                MainCanvas.Children.Add(commentsScrollViewer);
                MainCanvas.RegisterName(commentsScrollViewer.Name, commentsScrollViewer);

                TextBox comments = new TextBox
                {
                    Width = 210,
                    Height = 300,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    CaretBrush = new SolidColorBrush(Colors.Transparent),
                    Background = backgroundBrush,
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Name = "textbox" + location.ToString() + "comments",
                    Text = GetCommentsText(guesses.Last())
                };

                commentsScrollViewer.Content = comments;
                commentsScrollViewer.RegisterName(comments.Name, comments);
                ((Control)sender).MouseLeave += (s, args) => { if (!commentsScrollViewer.IsMouseOver) OnMouseLeaveMainLabel(commentsScrollViewer, sender, e); };
                comments.MouseLeave += (s, args) => { if (!((Control)sender).IsMouseOver) OnMouseLeaveMainLabel(commentsScrollViewer, sender, e); };

            }
            else
            {
                commentsScrollViewer.Visibility = commentsScrollViewer.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                var textBox = (TextBox)commentsScrollViewer.FindName("textbox" + location.ToString() + "comments");
                textBox.Text = GetCommentsText(guesses.Last());
            }

            if (commentsScrollViewer.Visibility == Visibility.Visible)
            {
                ProcessWindowManager.Instance.ForceFocus();
            }
        }

        private void OnMouseLeaveMainLabel(ScrollViewer commentsScrollViewer, object sender, RoutedEventArgs e)
        {
            if (commentsScrollViewer.Visibility == Visibility.Visible)
            {
                OnMainLabelClick(sender, e);
            }
        }

        private void tb_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                //If nothing has been entered yet.
                if (((TextBox)sender).Foreground == Brushes.Gray)
                {
                    ((TextBox)sender).Text = "";
                    ((TextBox)sender).Foreground = Brushes.White;
                }
            }
        }

        private static string GetCommentsText(CardGuess guess)
        {
            var commentsText = "";

            ReviewDatabase.ReviewDataSource.CardReview review = guess.Review;
            Dictionary<string, string> commentsByReviewer = review.CommentsByReviewer;
            Dictionary<string, string> ratingsByReviewer = review.RatingsByReviewer;

            foreach (var reviewer in commentsByReviewer.Keys)
            {
                string comment = commentsByReviewer[reviewer];
                commentsText += reviewer + " (" + ratingsByReviewer[reviewer] + ")";
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

            return commentsText;
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            GetAllCardReviews();
        }

        private void PausePlayButtonClick(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;

            if (!isPaused && !isWindowSetup)
            {
                isWindowSetup = true;
                ProcessWindowManager.Instance.Init("Eternal");

                SetupPickNumber();
                SetupCardReviews();
            }

            MainCanvas.Visibility = isPaused ? Visibility.Hidden : Visibility.Visible;

            var icon = isPaused ? "resume" : "pause";
            ((Button)sender).Content = new Image {
                Source = new BitmapImage(new Uri("Images/" + icon + "_icon.png", UriKind.Relative)),
            };

            if (!isPaused)
            {
                ProcessWindowManager.Instance.BringWindowForward();
                SetupPickNumber();
            }
        }

        private void OnShowColorsRulerClick(object sender, RoutedEventArgs e)
        {
            MakeUnmakeColorsRuler(false);
        }

        private void OnShowChooseColorsClick(object sender, RoutedEventArgs e)
        {
            UserColorsPanel.Visibility = UserColorsPanel.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

            var colors = Settings.Instance.GetChosenRatingsColors();
            int i = 0;
            foreach (var color in colors)
            {
                var name = "Rating_" + (int) (Settings.RatingsColorsThresholds[i] * 10);
                (FindName(name) as Button).Background = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(System.Drawing.Color.FromName(color)));
                i++;
            }
        }

        private void OnColorModeChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (e.AddedItems[0] as ComboBoxItem).Content as string;
            Settings.Instance.SetNewColorModeString(text);
            UpdateCardBestGuessLabelColors();
            OnShowChooseColorsClick(null, null);
            OnShowChooseColorsClick(null, null);
            MakeUnmakeColorsRuler(true);
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsCanvas.Visibility = SettingsCanvas.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void OnHelpButtonClick(object sender, RoutedEventArgs e)
        {
            DialogWindowManager.ShowHelp(HolderCanvas);
        }

        private void QuitButtonClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private static string GetBestGuessLabelName(CardNameLocation location)
        {
            return "label" + location.ToString() + "bestGuess";
        }

        private CardNameLocation GetLocationFromSecondaryLabelName(string labelName)
        {
            return CardNameLocation.FromString(labelName.Replace("textbox", "").Replace("guess", ""));
        }

        private static CardNameLocation GetLocationFromBestGuessLabelName(string labelName)
        {
            return CardNameLocation.FromString(labelName.Replace("label", "").Replace("bestGuess", ""));
        }

        #region Window styles

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        [Flags]
        public enum ExtendedWindowStyles { WS_EX_TOOLWINDOW = 0x00000080 }

        public enum GetWindowLongFields { GWL_EXSTYLE = (-20) }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        #endregion
    }
}
