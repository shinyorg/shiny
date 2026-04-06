using System;
using System.Linq;
using System.Collections.Generic;
using Foundation;

namespace Shiny;


public static class AppleExtensions
{
    readonly static DateTime reference = new(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);


    public static bool IsSimulator => false;


    public static DateTime ToDateTime(this NSDate date)
    {
        var utcDateTime = reference.AddSeconds(date.SecondsSinceReferenceDate);
        var dateTime = utcDateTime.ToLocalTime();
        return dateTime;
    }


    public static NSDate ToNSDate(this DateTime datetime)
    {
        var utcDateTime = datetime.ToUniversalTime();
        var date = NSDate.FromTimeIntervalSinceReferenceDate((utcDateTime - reference).TotalSeconds);
        return date;
    }


    public static IDictionary<string, string> FromNsDictionary(this NSDictionary ns)
    {
        var dict = new Dictionary<string, string>();
        if (ns != null)
        {
            foreach (var pair in ns)
                dict.Add(pair.Key.ToString(), pair.Value.ToString());
        }
        return dict;
    }


    public static NSDictionary ToNsDictionary(this IDictionary<string, string> dict)
        => NSDictionary.FromObjectsAndKeys(dict.Values.ToArray(), dict.Keys.ToArray());


    public static bool AssertInfoPlistEntry(string key, bool assert)
    {
        var contains = NSBundle.MainBundle.InfoDictionary.ContainsKey(new NSString(key));
        if (!contains)
        {
            if (assert)
                throw new ArgumentException($"You must set '{key}' in your Info.plist file");

            return false;
        }
        return true;
    }


    public static Guid ToGuid(this NSUuid uuid) => Guid.ParseExact(uuid.AsString(), "d");
    public static NSUuid ToNSUuid(this Guid guid) => new NSUuid(guid.ToString());
}
