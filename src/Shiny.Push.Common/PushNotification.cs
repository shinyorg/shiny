﻿using System.Collections.Generic;

namespace Shiny.Push;


public record PushNotification(
    IDictionary<string, string> data,
    Notification? Notification
);