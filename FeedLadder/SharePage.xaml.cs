using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
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

            // Delay before opening browser (I can't understand necessity...)
            // http://blog.okazuki.jp/entry/20120302/1330643881
            await Task.Delay(TimeSpan.FromMilliseconds(1000));

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
