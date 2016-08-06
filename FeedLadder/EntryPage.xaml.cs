using System;
using System.Net;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class EntryPage : Page
    {
        private string title;
        private string link;
        private string body;
        private const string guideURL = "http://reader.livedwango.com/contents/guide";

        public EntryPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                ProgressIndicator.IsActive = true;
                FeedItem item = e.Parameter as FeedItem;
                if(item == null)
                {
                    title = "Live Dwango Reader Guide";
                    link = guideURL;
                    BrowserComponent.Navigate(new Uri(guideURL));
                    PageTitle.Text = title;
                    return;
                }
                title = item.Title;
                link = item.Link;
                body = item.Body;

                PageTitle.Text = title;

                //BrowserComponent.NavigateToString(formatHTML(body));

                if (string.IsNullOrEmpty(body))
                {
                    BrowserComponent.Visibility = Visibility.Collapsed;
                    NoBodyLabel.Visibility = Visibility.Visible;
                    ProgressIndicator.IsActive = false;
                    //BrowserMask.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BrowserComponent.NavigateToString(formatHTML(body));
                }
            }
        }

        private string formatHTML(string source)
        {
            // Don't use DOCTYPE https://msdn.microsoft.com/ja-jp/library/dn384051(v=vs.85).aspx
            // Fit to device width https://msdn.microsoft.com/ja-jp/library/hh869615(v=vs.85).aspx
            string header = "<html><head><style type=\"text/css\">@-ms-viewport {width: device-width; zoom: 1.0;} img {max-width: 100%; width: auto; height: auto;}</style></head><body>";
            string footer = "</body></html>";
            //source.Replace("\'", "&#039;");
            source.Replace("<html>", "");
            source.Replace("<body>", "");
            source.Replace("</body>", "");
            source.Replace("</html>", "");
            System.Text.RegularExpressions.Regex.Replace(source, "<head*/head>","");
            source = header + source + footer;
            return source;
        }

        // If content loaded, show content
        // from http://stackoverflow.com/questions/5602132/is-it-possible-to-change-the-background-color-of-the-webbrowser-control-before-l
        private void BrowserComponent_LoadCompleted(object sender, NavigationEventArgs e)
        {
            BrowserComponent.Opacity = 1;
            BrowserComponent.Visibility = Visibility.Visible;
            ProgressIndicator.IsActive = false;
        }

        // hook navigation
        private async void BrowserComponent_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Uri uri = args.Uri;
            if (uri != null && uri.AbsoluteUri.StartsWith("http") && !uri.AbsoluteUri.StartsWith(guideURL))
            {
                // cancel navigation
                args.Cancel = true;
                // open via browser directly
                bool success = await Launcher.LaunchUriAsync(uri);
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            //dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            dataTransferManager.DataRequested += (s, args) =>
            {
                DataRequest request = args.Request;
                request.Data.Properties.Title = title;
                request.Data.SetText(title);
                request.Data.SetWebLink(new Uri(link));
            };
            DataTransferManager.ShowShareUI();
        }

        // Open item in browser
        private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            bool success = await Launcher.LaunchUriAsync(new Uri(link));
        }
    }
}
