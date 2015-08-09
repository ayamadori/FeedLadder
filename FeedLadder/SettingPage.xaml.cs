using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Windows.Controls;

namespace FeedLadder
{
    public partial class SettingPage : PhoneApplicationPage
    {
        private ProtectedSettingDictionary account;

        // http://blog.ch3cooh.jp/entry/20110407/1302201270
        private IsolatedStorageSettings sort;
        private string sortMode;

        public SettingPage()
        {
            InitializeComponent();
            account = new ProtectedSettingDictionary("ProtectedStore");
            sort = IsolatedStorageSettings.ApplicationSettings;

        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.account.ContainsKey("UserName"))
            {
                this.usernameTextBox.Text = this.account["UserName"];
            }
            if (this.account.ContainsKey("Password"))
            {
                this.passwordBox.Password = this.account["Password"];
            }

            sortMode = "";
            if (this.sort.TryGetValue<string>("Sort", out sortMode))
            {
                switch(sortMode)
                {
                    case "Folder":
                        radioButton1.IsChecked = true;
                        break;
                    case "Rate":
                        radioButton2.IsChecked = true;
                        break;
                }

            }
            else //default is "Folder"
            {
                radioButton1.IsChecked = true;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.account["UserName"] = this.usernameTextBox.Text;
            this.account["Password"] = this.passwordBox.Password;
            this.account.Save();

            this.sort["Sort"] = sortMode;
            this.sort.Save();

            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        // https://social.msdn.microsoft.com/Forums/ja-JP/757efbc7-117a-4ffd-b572-e2c36ef81ef4/textblock?forum=wp7devtoolja
        private void radioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            sortMode = (string)radioButton.Content;
        }

    }
}