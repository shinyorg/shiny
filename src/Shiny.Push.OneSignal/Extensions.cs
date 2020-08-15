using System;
using System.Collections.Generic;
using System.Linq;
using Com.OneSignal.Abstractions;


namespace Shiny.Push.OneSignal
{
    static class Extensions
    {
        public static IDictionary<string, string> ToDictionary(this OSNotificationPayload payload)
            => payload?
                .additionalData?
                .ToDictionary(
                    y => y.Key,
                    y => y.Value.ToString()
                )
                ?? new Dictionary<string, string>(0);
    }
}
