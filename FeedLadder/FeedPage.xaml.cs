using System;
using System.Net;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Runtime.Serialization.Json;
using Microsoft.Phone.Tasks;
using System.Text;
using System.IO;

namespace FeedLadder
{
    public partial class FeedPage : PhoneApplicationPage
    {
        private const string domain = "http://reader.livedoor.com";
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
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {
                //NavigationContext.QueryString.TryGetValue("subscribe_id", out subscribeID);
                //NavigationContext.QueryString.TryGetValue("title", out title);
                //NavigationContext.QueryString.TryGetValue("ApiKey", out apiKey);

                NavigationContext.QueryString.TryGetValue("ApiKey", out apiKey);
                //PageTitle.Text = title;
                int group = (App.Current as App).GroupIndex;
                int item = (App.Current as App).ItemIndex;
                title = (App.Current as App).SubscriptionList[group][item].Title;
                PageTitle.Text = title;
                subscribeID = (App.Current as App).SubscriptionList[group][item].SubscribeID;
                Unread(subscribeID);

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                //JST = UTC + 9hour
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }

        void ContentPanel_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            double deltaX = e.TotalManipulation.Translation.X;
            double deltaY = e.TotalManipulation.Translation.Y;
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (deltaX > delta) // flick right
                {
                    //MessageBox.Show("Flick to Right: " + deltaX);
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    goPreviousFeed();
                }
                else if(deltaX < delta*(-1))// flick left
                {
                    //MessageBox.Show("Flick to Left: " + deltaX);
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    goNextFeed();
                }
            }
        }

        /// <summary>
        /// Unread
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        private void Unread(string subscribe_id)
        {
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_UnreadCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postdata = "subscribe_id=" + subscribe_id + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/unread"), "POST", postdata);
        }

        void cli_UnreadCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            //(Application.Current as App).CWClient.UploadStringCompleted -= (UploadStringCompletedEventHandler)cli_UnreadCompleted;

            ////////////////////////////////////////////////////////////////////////////////////
            // ② 読み込みデータのを、オブジェクトに変換しUIに渡す処理
            ////////////////////////////////////////////////////////////////////////////////////
            //progressBar1.Visibility = Visibility.Collapsed;
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                MessageBox.Show("Please check network status", "Network Error @ Unread", MessageBoxButton.OK);
            }
            else
            {
                feedListResult.Visibility = Visibility.Visible;
                string res = e.Result;

                // refer to http://blogs.gine.jp/taka/archives/2106
                var serializer = new DataContractJsonSerializer(typeof(FeedItems));
                byte[] bytes = Encoding.UTF8.GetBytes(res);
                MemoryStream ms = new MemoryStream(bytes);
                feedItems = serializer.ReadObject(ms) as FeedItems;
                res = "";

                // AD feed block (cannnot use "\s" ?)
                // from http://www.atmarkit.co.jp/fdotnet/dotnettips/815listremove/listremove.html
                feedItems.Items.RemoveAll(item => System.Text.RegularExpressions.Regex.IsMatch(item.Title, adBlockRegExp));

                //Format item         
                foreach (FeedItem item in feedItems.Items)
                {
                    // trim (=remove \s or \n or zenkaku space from start and end) from title string
                    item.Title = item.Title.Trim(' ', '\u3000', '\n');
                    item.Title = HttpUtility.HtmlDecode(item.Title);
                    item.Link = HttpUtility.HtmlDecode(item.Link);
                }
                this.feedListResult.ItemsSource = feedItems.Items;

                if (feedItems.Items.Count == 0)
                {
                    feedListResult.Visibility = Visibility.Collapsed;
                    NoItemLabel.Visibility = Visibility.Visible;
                }

                //// Set as read
                //Touch(subscribeID, timeStamp);
            }
        }

        /// <summary>
        /// Touch
        /// from http://subtech.g.hatena.ne.jp/mala/20090109/1231495352
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        /// <param name="timestamp">timestamp</param>
        private void Touch(string subscribe_id, long timestamp)
        {
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_TouchCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postdata = "subscribe_id=" + subscribe_id + "&timestamp=" + timestamp + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/touch"), "POST", postdata);
        }

        /// <summary>
        /// TouchAll
        /// from http://subtech.g.hatena.ne.jp/mala/20090109/1231495352
        /// </summary>
        /// <param name="subscribe_id">subscribe_id</param>
        /// <param name="timestamp">timestamp</param>
        private void TouchAll(string subscribe_id)
        {
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_TouchCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postdata = "subscribe_id=" + subscribe_id + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/touch"), "POST", postdata);
        }

        void cli_TouchCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                MessageBox.Show("Please check network status", "Network Error @ Touch", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// PinAdd
        /// from http://d.hatena.ne.jp/zuzu_sion/20091011/1255337739
        /// </summary>
        /// <param name="link">link</param>
        /// <param name="title">title</param>
        private void PinAdd(string link, string title)
        {
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_PinAddCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postdata = "link=" + HttpUtility.UrlEncode(link) + "&title=" + HttpUtility.UrlEncode(title) + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/pin/add"), "POST", postdata);
        }

        void cli_PinAddCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                //MessageBox.Show("Please check network status", "Network Error @ PinAdd", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// PinRemove
        /// from http://d.hatena.ne.jp/zuzu_sion/20091011/1255337739
        /// </summary>
        /// <param name="link">link</param>
        private void PinRemove(string link)
        {
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_PinRemoveCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postdata = "link=" + HttpUtility.UrlEncode(link) + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/pin/remove"), "POST", postdata);
        }

        void cli_PinRemoveCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                //MessageBox.Show("Please check network status", "Network Error @ PinRemove", MessageBoxButton.OK);
            }
        }

        // Open item in browser
        private void ShowInBrowser(object sender, System.Windows.Input.GestureEventArgs e)
        {
            FeedItem item = (FeedItem)((sender as LongListSelector).SelectedItem);
            if (item != null)
            {
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = new Uri(item.Link);
                task.Show();
            }
        }

        private void showEntry(object sender, System.Windows.Input.GestureEventArgs e)
        {
            FeedItem item = (FeedItem)((sender as LongListSelector).SelectedItem);
            if (item != null)
            {
                if (String.IsNullOrEmpty(item.Body) == false)
                {
                    String navigateUri = "/EntryPage.xaml?title=" + HttpUtility.UrlEncode(item.Title) + "&link=" + HttpUtility.UrlEncode(item.Link) + "&body=" + HttpUtility.UrlEncode(item.Body);
                    Console.WriteLine("URI length = " + navigateUri.Length);
                    NavigationService.Navigate(new Uri(navigateUri, UriKind.Relative));
                    //NavigationService.Navigate(new Uri("/EntryPage.xaml?title=" + HttpUtility.UrlEncode(item.Title) + "&link=" + HttpUtility.UrlEncode(item.Link) + "&body=" + HttpUtility.UrlEncode(item.Body), UriKind.Relative));
                    //NavigationService.Navigate(new Uri("/EntryPage.xaml?title=" + item.Title + "&link=" + item.Link + "&body=" + item.Body, UriKind.Relative));
                    //NavigationService.Navigate(new Uri("/EntryPage.xaml?title=" + item.Title + "&link=" + item.Link + "&body=" + HttpUtility.UrlEncode(item.Body), UriKind.Relative));
                }
                else // if body is null/empty, open browser directly
                {
                    WebBrowserTask task = new WebBrowserTask();
                    task.Uri = new Uri(item.Link);
                    task.Show();
                }
            }
        }

        private void feedListResult_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!pressedPin)
            {
                showEntry(sender, e);
            }
            pressedPin = false;
        }

        private void feedListResult_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!pressedPin)
            {
                ShowInBrowser(sender, e);
            }
            pressedPin = false;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            pressedPin = true;
        }

        private void goNextFeed()
        {
            progressIndicator.IsVisible = true;

            // Add pin
            foreach (FeedItem temp in feedItems.Items)
            {
                if (temp.isPinned)
                    PinAdd(temp.Link, temp.Title);
            }

            // Mark this feed as read
            int group = (App.Current as App).GroupIndex;
            int item = (App.Current as App).ItemIndex;
            //(App.Current as App).SubscriptionList[group][item].UnreadCount = null;
            (App.Current as App).SubscriptionList[group][item].isRead = true;
            Touch(subscribeID, timeStamp);

            //if(itemIndex > (App.Current as App).SubscriptionList.Count - 2)
            if (item > (App.Current as App).SubscriptionList[group].Count - 2)
            {
                NavigationService.GoBack();
            }
            //else if (itemIndex > -1)
            else
            {
                feedListResult.Visibility = Visibility.Collapsed;

                // Go next feed
                //SubscriptionItem next = (App.Current as App).SubscriptionList[itemIndex + 1];
                SubscriptionItem next = (App.Current as App).SubscriptionList[group][item + 1];
                PageTitle.Text = next.Title;
                subscribeID = next.SubscribeID;
                Unread(subscribeID);
                (App.Current as App).ItemIndex = item + 1;

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }

        private void goPreviousFeed()
        {
            progressIndicator.IsVisible = true;

            // Add pin
            foreach (FeedItem temp in feedItems.Items)
            {
                if (temp.isPinned)
                    PinAdd(temp.Link, temp.Title);
            }

            // Mark this feed as read
            int group = (App.Current as App).GroupIndex;
            int item = (App.Current as App).ItemIndex;
            (App.Current as App).SubscriptionList[group][item].UnreadCount = null;
            Touch(subscribeID, timeStamp);

            if (item == 0)
            {
                NavigationService.GoBack();
            }
            else
            {
                feedListResult.Visibility = Visibility.Collapsed;

                // Go previous feed
                SubscriptionItem prev = (App.Current as App).SubscriptionList[group][item - 1];
                PageTitle.Text = prev.Title;
                subscribeID = prev.SubscribeID;
                Unread(subscribeID);
                (App.Current as App).ItemIndex = item - 1;

                // Update timestamp
                DateTime utcNow = DateTime.UtcNow;
                timeStamp = UnixEpochTime.GetUnixTime(utcNow) + (60 * 60 * 9);
            }
        }
    }
}