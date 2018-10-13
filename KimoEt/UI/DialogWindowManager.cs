using KimoEt.ProcessWindow;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KimoEt.UI
{
    public class DialogWindowManager
    {

        public static void ShowInitialWarning(Canvas canvas)
        {
            ProcessWindowManager.Instance.ForceFocus();
            var brush = MainWindow.backgroundBrush.Clone();
            brush.Opacity = 1f;

            StackPanel warningPanel = new StackPanel()
            {
                Background = brush,
                Orientation = Orientation.Vertical,
            };

            System.Windows.Controls.Image image = new System.Windows.Controls.Image()
            {
                Source = new BitmapImage(new Uri("Images/warning_icon.png", UriKind.Relative)),
                Width = 100,
                Height = 100
            };
            warningPanel.Children.Add(image);

            TextBlock warningTextBlock = new TextBlock()
            {
                Text = "Drifter [TDC]:\n\n" +
                "\"This tool will be less accurate as your draft goes on because factors such as being on-faction vs on the splash," +
                " curve, open signals, and specific synergies will increasingly change the relative value of your picks. \n\n" +
                "Visit #draft on the Eternal discord if you need more help!\"",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(10, 10, 10, 10),
                TextWrapping = TextWrapping.Wrap,
                Width = 400,
                FontSize = 15,
            };
            warningPanel.Children.Add(warningTextBlock);

            Button okButton = new Button()
            {
                Content = "OK!",
                Width = 100,
                Height = 30,
                Margin = new Thickness(10, 10, 10, 10),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Colors.White)
            };
            warningPanel.Children.Add(okButton);
            okButton.Click += (e, args) =>
            {
                ProcessWindowManager.Instance.ReleaseFocus();
                canvas.Children.Remove(warningPanel);
            };

            warningPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(warningPanel, 1920 / 2 - warningPanel.DesiredSize.Width / 2);
            Canvas.SetTop(warningPanel, 1080 / 2 - warningPanel.DesiredSize.Height / 2);
            canvas.Children.Add(warningPanel);
        }

        public static void ShowHelp(Canvas canvas)
        {
            var panel = canvas.FindName("HelpDialog") as StackPanel;
            if (panel != null)
            {
                canvas.Children.Remove(panel);
                canvas.UnregisterName(panel.Name);
                return;
            }

            ProcessWindowManager.Instance.ForceFocus();
            var brush = MainWindow.backgroundBrush.Clone();
            brush.Opacity = 1f;

            StackPanel helpPanel = new StackPanel()
            {
                Name = "HelpDialog",
                Background = brush,
                Orientation = Orientation.Vertical,
            };

            StackPanel kimoEtInfoPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };
            helpPanel.Children.Add(kimoEtInfoPanel);

            System.Windows.Controls.Image image = new System.Windows.Controls.Image()
            {
                Source = new BitmapImage(new Uri("Images/KimoEt.png", UriKind.Relative)),
                Width = 110,
                Height = 110,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 10, 10, 0),
            };
            kimoEtInfoPanel.Children.Add(image);

            TextBlock appInfoTextBlock = new TextBlock()
            {
                Text = "KimoEt version " + MainWindow.VERSION_STRING + "\n\n",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 15,
            };
            appInfoTextBlock.Inlines.Add(new Run("More info: "));
            Hyperlink link = new Hyperlink(new Run("https://github.com/skmll/KimoEt"))
            {
                NavigateUri = new Uri("https://github.com/skmll/KimoEt")
            };
            link.RequestNavigate += Hyperlink_RequestNavigate;
            appInfoTextBlock.Inlines.Add(link);
            appInfoTextBlock.Inlines.Add(new Run("\nFor issues or ideas, please contact me in Eternal discord @zkeme"));

            kimoEtInfoPanel.Children.Add(appInfoTextBlock);

            TextBlock tdcInfoTextBlock = new TextBlock()
            {
                Text = "Huge thanks to the creators of the tier list from the Team Draft Chaff (a.k.a TDC), namely:\n" +
                "Drifter, flash2351, Isomorphic and Mgallop\n" +
                "Tier List: ",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(10, 0, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                FontSize = 15,
                LineHeight = 30,
            };
            Hyperlink linkTierList = new Hyperlink(new Run("https://docs.google.com/spreadsheets/d/1NH1i_nfPKhXO53uKYgJYICrTx_XSqDC88b2I3e0vsc0"))
            {
                NavigateUri = new Uri("https://docs.google.com/spreadsheets/d/1NH1i_nfPKhXO53uKYgJYICrTx_XSqDC88b2I3e0vsc0")
            };
            linkTierList.RequestNavigate += Hyperlink_RequestNavigate;
            tdcInfoTextBlock.Inlines.Add(linkTierList);
            helpPanel.Children.Add(tdcInfoTextBlock);

            TextBlock sunyveilInfoTextBlock = new TextBlock()
            {
                Text = "Huge thanks to Sunyveil for adapting his tier list for me to be able to use it!\n" +
                "Tier List: ",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(10, 0, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                FontSize = 15,
                LineHeight = 30,
            };
            Hyperlink sunylinkTierList = new Hyperlink(new Run("https://docs.google.com/spreadsheets/d/1IeAah8Lx-c1-rQcJ_sBx-z0Eedx_Cjz8jX13WC4YwVc"))
            {
                NavigateUri = new Uri("https://docs.google.com/spreadsheets/d/1IeAah8Lx-c1-rQcJ_sBx-z0Eedx_Cjz8jX13WC4YwVc")
            };
            sunylinkTierList.RequestNavigate += Hyperlink_RequestNavigate;
            sunyveilInfoTextBlock.Inlines.Add(sunylinkTierList);
            helpPanel.Children.Add(sunyveilInfoTextBlock);

            Button okButton = new Button()
            {
                Content = "CLOSE",
                Width = 100,
                Height = 30,
                Margin = new Thickness(10, 10, 10, 10),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Colors.White)
            };
            helpPanel.Children.Add(okButton);
            okButton.Click += (e, args) =>
            {
                ProcessWindowManager.Instance.ReleaseFocus();
                canvas.Children.Remove(helpPanel);
            };

            helpPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(helpPanel, 1920 / 2 - helpPanel.DesiredSize.Width / 2);
            Canvas.SetTop(helpPanel, 1080 / 2 - helpPanel.DesiredSize.Height / 2);
            canvas.Children.Add(helpPanel);
            canvas.RegisterName(helpPanel.Name, helpPanel);
        }

        private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.ToString());
        }
    }
}
