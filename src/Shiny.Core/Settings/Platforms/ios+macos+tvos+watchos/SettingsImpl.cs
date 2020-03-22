using System;
using Shiny.Infrastructure;
using Foundation;


namespace Shiny.Settings
{
    public class SettingsImpl : AbstractSettings
    {
        readonly NSUserDefaults prefs;
        readonly object syncLock;


        public SettingsImpl(ISerializer serializer, string? nameSpace = null) : base(serializer)
        {
            this.syncLock = new object();
            this.prefs = nameSpace == null
                ? NSUserDefaults.StandardUserDefaults
                : new NSUserDefaults(nameSpace, NSUserDefaultsType.SuiteName);
        }


        public override bool Contains(string key) => this.prefs.ValueForKey(new NSString(key)) != null;


        protected override object NativeGet(Type type, string key)
        {
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return this.prefs.BoolForKey(key);

                case TypeCode.Double:
                    return this.prefs.DoubleForKey(key);

                case TypeCode.Int32:
                    return (int)this.prefs.IntForKey(key);

                case TypeCode.Single:
                    return (float)this.prefs.FloatForKey(key);

                case TypeCode.String:
                    return this.prefs.StringForKey(key);

                default:
                    var @string = this.prefs.StringForKey(key);
                    return this.Deserialize(type, @string);
            }
        }


        protected override void NativeSet(Type type, string key, object value)
        {
            lock (this.syncLock)
            {
                var typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        this.prefs.SetBool((bool)value, key);
                        break;

                    case TypeCode.Double:
                        this.prefs.SetDouble((double)value, key);
                        break;

                    case TypeCode.Int32:
                        this.prefs.SetInt((int)value, key);
                        break;

                    case TypeCode.String:
                        this.prefs.SetString((string)value, key);
                        break;

                    default:
                        var @string = this.Serialize(type, value);
                        this.prefs.SetString(@string, key);
                        break;
                }
                this.prefs.Synchronize();
            }
        }


        protected override void NativeRemove(string[] keys)
        {
            lock (this.syncLock)
            {
                foreach (var key in keys)
                    this.prefs.RemoveObject(key);

                this.prefs.Synchronize();
            }
        }


        protected override void NativeClear()
        {
            lock (this.syncLock)
            {
                foreach (var key in this.prefs.ToDictionary())
                    this.prefs.RemoveObject(key.Key.ToString());

                this.prefs.Synchronize();
            }
        }
    }
}