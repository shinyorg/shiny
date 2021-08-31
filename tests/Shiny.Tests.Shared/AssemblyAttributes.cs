#if __IOS__ || __ANDROID__ || WINDOWS_UWP
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
#endif