using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private const string domainURL = "http://reader.livedwango.com";
        private const string logoutURL = "http://member.livedoor.com/login/logout";

        public SettingPage()
        {
            this.InitializeComponent();

            ObservableCollection<string> items = new ObservableCollection<string>();
            items.Add("Folder");
            items.Add("Rating");
            SortModeComboBox.DataContext = items;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                var roamingSettings = ApplicationData.Current.RoamingSettings;
                // Read data from a simple setting
                string sortMode = roamingSettings.Values["SortModeString"] as string;
                if (sortMode == null)
                {
                    // Default is Folder
                    sortMode = "Folder";
                    roamingSettings.Values["SortModeString"] = sortMode;
                }
                SortModeComboBox.SelectedItem = sortMode;
                object adBlockEnable = roamingSettings.Values["AdBlockEnableBool"];
                if (adBlockEnable == null)
                    AdBlockSwitch.IsOn = false; // Default is no blocking
                else
                    AdBlockSwitch.IsOn = (bool)adBlockEnable;

                string userName = e.Parameter as string;
                if (userName == null) userName = "(No login)";
                UsernameTextBox.Text = userName;
            }
        }

        private void AdBlockSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["AdBlockEnableBool"] = AdBlockSwitch.IsOn;
        }

        private void SortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["SortModeString"] = SortModeComboBox.SelectedItem as string;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Referer = new Uri(domainURL + "/reader/");
                HttpResponseMessage response1 = await httpClient.GetAsync(new Uri(domainURL + "/reader/logout"));
                httpClient.DefaultRequestHeaders.Referer = new Uri("http://www.livedoor.com/");
                HttpResponseMessage response2 = await httpClient.GetAsync(new Uri(logoutURL));
                if (response1.IsSuccessStatusCode && response2.IsSuccessStatusCode)
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    // Delete a simple setting
                    localSettings.Values.Remove("ApiKeyString");
                    UsernameTextBox.Text = "(No login)";
                    await new MessageDialog("Please restart app.", "Logout").ShowAsync();
                }
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ Logout").ShowAsync();
            }
        }
    }
}
