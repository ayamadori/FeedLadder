using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SharePage : Page
    {
        public SharePage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // refer to https://msdn.microsoft.com/en-us/library/windows/apps/mt243292.aspx
            ShareOperation shareOperation = (ShareOperation)e.Parameter;
            string url = "";

            if (shareOperation.Data.Contains(StandardDataFormats.WebLink)) // URI
            {
                Uri uri = await shareOperation.Data.GetWebLinkAsync();
                url = uri.AbsoluteUri;

                Debug.WriteLine("Uri: " + url);
            }

            // Get OS Version
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/2d8a7dab-1bad-4405-b70d-768e4cb2af96/uwp-get-os-version-in-an-uwp-app
            string deviceFamilyVersion = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong version = ulong.Parse(deviceFamilyVersion);
            ulong major = (version & 0xFFFF000000000000L) >> 48;
            ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            ulong revision = (version & 0x000000000000FFFFL);
            var osVersion = $"{major}.{minor}.{build}.{revision}";
            Debug.WriteLine("OS Version: " + osVersion);

            if (build < 15063) // Older build than Creators Update(=15063)
            {
                // Delay before opening browser (I can't understand necessity...)
                // http://blog.okazuki.jp/entry/20120302/1330643881
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
            }

            Subscribe(url);

            // Exit app
            Windows.UI.Xaml.Application.Current.Exit();
        }

        private async void Subscribe(string url)
        {
            // refer to https://msdn.microsoft.com/library/windows/apps/mt228341

            var uri = new Uri(@"http://reader.livedwango.com/subscribe/" + url);
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
