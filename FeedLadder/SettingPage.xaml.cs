using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private const string logoutURL = "http://reader.livedwango.com/reader/logout";

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
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.GetAsync(new Uri(logoutURL));
                if (response.IsSuccessStatusCode)
                {
                    await new MessageDialog("Prease restart app", "Logout").ShowAsync();
                    var roamingSettings = ApplicationData.Current.RoamingSettings;
                    // Delete a simple setting
                    roamingSettings.Values.Remove("ApiKeyString");
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.ToString(), "Error @ Logout").ShowAsync();
            }
        }
    }
}
