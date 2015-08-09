using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Text;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;

namespace FeedLadder
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const string domain = "http://reader.livedoor.com";
        private const string auth_url = "https://member.livedoor.com/login/";
        private const string logout_url = "http://www.livedoor.com/r/user_logout";
        private string livedoor_id;
        private string password;
        private string apiKey;
        private ProtectedSettingDictionary settings; //encripted setting
        private int loginParam;
        private List<PinItem> pinItems;
        //private string link;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();
            settings = new ProtectedSettingDictionary("ProtectedStore");
        }

        //// Load ApiKey
        //protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);

        //    if (e.IsNavigationInitiator)
        //    {
        //        if (this.settings.ContainsKey("ApiKey"))
        //        {
        //            apiKey = this.settings["ApiKey"];
        //            this.settings["ApiKey"] = "";
        //            this.settings.Save();
        //        }
        //    }
        //}

        // 画面がロードされてからの処理：RSSデータを非同期で読み込む
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.settings.ContainsKey("UserName"))
            {
                livedoor_id = this.settings["UserName"];
            }
            if (this.settings.ContainsKey("Password"))
            {
                password = this.settings["Password"];
            }

            if (livedoor_id == null || password == null)
            {
                LoginLabel.Visibility = Visibility.Visible;
                PinLoginLabel.Visibility = Visibility.Visible;
                //Get from Button collection
                //from http://ayano.hateblo.jp/entry/20110822/p1
                ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = false;
            }
            else if (subscriptionListResult.Visibility == Visibility.Collapsed)
            {
                NoItemLabel.Visibility = Visibility.Visible;
                if (PinList.Visibility == Visibility.Collapsed)
                {
                    PinNoItemLabel.Visibility = Visibility.Visible;
                }
            }
        }

        //private void LoadData()
        //{
        //    //////////////////////////////////////////////////////////////////////////////////////
        //    //// ① データ読み込み処理
        //    //////////////////////////////////////////////////////////////////////////////////////
        //    string URL = "http://www3.nhk.or.jp/rss/news/cat0.xml";
        //    WebClient cli = new WebClient();
        //    cli.DownloadStringCompleted += new DownloadStringCompletedEventHandler(cli_DownloadStringCompleted);
        //    cli.DownloadStringAsync(new Uri(URL));
        //}

        private void Login()
        {
            progressIndicator.IsVisible = true;

            // Create new webclient for concurrent network access
            // from http://stackoverflow.com/questions/7084948/c-sharp-concurrent-i-o-operations-exception
            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_LoginCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string postData = "livedoor_id=" + livedoor_id + "&password=" + password + "&.next=" + domain + "/lite/&.sv=reader";
            cli.UploadStringAsync(new Uri(auth_url), "POST", postData);
        }

        void cli_LoginCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            ////////////////////////////////////////////////////////////////////////////////////
            // ② 読み込みデータのを、オブジェクトに変換しUIに渡す処理
            ////////////////////////////////////////////////////////////////////////////////////
            progressIndicator.IsVisible = false;
            //listResult.Visibility = Visibility.Visible;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                MessageBox.Show("Please check network status", "Network Error @ Login", MessageBoxButton.OK);
                ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = true;
            }
            else
            {
                //Get "reader_sid"="ApiKey" from Cookie
                string cookie = (sender as CustomWebClient).ResponseHeaders["Set-Cookie"];
                if(cookie == null)
                {
                    MessageBox.Show("Please check Settings", "Login Failed", MessageBoxButton.OK);
                    ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = true;
                    return;
                }
                int start = cookie.IndexOf("reader_sid") + 11;
                if (start > 10)
                {
                    int end = cookie.IndexOf(";", start);
                    apiKey = cookie.Substring(start, end - start);
                }
                else
                {
                    MessageBox.Show("Please check Settings", "Login Failed", MessageBoxButton.OK);
                    ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = true;
                    return;
                }
               

                ////Get "reader_sid"="ApiKey" from Response Body
                //string res = e.Result;
                //int start = res.IndexOf("ApiKey") + 10;
                //if (start < 10)
                //{
                //    MessageBox.Show("Please check Username and Password", "Login Failed", MessageBoxButton.OK);
                //    ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = true;
                //    return;
                //}
                //int end = res.IndexOf("\"", start);
                //apiKey = res.Substring(start, end - start);
                LoginLabel.Visibility = Visibility.Collapsed;

                //Only "reader_sid" key is not set in Cookie automatically, so set manually :(
                (Application.Current as App).GlobalCookieContainer = (sender as CustomWebClient).CookieContainer;
                (Application.Current as App).GlobalCookieContainer.SetCookies(new Uri("http://reader.livedoor.com"), "reader_sid=" + apiKey);

                if (loginParam == 0)
                {
                    // check unread
                    Subs(1);
                }
                else if (loginParam == 1)
                {
                    // get Pin item
                    PinAll();
                }
            }
        }

        /// <summary>
        /// unread = 1:unread only, 0:all
        /// </summary>
        /// <param name="unread"></param>
        private void Subs(int unread)
        {
            subscriptionListResult.Visibility = Visibility.Collapsed;
            NoItemLabel.Visibility = Visibility.Collapsed;
            progressIndicator.IsVisible = true;

            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_SubsCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            // Set cookie manually
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postData = "unread=" + unread + "&ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/subs"), "POST", postData);
            //string temp = cli.CookieContainer.GetCookieHeader(new Uri("http://reader.livedoor.com"));
        }

        private void cli_SubsCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                MessageBox.Show("Please check network status", "Network Error @ Subs", MessageBoxButton.OK);
            }
            else
            {
                string res = e.Result;
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
                    item.Title = HttpUtility.HtmlDecode(item.Title);
                    switch(item.Rate)
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

                IsolatedStorageSettings sort = IsolatedStorageSettings.ApplicationSettings;
                // default
                List<Group<SubscriptionItem>> source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Folder; }, false);
                string sortMode = "";
                if (sort.TryGetValue<string>("Sort", out sortMode))
                {
                    switch (sortMode)
                    {
                        case "Folder":
                            source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Folder; }, false);
                            break;
                        case "Rate":
                            source = Group<SubscriptionItem>.CreateGroups(subItems, item => { return item.Rate; }, true);
                            break;
                    }
                    (Application.Current as App).SubscriptionList = source;
                    this.subscriptionListResult.ItemsSource = (Application.Current as App).SubscriptionList;
                }
                

                if (subItems.Count > 0)
                {
                    NoItemLabel.Visibility = Visibility.Collapsed;
                    subscriptionListResult.Visibility = Visibility.Visible;
                }
                else
                {
                    NoItemLabel.Visibility = Visibility.Visible;
                }
                ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = true;
            }
        }

        private void abmAbout_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void abmRefresh_Click(object sender, EventArgs e)
        {
            ((ApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = false;

            if (apiKey == null)
            {
                loginParam = 0;
                Login();
            }
            else
            {
                Subs(1);
            }

        }

        private void abmSetting_Click(object sender, EventArgs e)
        {
            // reset CookieContainer
            //apiKey = null;
            //(App.Current as App).GlobalCookieContainer = new CookieContainer();

            //// Save ApiKey
            //this.settings["ApiKey"] = apiKey;
            //this.settings.Save();

            (App.Current as App).GroupIndex = -1;
            (App.Current as App).ItemIndex = -1;
            NavigationService.Navigate(new Uri("/SettingPage.xaml", UriKind.Relative));
        }

        private void subscriptionListResult_Tap(object sender, GestureEventArgs e)
        {
            SubscriptionItem item = (SubscriptionItem)((sender as LongListSelector).SelectedItem);
            int itemIndex;
            int groupIndex;
            foreach (Group<SubscriptionItem> group in (App.Current as App).SubscriptionList)
            {
                if ((itemIndex = group.FindIndex(temp => temp.Equals(item))) > -1)
                {
                    groupIndex = (App.Current as App).SubscriptionList.FindIndex(temp => temp.Equals(group));
                    NavigationService.Navigate(new Uri("/FeedPage.xaml?ApiKey=" + apiKey, UriKind.Relative));
                    (sender as LongListSelector).SelectedItem = null;
                    (App.Current as App).GroupIndex = groupIndex;
                    (App.Current as App).ItemIndex = itemIndex;
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (RootPivot.SelectedIndex == 1) // pinned
            {
                return;
            }

            int group = (App.Current as App).GroupIndex;
            int item = (App.Current as App).ItemIndex;
            if (group > -1)
            {          
                if ((App.Current as App).SubscriptionList[group][item].UnreadCount == null)
                {
                    for (int i = item + 1; i < (App.Current as App).SubscriptionList[group].Count; i++ )
                    {
                        if ((App.Current as App).SubscriptionList[group][i].UnreadCount != null)
                        {
                            subscriptionListResult.ScrollTo((App.Current as App).SubscriptionList[group][i]);
                            return;
                        }
                    }                   
                    if (group > (App.Current as App).SubscriptionList.Count - 2)
                    {
                        subscriptionListResult.ScrollTo((App.Current as App).SubscriptionList[group][item]);
                    }
                    else
                    {
                        subscriptionListResult.ScrollTo((App.Current as App).SubscriptionList[group + 1][0]);
                    }
                }
                else
                {
                    subscriptionListResult.ScrollTo((App.Current as App).SubscriptionList[group][item]);
                }
            }
        }

        // from http://www.cocoloware.com/?p=32
        private void RootPivot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RootPivot.SelectedIndex == 0) //unreed
            {
                ApplicationBar.IsVisible = true;
            }
            else if (RootPivot.SelectedIndex == 1) //pinned
            {
                ApplicationBar.IsVisible = false;
                if (livedoor_id == null || password == null) return;
                if (apiKey == null)
                {
                    loginParam = 1;
                    Login();
                }
                else
                {
                    PinAll();
                }
            }
        }

        private void PinList_Tap(object sender, GestureEventArgs e)
        {
            PinItem item = (PinItem)((sender as LongListSelector).SelectedItem);
            if (item != null)
            {
                // remove item
                //pinItems.Remove(item);
                //link = item.Link;
                //PinRemove(link);
                PinRemove(item.Link);

                // open link
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = new Uri(item.Link);
                task.Show();
            }
        }

        /// <summary>
        /// PinAll
        /// </summary>
        private void PinAll()
        {
            PinList.Visibility = Visibility.Collapsed;
            PinNoItemLabel.Visibility = Visibility.Collapsed;
            progressIndicator.IsVisible = true;

            CustomWebClient cli = new CustomWebClient();
            cli.UploadStringCompleted += new UploadStringCompletedEventHandler(cli_PinAllCompleted);
            cli.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            // Set cookie manually
            cli.CookieContainer = (Application.Current as App).GlobalCookieContainer;
            string postData = "ApiKey=" + apiKey;
            cli.UploadStringAsync(new Uri(domain + "/api/pin/all"), "POST", postData);
        }

        private void cli_PinAllCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressIndicator.IsVisible = false;

            if (e.Error != null)
            {
                //MessageBox.Show("通信エラーが発生しました。");
                MessageBox.Show("Please check network status", "Network Error @ PinAll", MessageBoxButton.OK);
            }
            else
            {
                string res = e.Result;
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
                    item.Title = HttpUtility.HtmlDecode(item.Title);
                    item.Link = HttpUtility.HtmlDecode(item.Link);
                }

                // Data binding
                PinList.ItemsSource = pinItems;
                

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

            //if (e.Error != null)
            //{
            //    MessageBox.Show("Please check network status", "Network Error @ PinRemove", MessageBoxButton.OK);
            //}
            //else
            //{
            //    // open link
            //    WebBrowserTask task = new WebBrowserTask();
            //    task.Uri = new Uri(link);
            //    task.Show();
            //}
        }
    }
}