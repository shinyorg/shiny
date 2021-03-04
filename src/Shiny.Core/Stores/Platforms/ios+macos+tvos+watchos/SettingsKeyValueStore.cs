using System;


namespace Shiny.Stores
{
    public class SettingsKeyValueStore : IKeyValueStore
    {
        public void Clear() => throw new NotImplementedException();
        public bool Contains(string key) => throw new NotImplementedException();
        public T? Get<T>(string key) => throw new NotImplementedException();
        public object Get(Type type, string key) => throw new NotImplementedException();
        public bool Remove(string key) => throw new NotImplementedException();
        public void Set<T>(string key, T value) => throw new NotImplementedException();
        public void Set(string key, object value) => throw new NotImplementedException();
    }
}
//NSUserDefaults Prefs() => this.nameSpace.IsEmpty()
//            ? NSUserDefaults.StandardUserDefaults
//            : new NSUserDefaults(this.nameSpace, NSUserDefaultsType.SuiteName);


//T Get<T>(Func<NSUserDefaults, T> getter)
//{
//    lock (this.syncLock)
//    {
//        using (var native = this.Prefs())
//            return getter(native);
//    }
//}


//void Do(Action<NSUserDefaults> action)
//{
//    lock (this.syncLock)
//    {
//        using (var native = this.Prefs())
//        {
//            action(native);
//            native.Synchronize();
//        }
//    }
//}


//public override bool Contains(string key) => this.Get(x => x.ValueForKey(new NSString(key)) != null);


//protected override object NativeGet(Type type, string key) => this.Get<object>(prefs =>
//{
//    var typeCode = Type.GetTypeCode(type);

//    switch (typeCode)
//    {
//        case TypeCode.Boolean:
//            return prefs.BoolForKey(key);

//        case TypeCode.Double:
//            return prefs.DoubleForKey(key);

//        case TypeCode.Int32:
//            return (int)prefs.IntForKey(key);

//        case TypeCode.Single:
//            return (float)prefs.FloatForKey(key);

//        case TypeCode.String:
//            return prefs.StringForKey(key);

//        default:
//            var @string = prefs.StringForKey(key);
//            return this.Deserialize(type, @string);
//    }
//});


//protected override void NativeSet(Type type, string key, object value) => this.Do(prefs =>
//{
//    var typeCode = Type.GetTypeCode(type);
//    switch (typeCode)
//    {
//        case TypeCode.Boolean:
//            prefs.SetBool((bool)value, key);
//            break;

//        case TypeCode.Double:
//            prefs.SetDouble((double)value, key);
//            break;

//        case TypeCode.Int32:
//            prefs.SetInt((int)value, key);
//            break;

//        case TypeCode.String:
//            prefs.SetString((string)value, key);
//            break;

//        default:
//            var @string = this.Serialize(type, value);
//            prefs.SetString(@string, key);
//            break;
//    }
//});


//protected override void NativeRemove(string[] keys) => this.Do(prefs =>
//{
//    foreach (var key in keys)
//        prefs.RemoveObject(key);
//});


//protected override void NativeClear() => this.Do(prefs =>
//{
//    foreach (var key in prefs.ToDictionary())
//        prefs.RemoveObject(key.Key.ToString());
//});