using System;
using System.Net;
using System.Windows.Data;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace FeedLadder
{
    ///////////////////////////////////////////////////////////////////////////////////////////////
    // オプション Converter：RssテキストでHTMLタグが入ってしまう場合など
    // 
    // XAMLのテキストバインディングにConverterオプションをつける
    // TextBlock Text="{Binding Summary.Text}}"
    //  　　　　　　　　　　　　　↓
    // TextBlock Text="{Binding Summary.Text, Converter={StaticResource RssTextTrimmer}}"
    ///////////////////////////////////////////////////////////////////////////////////////////////

    public class RssTextTrimmer : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            int strLength = 0;
            string fixedString = "";

            fixedString = System.Text.RegularExpressions.Regex.Replace(value.ToString(), "<[^>]+>", string.Empty);
            fixedString = fixedString.Replace("\r", "").Replace("\n", "");
            fixedString = HttpUtility.HtmlDecode(fixedString);
            strLength = fixedString.ToString().Length;

            if (strLength == 0) return null;
            else if (strLength >= 200) fixedString = fixedString.Substring(0, 200);
            return fixedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// C#のWebRequestとWebClientでCookie認証をする方法
    /// from http://neue.cc/2009/12/17_230.html
    /// WebClientでクッキーを使用する方法
    /// from http://www.crystal-creation.com/software/technical-information/programming/c-sharp/network/web-client.htm
    /// </summary>
    public class CustomWebClient : WebClient
    {
        private CookieContainer cookieContainer = new CookieContainer();

        public CookieContainer CookieContainer
        {
            get
            {
                return cookieContainer;
            }
            set
            {
                cookieContainer = value;
            }
        }

        // WebClientはWebRequestのラッパーにすぎないので、
        // GetWebRequestのところの動作をちょっと横取りして書き換える
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = cookieContainer;
            }
            return request;
        }
    }

    /// <summary>
    /// UNIX時間を求めるには？
    /// from http://www.atmarkit.co.jp/fdotnet/dotnettips/980unixtime/unixtime.html
    /// </summary>
    public class UnixEpochTime
    {
        // UNIXエポックを表すDateTimeオブジェクトを取得
        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static long GetUnixTime(DateTime targetTime)
        {
            // UTC時間に変換
            targetTime = targetTime.ToUniversalTime();

            // UNIXエポックからの経過時間を取得
            TimeSpan elapsedTime = targetTime - UNIX_EPOCH;

            // 経過秒数に変換
            return (long)elapsedTime.TotalSeconds;
        }
    }


    /// <summary>
    /// 設定値を暗号化して保存するディクショナリ
    /// from http://d.hatena.ne.jp/iseebi/20121110/p1
    /// </summary>
    public class ProtectedSettingDictionary : Dictionary<string, string>
    {
        private DataContractSerializer serializer = new DataContractSerializer(typeof(SerializeItem[]));

        /// <summary>
        /// 読み書きするIsolatedStorage上のパス
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filePath">読み書きするファイルのパス</param>
        public ProtectedSettingDictionary(string filePath)
        {
            FilePath = filePath;
            Load();
        }

        /// <summary>
        /// ファイルから設定値を読み込む。ファイルがない場合は初期化する。
        /// </summary>
        public void Load()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                this.Clear();

                if (store.FileExists(FilePath))
                {
                    using (var mstream = new MemoryStream())
                    {
                        byte[] cryptedData;
                        byte[] decryptedData;
                        using (var file = store.OpenFile(FilePath, System.IO.FileMode.Open))
                        {
                            cryptedData = new byte[file.Length];
                            file.Read(cryptedData, 0, cryptedData.Length);
                        }

                        decryptedData = ProtectedData.Unprotect(cryptedData, null);
                        mstream.Write(decryptedData, 0, decryptedData.Length);
                        mstream.Seek(0, SeekOrigin.Begin);

                        var objects = (SerializeItem[])serializer.ReadObject(mstream);
                        foreach (var item in objects)
                        {
                            this.Add(item.Key, item.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 設定値を保存する
        /// </summary>
        public void Save()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var objects = this.Select(i => new SerializeItem { Key = i.Key, Value = i.Value })
                    .ToArray();
                using (var mstream = new MemoryStream())
                {
                    serializer.WriteObject(mstream, objects);
                    mstream.Seek(0, SeekOrigin.Begin);

                    var decryptedData = new byte[mstream.Length];
                    mstream.Read(decryptedData, 0, decryptedData.Length);
                    var cryptedData = ProtectedData.Protect(decryptedData, null);

                    using (var file = store.OpenFile(FilePath, FileMode.Create))
                    {
                        file.Write(cryptedData, 0, cryptedData.Length);
                    }
                }
            }
        }

        [DataContract]
        public class SerializeItem
        {
            [DataMember]
            public string Key { get; set; }
            [DataMember]
            public string Value { get; set; }
        }
    }

    /// <summary>
    /// data of LongListSelector by grouping
    /// refer to http://msdn.microsoft.com/ja-jp/library/windowsphone/develop/jj244365(v=vs.105).aspx
    /// </summary>
    public class Group<T> : List<T>
    {
        /// <summary>
        /// The delegate that is used to get the key information.
        /// </summary>
        /// <param name="item">An object of type T</param>
        /// <returns>The key value to use for this object</returns>
        public delegate string GetKeyDelegate(T item);

        /// <summary>
        /// The Key of this group.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="key">The key for this group.</param>
        public Group(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Create a list of Group[T] with keys set by a string.
        /// </summary>
        /// <param name="items">The items to place in the groups.</param>
        /// <param name="getKey">A delegate to get the key from an item.</param>
        /// <param name="descendingSort">Will sort the group by descending order if true.</param>
        /// <returns>An items source for a LongListSelector</returns>
        public static List<Group<T>> CreateGroups(IEnumerable<T> items, GetKeyDelegate getKey, bool descendingSort)
        {
            List<Group<T>> list = new List<Group<T>>();

            foreach (T item in items)
            {
                bool found = false;
                // if Group for item is in List, add item
                foreach (Group<T> group in list)
                {
                    if (getKey(item).Equals(group.Key))
                    {
                        group.Add(item);
                        found = true;
                        break;
                    }
                }
                // if not, create Group and add to List, and add item
                if (!found)
                {
                    Group<T> newGroup = new Group<T>(getKey(item));
                    newGroup.Add(item);
                    list.Add(newGroup);
                }
            }

            if (descendingSort)
            {
                // sort group, not item
                list.Sort((c0, c1) => { return c1.Key.CompareTo(c0.Key); });
            }

            return list;
        }
    }


    /// <summary>
    /// refer to http://chorusde.hatenablog.jp/entry/20110910/1315620186
    /// </summary>
    [DataContract]
    public class SubscriptionItem
    {
        /// <summary>
        /// subscribe_id
        /// </summary>
        [DataMember(Name = "subscribe_id")]
        public string SubscribeID { get; set; }

        /// <summary>
        /// title
        /// </summary>
        [DataMember(Name = "title")]
        public string Title { get; set; }

        /// <summary>
        /// unread_count
        /// </summary>
        [DataMember(Name = "unread_count")]
        public string UnreadCount { get; set; }

        /// <summary>
        /// folder
        /// </summary>
        [DataMember(Name = "folder")]
        public string Folder { get; set; }

        /// <summary>
        /// rate
        /// </summary>
        [DataMember(Name = "rate")]
        public string Rate{ get; set; }

        // read flag
        public bool isRead { get; set; }
    }

    /// <summary>
    /// refer to http://chorusde.hatenablog.jp/entry/20110910/1315620186
    /// </summary>
    [DataContract]
    public class FeedItems
    {
        /// <summary>
        /// items
        /// </summary>
        [DataMember(Name = "items")]
        public List<FeedItem> Items { get; set; }
    }

    /// <summary>
    /// refer to http://chorusde.hatenablog.jp/entry/20110910/1315620186
    /// </summary>
    [DataContract]
    public class FeedItem
    {
        /// <summary>
        /// title
        /// </summary>
        [DataMember(Name = "title")]
        public string Title { get; set; }

        /// <summary>
        /// link
        /// </summary>
        [DataMember(Name = "link")]
        public string Link { get; set; }

        /// <summary>
        /// body
        /// </summary>
        [DataMember(Name = "body")]
        public string Body { get; set; }

        // Pin
        public bool isPinned { get; set; }
    }

    /// <summary>
    /// refer to http://chorusde.hatenablog.jp/entry/20110910/1315620186
    /// </summary>
    [DataContract]
    public class PinItem
    {
        /// <summary>
        /// title
        /// </summary>
        [DataMember(Name = "title")]
        public string Title { get; set; }

        /// <summary>
        /// link
        /// </summary>
        [DataMember(Name = "link")]
        public string Link { get; set; }
    }

}
