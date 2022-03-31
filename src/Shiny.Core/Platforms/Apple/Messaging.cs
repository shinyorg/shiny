using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace Shiny
{
    //    services.AddSingleton<IInterface1>();
    //services.AddSingleton<IInterface2>(x => x.GetService<IInterface1>());
    //public static class ServiceCollectionExt
    //{
    //    public static void AddSingleton<I1, I2, T>(this IServiceCollection services)
    //        where T : class, I1, I2
    //        where I1 : class
    //        where I2 : class
    //    {
    //        services.AddSingleton<I1, T>();
    //        services.AddSingleton<I2, T>(x => (T)x.GetService<I1>());
    //    }
    //}


    //http://jonathanpeppers.com/Blog/xamarin-ios-under-the-hood-calling-objective-c-from-csharp

    public static class Messaging
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);
        //[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        //public static extern IntPtr IntPtr_objc_msgSend_byte(IntPtr receiver, IntPtr selector, byte arg1);

        //[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        //public static extern IntPtr IntPtr_objc_msgSend_Double(IntPtr receiver, IntPtr selector, double arg1);
    }

    //class MyClass
    //{
    //    @objc
    //    func application(_ application: UIApplication, didReceiveRemoteNotification userInfo: [AnyHashable: Any], fetchCompletionHandler completionHandler: @escaping (UIBackgroundFetchResult) -> Void)
    //    {
    //        // my code
    //    }

    //    private func swizzleDidReceiveRemoteNotification()
    //    {
    //        let appDelegate = UIApplication.shared.delegate
    //    let appDelegateClass = object_getClass(appDelegate)
    
    //    let originalSelector = #selector(UIApplicationDelegate.application(_:didReceiveRemoteNotification:fetchCompletionHandler:))
    //    let swizzledSelector = #selector(MyClass.self.application(_:didReceiveRemoteNotification:fetchCompletionHandler:))

    //    guard let swizzledMethod = class_getInstanceMethod(MyClass.self, swizzledSelector) else
    //        {
    //            return
    //    }

    //        if let originalMethod = class_getInstanceMethod(appDelegateClass, originalSelector)  {
    //            // exchange implementation
    //            method_exchangeImplementations(originalMethod, swizzledMethod)
    //        } else
    //        {
    //            // add implementation
    //            class_addMethod(appDelegateClass, swizzledSelector, method_getImplementation(swizzledMethod), method_getTypeEncoding(swizzledMethod))
    //    }
    //    }
    //}

    public class TestObject
    {
        private IntPtr _handle;

        public TestObject()
        {
            var classHandle = Class.GetHandle("NSObject");
            var alloc = new Selector("alloc");
            var init = new Selector("init");
            this._handle = Messaging.IntPtr_objc_msgSend(classHandle, alloc.Handle);
            this._handle = Messaging.IntPtr_objc_msgSend(this._handle, init.Handle);
        }

        ~TestObject()
        {
            this.Dispose();
        }

        public void MakeItRain(string currency)
        {
            var selector = new Selector("makeItRain:");
            var currencyString = new NSString(currency);
            Messaging.void_objc_msgSend_IntPtr(this._handle, selector.Handle, currencyString.Handle);
            currencyString.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            var release = new Selector("release");
            Messaging.void_objc_msgSend(this._handle, release.Handle);
        }
    }
}
