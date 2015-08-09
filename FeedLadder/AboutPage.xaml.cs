using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace FeedLadder
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Launch review
            LaunchReview();
        }

        // Launch the URI
        async void LaunchReview()
        {
            // Launch the URI
            string appid = "8e3f11ec-9950-4ddc-8586-bcb8d9347a50";
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri("zune:reviewapp?appid=app" + appid));

            if (success)
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }
    }
}