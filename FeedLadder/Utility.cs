using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FeedLadder
{
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
