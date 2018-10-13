using KimoEt.EternalSpecifics;
using KimoEt.ProcessWindow;
using KimoEt.ReviewDatabase;
using KimoEt.UI;
using KimoEt.Utililties;
using KimoEt.Utilities;
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
        private Dictionary<CardNameLocation, HashSet<CardGuess>> currentCardGuessesByLocation = new Dictionary<CardNameLocation, HashSet<CardGuess>>();
        public static SolidColorBrush backgroundBrush = new SolidColorBrush(Utils.ConvertStringToColor("#1E1E1E")) { Opacity = 0.7f };
        public static readonly decimal VERSION = 1.2m;
        public static string VERSION_STRING = VERSION.ToString(CultureInfo.InvariantCulture);

        public static double ScaleFactorX;
        public static double ScaleFactorY;
        public Screen currentScreen = DraftScreen.Instance;

        HotKey hotKey;

        public MainWindow()
        {
            InitializeComponent();

            ProcessWindowManager.Instance.BindLocationToThisWindow(this);
            Settings.LoadSettingsFromDisk();
            TierListDownloader.DownloadTDCs();
            TierListDownloader.DownloadSunyveils();

            if (Settings.Instance.LastVersionUsed < VERSION)
            {
                DialogWindowManager.ShowInitialWarning(HolderCanvas);
                Settings.Instance.SetNewLastVersionUsed(VERSION);
            }

            hotKey = new HotKey(Key.F, KeyModifier.Ctrl, OnSearchShortcut);
            KimoEtTitle.Text = "KimoEt - v" + VERSION_STRING;
            FontFamily = new FontFamily("Segoe UI");

            Utils.MakePanelDraggable(CanvasBorder, HolderCanvas, this, this);
            Canvas.SetLeft(CanvasBorder, Settings.Instance.MenuPoint.X);
            Canvas.SetTop(CanvasBorder, Settings.Instance.MenuPoint.Y);

            ComboboxColorMode.SelectedIndex = Settings.Instance.RatingColorMode;
            ComboboxTierlistMode.SelectedIndex = Settings.Instance.TierListMode;
            CheckForUpdates();

            Loaded += (s, e) =>
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
                Updater.Update(VERSION, Process.GetCurrentProcess().Id);
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

            if (!(element.RenderTransform is TranslateTransform))
            {
                element.RenderTransform = new TranslateTransform();
            }

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
            //here we want to make sure we unmake them all, so use the draft screen
            //the locations will just be used for row x column, so its fine
            foreach (var location in DraftScreen.Instance.GetAllCardNameLocations())
            {
                UnmakeCardReviewLabel(location);
            }

            foreach (var location in currentScreen.GetAllCardNameLocations())
            {
                MakeCardReviewLabel(location);
            }
        }

        private Storyboard latestStoryboardRefreshButton;
        private void GetAllCardReviews()
        {
            if (isPaused) return;

            SetupCardReviews();

            foreach (var location in currentScreen.GetAllCardNameLocations())
            {
                var myTextBox = (Button)FindName(GetBestGuessLabelName(location));
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

            foreach (var location in currentScreen.GetCardNameLocationsAvailable(draftPickNumber))
            {
                Task.Run(RecognizeForLocation(location, currentScreen, draftPickNumber));
            }
        }

        private Action RecognizeForLocation(CardNameLocation location, Screen recognizingScreen, int recognizingPickNumber)
        {
            return () =>
            {
                var cardGuesses = CardRecognitionManager.Instance.ReadCardName(location);

                if ((cardGuesses == null || cardGuesses.Count == 0)
                && currentScreen == recognizingScreen
                && recognizingPickNumber == draftPickNumber
                && !isPaused
                && currentScreen != ForgeScreen.Instance)
                {

                   //we failed the recognition, set a new task for this location for later:
                   Console.WriteLine(Utils.GetSecondsSinceEpoch() + " - we failed the recognition, set a new task for this location for later");
                    Task.Delay(1000).ContinueWith((obj) =>
                    {
                        Console.WriteLine(Utils.GetSecondsSinceEpoch() + " - starting now!");
                        Task.Run(RecognizeForLocation(location, recognizingScreen, recognizingPickNumber));
                    });

                }
                UpdateCardReview(location, cardGuesses);
            };
        }

        private void SetupPickNumber()
        {
            Screen checkingCurrentScreen;
            if (currentScreen == null)
            {
                checkingCurrentScreen = DraftScreen.Instance;
            }
            else
            {
                checkingCurrentScreen = currentScreen;
            }

            RECT pickRect = checkingCurrentScreen.GetRectForPickNumber();

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
                        checkingCurrentScreen = DraftScreen.Instance;
                        var pickNumber = GetCurrentPickNumber(checkingCurrentScreen);

                        if (pickNumber == null || !pickNumber.StartsWith(checkingCurrentScreen.PickNumberStartsWith()))
                        {
                            checkingCurrentScreen = ForgeScreen.Instance;
                            pickNumber = GetCurrentPickNumber(checkingCurrentScreen);
                        }

                        if (pickNumber == null || !pickNumber.StartsWith(checkingCurrentScreen.PickNumberStartsWith()))
                        {
                            SyncHideMainCanvas(true);
                            continue;
                        }

                        SyncHideMainCanvas(false);

                        bool changedScreens = currentScreen != checkingCurrentScreen;
                        currentScreen = checkingCurrentScreen;

                        try
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() => OnNewPickNumber(pickNumber, txtNumber, changedScreens)));

                        } catch (Exception) { /*do nothing*/}
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        ManualResetEvent syncOperation;
        private void SyncHideMainCanvas(bool hide)
        {
            syncOperation = new ManualResetEvent(false);

            try
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action)(() => HideMainCanvasOnUiThread(hide))
                );

            }
            catch (Exception) { /*do nothing*/}
            
            syncOperation.WaitOne();
        }

        private void HideMainCanvasOnUiThread(bool hide)
        {
            MainCanvas.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            syncOperation.Set();
        }

        private string GetCurrentPickNumber(Screen checkingCurrentScreen)
        {
            var pickNumber = CardRecognitionManager.Instance.ReadPickNumber(ProcessWindowManager.Instance.GetWindowAreaBitmap(checkingCurrentScreen.GetRectForPickNumber(isDoubleDigitsPickNumber), false));
            if (pickNumber == null || !pickNumber.StartsWith(checkingCurrentScreen.PickNumberStartsWith()))
            {
                isDoubleDigitsPickNumber = !isDoubleDigitsPickNumber;
                pickNumber = CardRecognitionManager.Instance.ReadPickNumber(ProcessWindowManager.Instance.GetWindowAreaBitmap(checkingCurrentScreen.GetRectForPickNumber(isDoubleDigitsPickNumber), false));
            }

            return pickNumber;
        }


        private void OnNewPickNumber(string newPickNumber, TextBox txtNumber, bool changedScreens)
        {
            var textRead = newPickNumber;
            newPickNumber = newPickNumber.Replace(currentScreen.PickNumberStartsWith(), "").Trim();
            newPickNumber = currentScreen.GetExceptionPickNumber(newPickNumber);

            if (string.IsNullOrWhiteSpace(newPickNumber))
                return;


            var is1Numeric = int.TryParse(newPickNumber[0].ToString(), out int n);
            var is2Numeric = int.TryParse(newPickNumber[1].ToString(), out int n2);
            var firstNumber = is1Numeric ? newPickNumber[0].ToString() : "";
            var secondNumber = is2Numeric ? newPickNumber[1].ToString() : "";
            newPickNumber = firstNumber + secondNumber;

            if (string.IsNullOrEmpty(newPickNumber)) return;

            var newDraftPickNumber = Convert.ToInt32(newPickNumber);

            if (newDraftPickNumber != draftPickNumber || changedScreens)
            {
                draftPickNumber = newDraftPickNumber;

                txtNumber.Text = textRead;

                GetAllCardReviews();
            }
        }

        private void UnmakeCardReviewLabel(CardNameLocation location)
        {
            var btn = FindName(GetBestGuessLabelName(location)) as Button;
            if (btn == null)
            {
                return;
            }
            btn.PreviewMouseDown -= OnMainLabelClick;
            MainCanvas.UnregisterName(btn.Name);
            MainCanvas.Children.Remove(btn);
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
    

        private void UpdateCardReview(CardNameLocation location, HashSet<CardGuess> cardGuesses)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => InternalUpdateCardReview(location, cardGuesses)));
            }
            catch (Exception) { /*do nothing*/}
        }

        private void InternalUpdateCardReview(CardNameLocation location, HashSet<CardGuess> cardGuesses)
        {
            currentCardGuessesByLocation[location] = cardGuesses;

            var name = GetBestGuessLabelName(location);
            var myTextBox = FindName(name) as Button;
            if (myTextBox == null) return;
            myTextBox.Visibility = Visibility.Visible;

            if (cardGuesses != null)
            {
                float rating = CardReviewForUiUtils.GetRatingForColor(cardGuesses.Last());
                myTextBox.Background = backgroundBrush;
                myTextBox.Foreground = new SolidColorBrush(Utils.GetMediaColorFromDrawingColor(Utils.GetColorForRating((decimal)rating)));

                myTextBox.Content = CardReviewForUiUtils.GetRatingLabel(cardGuesses);
            } else
            {
                myTextBox.Background = backgroundBrush;
                myTextBox.Foreground = new SolidColorBrush(Colors.Red);

                myTextBox.Content = (currentScreen.IsForge() ? "N/A or " : "") + "FAILED RECOGNITION";
            }

            Utils.UpdateFontSizeToFit(myTextBox);

            if (currentScreen.GetCardNameLocationsAvailable(draftPickNumber).Last().Equals(location))
            {
                latestStoryboardRefreshButton.Stop(this);
            }
        }

        private void OnMainLabelClick(object sender, RoutedEventArgs e)
        {
            ProcessWindowManager.Instance.ReleaseFocus();

            string name = ((Button)sender).Name;
            var location = GetLocationFromBestGuessLabelName(name);

            HashSet<CardGuess> guesses;
            currentCardGuessesByLocation.TryGetValue(location, out guesses);

            if (guesses == null) return;

            ScrollViewer commentsScrollViewer = FindName("canvas" + location.ToString() + "comments") as ScrollViewer;
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
                    Text = CardReviewForUiUtils.GetCommentsText(guesses.Last())
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
                textBox.Text = CardReviewForUiUtils.GetCommentsText(guesses.Last());
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

        private void OnTierlistModeChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (e.AddedItems[0] as ComboBoxItem).Content as string;
            Settings.Instance.SetNewTierListString(text);
            GetAllCardReviews();
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsCanvas.Visibility = SettingsCanvas.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnHelpButtonClick(object sender, RoutedEventArgs e)
        {
            DialogWindowManager.ShowHelp(HolderCanvas);
        }

        private void OnSearchShortcut(HotKey obj)
        {
            ChangeSearchBarVisibilityAndFocus();

            SearchTextBox.PreviewKeyDown += (s, eventArgs) =>
            {
                if (eventArgs.Key == Key.Return)
                {
                    SearchCardByName(SearchTextBox.Text, true);
                    ChangeSearchBarVisibilityAndFocus();
                    eventArgs.Handled = true;
                }
                else if (eventArgs.Key == Key.Escape)
                {
                    ChangeSearchBarVisibilityAndFocus();
                    eventArgs.Handled = true;
                }
            };
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchCardByName(SearchTextBox.Text, false);
        }

        private void SearchCardByName(string searchedCardName, bool showBestCard)
        {
            if (string.IsNullOrEmpty(searchedCardName))
            {
                SearchResults.Children.RemoveRange(0, SearchResults.Children.Count);
                return;
            }

            var bestMatches = RecognitionUtils.FindBestCardReviewMatches(searchedCardName);

            if (bestMatches.Count > 1 || !showBestCard)
            {
                SearchResults.Children.RemoveRange(0, SearchResults.Children.Count);
                foreach (var match in bestMatches)
                {
                    TextBlock searchResult = new TextBlock
                    {
                        Text = match.ReviewByTeam[Team.TDC].Name,
                        Foreground = Brushes.White,
                    };
                    searchResult.PreviewMouseDown += (e, args) =>
                    {
                        SearchCardByName(searchResult.Text, true);
                    };
                    SearchResults.Children.Add(searchResult);
                }
                return;
            }

            var newGuess = bestMatches.Last();

            StackPanel searchedCommentsPanel = FindName("ManualSearchControl") as StackPanel;
            if (searchedCommentsPanel == null)
            {
                searchedCommentsPanel = new StackPanel
                {
                    Name = "ManualSearchControl",
                    Orientation = Orientation.Vertical,
                    Background = backgroundBrush,
                    Width = 210,
                };

                Canvas.SetLeft(searchedCommentsPanel, 1275);
                Canvas.SetTop(searchedCommentsPanel, 420);
                MainCanvas.Children.Add(searchedCommentsPanel);
                MainCanvas.RegisterName(searchedCommentsPanel.Name, searchedCommentsPanel);
                
                ScrollViewer scrollViewerComments = new ScrollViewer
                {
                    Width = 210,
                    Height = 300,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    BorderThickness = new Thickness(0)
                };

                StackPanel titleCommentsPanel = new StackPanel
                {
                    Background = Brushes.DarkCyan,
                    Orientation = Orientation.Horizontal,
                    Height = 30,
                };
                searchedCommentsPanel.Children.Add(titleCommentsPanel);

                TextBox cardTitle = new TextBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.DarkCyan,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Colors.White),
                    Name = "cardTitle_ManualSearchControl",
                    Text = CardReviewForUiUtils.GetRatingLabel(new HashSet<CardGuess>() { newGuess }),
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(5,5,5,5),
                    Width = 175,
                };
                Utils.MakePanelDraggable(searchedCommentsPanel, HolderCanvas, this, null);
                Utils.UpdateFontSizeToFit(cardTitle);
                titleCommentsPanel.Children.Add(cardTitle);
                searchedCommentsPanel.RegisterName(cardTitle.Name, cardTitle);

                Button closeBtn = new Button()
                {
                    Content = "X",
                    Height = 20,
                    Width = 20,
                    Background = Brushes.DarkCyan,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10, 0, 10, 0),
                    Foreground = new SolidColorBrush(Colors.White),
                    Padding = new Thickness(5, 0, 5, 0),
                };
                titleCommentsPanel.Children.Add(closeBtn);
                closeBtn.Click += (e, args) =>
                {
                    searchedCommentsPanel.Visibility = Visibility.Hidden;
                };

                TextBox comments = new TextBox
                {
                    Width = 210,
                    Height = 300,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(0,20,0,0),
                    TextWrapping = TextWrapping.Wrap,
                    CaretBrush = new SolidColorBrush(Colors.Transparent),
                    Background = backgroundBrush,
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Name = "textbox_ManualSearchControl",
                    Text = CardReviewForUiUtils.GetCommentsText(newGuess),
                };
                scrollViewerComments.Content = comments;

                searchedCommentsPanel.Children.Add(scrollViewerComments);
                searchedCommentsPanel.RegisterName(comments.Name, comments);
            }
            else
            {
                searchedCommentsPanel.Visibility = Visibility.Visible;
                (FindName("textbox_ManualSearchControl") as TextBox).Text = CardReviewForUiUtils.GetCommentsText(newGuess);
                (FindName("cardTitle_ManualSearchControl") as TextBox).Text = CardReviewForUiUtils.GetRatingLabel(new HashSet<CardGuess>() { newGuess });
            }
        }

        private void ChangeSearchBarVisibilityAndFocus()
        {
            SearchTextBox.Visibility = SearchTextBox.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            if (SearchTextBox.Visibility == Visibility.Visible)
            {
                ProcessWindowManager.Instance.ForceFocus();
                ProcessWindowManager.Instance.BringOurWindowForward();
                SearchTextBox.Focus();
            }
            else
            {
                SearchTextBox.Text = "";
                RefreshBtn.Focus();
                SearchTextBox.Visibility = Visibility.Hidden;
                ProcessWindowManager.Instance.ReleaseFocus();
                ProcessWindowManager.Instance.BringWindowForward();
            }
        }

        private void QuitButtonClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
