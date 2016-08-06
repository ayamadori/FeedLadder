using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FeedLadder
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            // https://github.com/Microsoft/Windows-task-snippets/blob/master/tasks/Store-app-rating-pop-up.md
            var uriReview = new Uri($"ms-windows-store://REVIEW?PFN={Package.Current.Id.FamilyName}");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriReview);
        }

    }
}
