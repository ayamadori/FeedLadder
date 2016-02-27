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
using Windows.Web.Http.Filters;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string domainURL = "http://reader.livedwango.com";
        private string apiKey;
        private List<PinItem> pinItems;
        private string userName;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Show SubFrame
            if (SubFrame.Visibility == Visibility.Visible)
            {
                SubFrame.Navigate(typeof(FeedPage));
            }

            RefreshButton.IsEnabled = false;
            if (SubscriptionListResult.Visibility == Visibility.Collapsed)
                NoItemLabel.Visibility = Visibility.Visible;
            if (PinList.Visibility == Visibility.Collapsed)
                PinNoItemLabel.Visibility = Visibility.Visible;

            // Get login token
            var localSettings = ApplicationData.Current.LocalSettings;
            apiKey = localSettings.Values["ApiKeyString"] as string;
            if (apiKey == null)
                Login();
            else
            {
                // Set token to cookie
                HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(new Uri(domainURL));
                // If reader_sid==ApiKey, already logged-in
                foreach (var cookie in cookies)
                {
                    if (cookie.Name.StartsWith("reader_sid"))
                        if (cookie.Value.StartsWith(apiKey))
                        {
                            RefreshButton.IsEnabled = true;
                            return;
                        }
                }
                HttpCookie readerSID = new HttpCookie("reader_sid", domainURL.Replace("http://", ""), "/") { Value = apiKey };
                filter.CookieManager.SetCookie(readerSID);

                try
                {
                    // Check if can login to LDR
                    HttpClient httpClient = new HttpClient(filter);
                    string responseString = await httpClient.GetStringAsync(new Uri(domainURL + "/reader/"));
                    // If can't login,, show login window to user
                    if (responseString.Contains("ApiKey"))
                    {
                        apiKey = responseString.Substring(responseString.IndexOf("ApiKey") + 10, 32);
                        localSettings.Values["ApiKeyString"] = apiKey;

                        int _start = responseString.IndexOf("<a href=\"/user/") + 15;
                        int _end = responseString.IndexOf("\"", _start);
                        userName = responseString.Substring(_start, _end - _start);

                        RefreshButton.IsEnabled = true;
                    }
                    else
                        Login();
                }
                catch (Exception)
                {
                    await new MessageDialog("This app could not send/receive data.", "Error @ Login").ShowAsync();
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (RootPivot.SelectedIndex == 1) // pinned
            {
                return;
            }

            int group = (Windows.UI.Xaml.Application.Current as Application).GroupIndex;
            int item = (Windows.UI.Xaml.Application.Current as Application).ItemIndex;
            if (group > -1)
            {
                if ((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item].UnreadCount == null)
                {
                    for (int i = item + 1; i < (Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group].Count; i++)
                    {
                        if ((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][i].UnreadCount != null)
                        {
                            SubscriptionListView.ScrollIntoView((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][i]);
                            return;
                        }
                    }
                    if (group > (Windows.UI.Xaml.Application.Current as Application).SubscriptionList.Count - 2)
                    {
                        SubscriptionListView.ScrollIntoView((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item]);
                    }
                    else
                    {
                        SubscriptionListView.ScrollIntoView((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group + 1][0]);
                    }
                }
                else
                {
                    SubscriptionListView.ScrollIntoView((Windows.UI.Xaml.Application.Current as Application).SubscriptionList[group][item]);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Subs(1);
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingPage), userName);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private async void Login()
        {
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            HttpClient httpClient = new HttpClient(filter);
            HttpResponseMessage response;

            try
            {
                // http://reader.livedwango.com/
                filter.AllowAutoRedirect = false;
                string loginURL = domainURL + "/login_openid?openid_url=http://www.livedoor.com/";
                response = await httpClient.GetAsync(new Uri(loginURL));
                string startURL = response.Headers["Location"];
                Uri startURI = new Uri(startURL);
                string oicTime = startURL.Substring(startURL.IndexOf("oic.time") + 11, 31);
                // Verify endURL by using oic.time(=signature)
                Uri endURI = new Uri(domainURL + "/login_openid?openid-check=1&oic.time=" + oicTime + "&openid.mode=id_res");
                // Show authentication window
                var webAuthenticationResult =
                    await Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(
                    Windows.Security.Authentication.Web.WebAuthenticationOptions.None,
                    startURI,
                    endURI);

                switch (webAuthenticationResult.ResponseStatus)
                {
                    case Windows.Security.Authentication.Web.WebAuthenticationStatus.Success:
                        // Successful authentication. 
                        filter.AllowAutoRedirect = true;
                        string responseString = await httpClient.GetStringAsync(new Uri(webAuthenticationResult.ResponseData));
                        apiKey = responseString.Substring(responseString.IndexOf("ApiKey") + 10, 32);
                        var localSettings = ApplicationData.Current.LocalSettings;
                        localSettings.Values["ApiKeyString"] = apiKey;

                        int _start = responseString.IndexOf("<a href=\"/user/") + 15;
                        int _end = responseString.IndexOf("\"", _start);
                        userName = responseString.Substring(_start, _end - _start);

                        RefreshButton.IsEnabled = true;
                        break;
                    case Windows.Security.Authentication.Web.WebAuthenticationStatus.ErrorHttp:
                        // HTTP error.
                        await new MessageDialog("HTTP error").ShowAsync();
                        break;
                    default:
                        // Other error.
                        await new MessageDialog("Other error").ShowAsync();
                        break;
                }
            }
            catch (Exception)
            {
                // Authentication failed. Handle parameter, SSL/TLS, and Network Unavailable errors here.
                await new MessageDialog("Authentication failed").ShowAsync();
            }
        }

        /// <summary>
        /// unread = 1:unread only, 0:all
        /// </summary>
        /// <param name="unread"></param>
        private async void Subs(int unread)
        {
            SubscriptionListResult.Visibility = Visibility.Collapsed;
            NoItemLabel.Visibility = Visibility.Collapsed;
            ProgressIndicator.IsActive = true;

            try
            {
                // (Re-)Set token to cookie
                HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                HttpCookie readerSid = new HttpCookie("reader_sid", domainURL.Replace("http://", ""), "/") { Value = apiKey };
                filter.CookieManager.SetCookie(readerSid);

                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/subs?unread=" + unread + "&from_id=0&limit=100"), new HttpFormUrlEncodedContent(postData));

                string res = await response.Content.ReadAsStringAsync();
                // refer to http://blogs.gine.jp/taka/archives/2106
                var subItems = new List<SubscriptionItem>();
                //subItems = new List<SubscriptionItem>();
                var serializer = new DataContractJsonSerializer(typeof(List<SubscriptionItem>));
                byte[] bytes = Encoding.UTF8.GetBytes(res);
                MemoryStream ms = new MemoryStream(bytes);
                subItems = serializer.ReadObject(ms) as List<SubscriptionItem>;
                res = "";
                //Format item              
                foreach (SubscriptionItem item in subItems)
                {
                    item.Title = item.Title.Trim(' ', '　', '\n');
                    item.Title = WebUtility.HtmlDecode(item.Title);
                    switch (item.Rate)
                    {
                        case "5":
                            item.Rate = "★★★★★";
                            break;
                        case "4":
                            item.Rate = "★★★★☆";
                            break;
                        case "3":
                            item.Rate = "★★★☆☆";
                            break;
                        case "2":
                            item.Rate = "★★☆☆☆";
                            break;
                        case "1":
                            item.Rate = "★☆☆☆☆";
                            break;
                        case "0":
                            item.Rate = "☆☆☆☆☆";
                            break;
                    }
                }
                // Data binding
                //(Application.Current as App).SubscriptionList = subItems;
                //this.subscriptionListResult.ItemsSource = (Application.Current as App).SubscriptionList;

                // default
                List<Group<SubscriptionItem>> source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Folder; }, false);

                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                // Read data from a simple setting
                string sortMode = roamingSettings.Values["SortModeString"] as string;
                if (sortMode == null) sortMode = "Folder"; // Default is folder
                switch (sortMode)
                {
                    case "Folder":
                        source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Folder; }, false);
                        break;
                    case "Rating":
                        source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Rate; }, true);
                        break;
                }
                (Windows.UI.Xaml.Application.Current as Application).SubscriptionList = source;
                SubscriptionList.Source = (Windows.UI.Xaml.Application.Current as Application).SubscriptionList;

                ProgressIndicator.IsActive = false;

                if (subItems.Count > 0)
                {
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    SubscriptionListResult.Visibility = Visibility.Visible;
                }
                else
                {
                    NoItemLabel.Visibility = Visibility.Visible;
                }
                RefreshButton.IsEnabled = true;
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ Subs").ShowAsync();
            }
        }


        private void SubscriptionListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SubscriptionItem item = (SubscriptionItem)((sender as ListView).SelectedItem);
            int itemIndex;
            int groupIndex;
            foreach (Group<SubscriptionItem> group in (Application.Current as Application).SubscriptionList)
            {
                if ((itemIndex = group.FindIndex(temp => temp.Equals(item))) > -1)
                {
                    groupIndex = (Windows.UI.Xaml.Application.Current as Application).SubscriptionList.FindIndex(temp => temp.Equals(group));                    
                    (sender as ListView).SelectedItem = null;
                    (Windows.UI.Xaml.Application.Current as Application).GroupIndex = groupIndex;
                    (Windows.UI.Xaml.Application.Current as Application).ItemIndex = itemIndex;
                    if (SubFrame.Visibility == Visibility.Visible)
                    {
                        SubFrame.Navigate(typeof(FeedPage), apiKey);

                        // Delete backstack in SubFrame
                        // http://stackoverflow.com/questions/16243547/how-to-delete-page-from-navigation-stack-c-sharp-windows-8
                        SubFrame.BackStack.RemoveAt(SubFrame.BackStack.Count - 1);
                    }
                    else
                        Frame.Navigate(typeof(FeedPage), apiKey);
                }
            }
        }

        // from http://www.cocoloware.com/?p=32
        private void RootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RootPivot.SelectedIndex == 0) //unreed
            {
                ApplicationBar.Visibility = Visibility.Visible;
            }
            else if (RootPivot.SelectedIndex == 1) //pinned
            {
                ApplicationBar.Visibility = Visibility.Collapsed;
                PinAll();
            }
        }


        private async void PinList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PinItem item = (PinItem)((sender as ListView).SelectedItem);
            if (item != null)
            {
                // remove item
                PinRemove(item.Link);
                // open link
                bool success = await Launcher.LaunchUriAsync(new Uri(item.Link));
            }
        }

        /// <summary>
        /// PinAll
        /// </summary>
        private async void PinAll()
        {
            PinList.Visibility = Visibility.Collapsed;
            PinNoItemLabel.Visibility = Visibility.Collapsed;
            PinProgressIndicator.IsActive = true;

            try
            {
                HttpClient httpClient = new HttpClient();
                Dictionary<string, string> postData = new Dictionary<string, string>() { { "ApiKey", apiKey } };
                HttpResponseMessage response = await httpClient.PostAsync(new Uri(domainURL + "/api/pin/all"), new HttpFormUrlEncodedContent(postData));

                string res = await response.Content.ReadAsStringAsync();
                // refer to http://blogs.gine.jp/taka/archives/2106
                pinItems = new List<PinItem>();
                var serializer = new DataContractJsonSerializer(typeof(List<PinItem>));
                byte[] bytes = Encoding.UTF8.GetBytes(res);
                MemoryStream ms = new MemoryStream(bytes);
                pinItems = serializer.ReadObject(ms) as List<PinItem>;
                res = "";
                //Format item             
                foreach (PinItem item in pinItems)
                {
                    item.Title = item.Title.Trim(' ', '　', '\n');
                    item.Title = WebUtility.HtmlDecode(item.Title);
                    item.Link = WebUtility.HtmlDecode(item.Link);
                }

                // Data binding
                PinList.ItemsSource = pinItems;

                PinProgressIndicator.IsActive = false;

                if (pinItems.Count > 0)
                {
                    PinNoItemLabel.Visibility = Visibility.Collapsed;
                    PinList.Visibility = Visibility.Visible;
                }
                else
                {
                    PinNoItemLabel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data." ,"Error @ PinAll").ShowAsync();
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error @ PinRemove: " + ex.ToString());
            }
        }
    }
}
