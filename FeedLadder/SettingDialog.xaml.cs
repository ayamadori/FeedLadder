using System;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FeedLadder
{
    public sealed partial class SettingDialog : ContentDialog
    {
        private const string domainURL = "http://reader.livedwango.com";
        private const string logoutURL = "http://member.livedoor.com/login/logout";

        public SettingDialog(string parameter)
        {
            this.InitializeComponent();

            var roamingSettings = ApplicationData.Current.RoamingSettings;
            // Read data from a simple setting
            string sortMode = roamingSettings.Values["SortModeString"] as string;
            if (sortMode == null)
            {
                // Default is Folder
                sortMode = "Folder";
                roamingSettings.Values["SortModeString"] = sortMode;
            }
            SortModeComboBox.SelectedIndex = (sortMode == "Folder")? 0 : 1;
            object adBlockEnable = roamingSettings.Values["AdBlockEnableBool"];
            if (adBlockEnable == null)
                AdBlockSwitch.IsOn = false; // Default is no blocking
            else
                AdBlockSwitch.IsOn = (bool)adBlockEnable;

            UsernameTextBox.Text = (parameter == null)? "(No login)" : parameter;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void AdBlockSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["AdBlockEnableBool"] = AdBlockSwitch.IsOn;
        }

        private void SortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["SortModeString"] = (SortModeComboBox.SelectedItem as ComboBoxItem).Content as string;
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

                    // Reset cookies in livedoor.com and livedwango.com
                    HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                    var cookies = filter.CookieManager.GetCookies(new Uri(domainURL));
                    // Delete cookies
                    foreach (var cookie in cookies)
                    {
                        filter.CookieManager.DeleteCookie(cookie);
                    }
                    cookies = filter.CookieManager.GetCookies(new Uri("http://www.livedoor.com/"));
                    // Delete cookies
                    foreach (var cookie in cookies)
                    {
                        filter.CookieManager.DeleteCookie(cookie);
                    }

                    await new MessageDialog("Please restart app.", "Successfully Log Out").ShowAsync();
                }
            }
            catch (Exception)
            {
                await new MessageDialog("This app could not send/receive data.", "Error @ Log Out").ShowAsync();
            }
        }
    }
}
