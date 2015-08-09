using System;
using System.Windows;
using System.Net;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows.Media;

namespace FeedLadder
{
    public partial class EntryPage : PhoneApplicationPage
    {
        private string title;
        private string link;
        private string body;

        public EntryPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.IsNavigationInitiator)
            {
                NavigationContext.QueryString.TryGetValue("title", out title);
                title = HttpUtility.UrlDecode(title);
                NavigationContext.QueryString.TryGetValue("link", out link);
                link = HttpUtility.UrlDecode(link);
                NavigationContext.QueryString.TryGetValue("body", out body);
                body = HttpUtility.UrlDecode(body);

                PageTitle.Text = title;

                //BrowserComponent.NavigateToString(formatHTML(body));

                if (String.IsNullOrEmpty(body))
                {
                    BrowserComponent.Visibility = Visibility.Collapsed;
                    NoBodyLabel.Visibility = Visibility.Visible;
                    progressIndicator.IsVisible = false;
                    //BrowserMask.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BrowserComponent.NavigateToString(formatHTML(body));
                }
            }
        }

        // Open item in browser
        private void abmOpen_Click(object sender, EventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(this.link);
            task.Show();
        }

        private void abmShare_Click(object sender, EventArgs e)
        {
            ShareLinkTask task = new ShareLinkTask();
            task.Title = this.title;
            task.LinkUri = new Uri(this.link);
            task.Show();
        }

        private string formatHTML(string source)
        {
            // Get color resource and convert ARGB to RGB (#AARRGGBB -> #RRGGBB)
            string backgroundColor = (App.Current.Resources["PhoneBackgroundBrush"] as SolidColorBrush).Color.ToString().Remove(1,2);
            string textColor = (App.Current.Resources["PhoneForegroundBrush"] as SolidColorBrush).Color.ToString().Remove(1, 2);
            string linkColor = (App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color.ToString().Remove(1, 2);

            // from http://stackoverflow.com/questions/9124166/how-to-make-webbrowser-control-with-black-background-in-windows-phone-7-1
            // The min-width/max-width property is enabled only under the strict !DOCTYPE. => add "<!DOCTYPE html>" on top
            // from http://msdn.microsoft.com/en-us/library/ie/ms530811(v=vs.85).aspx
            string header = "<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, user-scalable=0\"/><style type=\"text/css\">body{background-color: " + backgroundColor + "; color: " + textColor + ";}a:link{color: " + linkColor + ";}img{max-width: 100%; width: auto; height: auto;}</style></head><body>";
            string footer = "</body></html>";
            //source.Replace("\'", "&#039;");
            source.Replace("<html>", "");
            source.Replace("<body>", "");
            source.Replace("</body>", "");
            source.Replace("</html>", "");
            source = header + source + footer;
            return source;
        }

        // If content loaded, show content
        // from http://stackoverflow.com/questions/5602132/is-it-possible-to-change-the-background-color-of-the-webbrowser-control-before-l
        private void BrowserComponent_LoadCompleted(object sender, NavigationEventArgs e)
        {
            BrowserComponent.Opacity = 1;
            BrowserComponent.Visibility = Visibility.Visible;
            progressIndicator.IsVisible = false;
        }

        // hook navigation
        // http://blog.ch3cooh.jp/entry/20111112/1321110983
        private void BrowserComponent_Navigating(object sender, NavigatingEventArgs e)
        {
            Uri uri = e.Uri;
            if (uri != null && uri.AbsoluteUri.StartsWith("http"))
            {
                // cancel navigation
                e.Cancel = true;
                // open via browser directly
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = uri;
                task.Show();
            }
        }

    }
}