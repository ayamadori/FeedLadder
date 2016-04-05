using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class FeedPage : Page
    {
        private const string domainURL = "http://reader.livedwango.com";
        private string subscribeID;
        private string title;
        private string apiKey;
        private long timeStamp;
        private bool pressedPin;
        private FeedItems feedItems;
        private const string adBlockRegExp = "^[【 ]*(AD|PR|ＡＤ|ＰＲ)[ 】]*[:：]";
        private const int delta = 50;

        public FeedPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                apiKey = e.Parameter as string;
                int group = (Windows.UI.Xaml.Application.Current as Application).GroupIndex;
                int item = (Windows.UI.Xaml.Application.Current as Application).ItemIndex;
                if (apiKey == null)
                {
                    PageTitle.Text = "Feeds";
                    FeedListResult.Visibility = Visibility.Collapsed;
                    PrevButton.IsEnabled = false;
                    NextButton.IsEnabled = false;
                    return;
                }
                else
                {
                    PrevButton.IsEnabled = true;
                    NextButton.IsEnabled = true;
                }
                title = (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].Title;
                PageTitle.Text = title;
                subscribeID = (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].SubscribeID;
                Unread(subscribeID);

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                //JST = UTC + 9hour
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }

        void ContentPanel_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            double deltaX = e.Cumulative.Translation.X;
            double deltaY = e.Cumulative.Translation.Y;
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (deltaX > delta) // flick to right
                {
                    //MessageBox.Show("Flick to Right: " + deltaX);
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    GoPreviousFeed();
                }
                else if (deltaX < delta * (-1))// flick to left
                {
                    //MessageBox.Show("Flick to Left: " + deltaX);
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    GoNextFeed();
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Unread
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        private async void Unread(string subscribe_id)
        {
            FeedListResult.ItemsSource = null;
            FeedListResult.Visibility = Visibility.Collapsed;
            NoItemLabel.Visibility = Visibility.Collapsed;
            ProgressIndicator.IsActive = true;

            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "subscribe_id", subscribe_id }, { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/pin/unread"), new HttpFormUrlEncodedContent(postData));
                string res = await response.Content.ReadAsStringAsync();

                // refer to http://blogs.gine.jp/taka/archives/2106
                var serializer = new DataContractJsonSerializer(typeof(FeedItems));
                byte[] bytes = Encoding.UTF8.GetBytes(res);
                MemoryStream ms = new MemoryStream(bytes);
                feedItems = serializer.ReadObject(ms) as FeedItems;
                res = "";

                // AD feed block (cannnot use "\s" ?)
                // from http://www.atmarkit.co.jp/fdotnet/dotnettips/815listremove/listremove.html
                var roamingSettings = ApplicationData.Current.RoamingSettings;
                object block = roamingSettings.Values["AdBlockEnableBool"];
                if (block != null && (bool)block) // Default is no blocking
                    feedItems.Items.RemoveAll(item => System.Text.RegularExpressions.Regex.IsMatch(item.Title, adBlockRegExp));

                //Format item         
                foreach (FeedItem item in feedItems.Items)
                {
                    // trim (=remove \s or \n or zenkaku space from start and end) from title string
                    item.Title = item.Title.Trim(' ', '\u3000', '\n');
                    item.Title = WebUtility.HtmlDecode(item.Title);
                    item.Link = WebUtility.HtmlDecode(item.Link);
                }

                // Data binding
                FeedListResult.ItemsSource = feedItems.Items;

                if (feedItems.Items.Count > 0)
                    FeedListResult.Visibility = Visibility.Visible;
                else
                    NoItemLabel.Visibility = Visibility.Visible;

                ProgressIndicator.IsActive = false;
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ Unread").ShowAsync();
                NoItemLabel.Visibility = Visibility.Visible;
                ProgressIndicator.IsActive = false;
            }
        }

        /// <summary>
        /// Touch
        /// from http://subtech.g.hatena.ne.jp/mala/20090109/1231495352
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        /// <param name="timestamp">timestamp</param>
        private async void Touch(string subscribe_id, long timestamp)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "subscribe_id", subscribe_id }, { "timestamp", timestamp.ToString() }, { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/touch"), new HttpFormUrlEncodedContent(postData));
                ProgressIndicator.IsActive = false;
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ Touch").ShowAsync();
            }
        }

        /// <summary>
        /// TouchAll
        /// from http://subtech.g.hatena.ne.jp/mala/20090109/1231495352
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        /// <param name="timestamp">timestamp</param>
        private async void TouchAll(string subscribe_id)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "subscribe_id", subscribe_id }, { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/touch"), new HttpFormUrlEncodedContent(postData));
                ProgressIndicator.IsActive = false;
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ TouchAll").ShowAsync();
            }
        }

        /// <summary>
        /// PinAdd
        /// from http://d.hatena.ne.jp/zuzu_sion/20091011/1255337739
        /// </summary>
        /// <param name="link">link</param>
        /// <param name="title">title</param>
        private async void PinAdd(string link, string title)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "link", link }, { "title", title }, { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/pin/add"), new HttpFormUrlEncodedContent(postData));
                ProgressIndicator.IsActive = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error @ PinAdd: " + ex.ToString());
            }
        }

        /// <summary>
        /// PinRemove
        /// from http://d.hatena.ne.jp/zuzu_sion/20091011/1255337739
        /// </summary>
        /// <param name="link">link</param>
        private async void PinRemove(string link)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "link", link }, { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/pin/remove"), new HttpFormUrlEncodedContent(postData));
                ProgressIndicator.IsActive = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error @ PinRemove: " + ex.ToString());
            }
        }

        // Open item in browser
        private async void ShowInBrowser(object sender)
        {
            FeedItem item = (FeedItem)((sender as Grid).DataContext); // At right-click, the item is not selected in ListView
            if (item != null)
            {
                bool success = await Launcher.LaunchUriAsync(new Uri(item.Link));
            }
        }

        private async void ShowEntry(object sender)
        {
            FeedItem item = (FeedItem)((sender as ListView).SelectedItem);
            if (item != null)
            {
                if (string.IsNullOrEmpty(item.Body) == false)
                {
                    if(SubFrame.Visibility == Visibility.Visible)
                        SubFrame.Navigate(typeof(EntryPage), item);
                    else
                        Frame.Navigate(typeof(EntryPage), item);
                }
                else // if body is null/empty, open browser directly
                {
                    bool success = await Launcher.LaunchUriAsync(new Uri(item.Link));
                }
            }
        }


        private void FeedListResult_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!pressedPin)
            {
                ShowEntry(sender);
                sender = null;
            }
            pressedPin = false;
        }

        // At right-click, the item is not selected in ListView
        private void FeedItemTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (!pressedPin)
            {
                ShowInBrowser(sender);
                sender = null;
            }
            pressedPin = false;
            e.Handled = true;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            pressedPin = true;
        }

        private void GoNextFeed()
        {
            ProgressIndicator.IsActive = true;

            // Add pin
            foreach (FeedItem temp in feedItems.Items)
            {
                if (temp.isPinned)
                    PinAdd(temp.Link, temp.Title);
            }

            // Mark this feed as read
            int group = (Application.Current as Application).GroupIndex;
            int item = (Application.Current as Application).ItemIndex;
            (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].UnreadCount = null;
            (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].isRead = true;
            Touch(subscribeID, timeStamp);

            if (item > (Application.Current as Application).SubscriptionList[group].Count - 2)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
                else
                    NextButton.IsEnabled = false;
            }
            else
            {
                FeedListResult.Visibility = Visibility.Collapsed;

                // Go next feed
                SubscriptionItem next = (Application.Current as Application).SubscriptionList[group][item + 1];
                PageTitle.Text = next.Title;
                subscribeID = next.SubscribeID;
                Unread(subscribeID);
                (Application.Current as Application).ItemIndex = item + 1;

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }

        private void GoPreviousFeed()
        {
            ProgressIndicator.IsActive = true;

            // Add pin
            foreach (FeedItem temp in feedItems.Items)
            {
                if (temp.isPinned)
                    PinAdd(temp.Link, temp.Title);
            }

            // Mark this feed as read
            int group = (Application.Current as Application).GroupIndex;
            int item = (Application.Current as Application).ItemIndex;
            (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].UnreadCount = null;
            (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].isRead = true;
            Touch(subscribeID, timeStamp);

            if (item == 0)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
                else
                    PrevButton.IsEnabled = false;
            }
            else
            {
                FeedListResult.Visibility = Visibility.Collapsed;

                // Go previous feed
                SubscriptionItem prev = (Application.Current as Application).SubscriptionList[group][item - 1];
                PageTitle.Text = prev.Title;
                subscribeID = prev.SubscribeID;
                Unread(subscribeID);
                (Application.Current as Application).ItemIndex = item - 1;

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            NoItemLabel.Visibility = Visibility.Collapsed;
            GoPreviousFeed();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NoItemLabel.Visibility = Visibility.Collapsed;
            GoNextFeed();
        }

        private void FeedPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Show SubFrame
            if (SubFrame.Visibility == Visibility.Visible)
            {
                SubFrame.Navigate(typeof(EntryPage));
            }
        }
    }
}
