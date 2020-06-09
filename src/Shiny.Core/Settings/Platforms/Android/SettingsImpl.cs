using System;
using Shiny.Infrastructure;
using Android.Content;
using Android.Preferences;


namespace Shiny.Settings
{
    public class SettingsImpl : AbstractSettings
    {
        readonly object syncLock = new object();
        readonly AndroidContext context;


        public SettingsImpl(AndroidContext context, ISerializer serializer) : base(serializer)
        {
            this.context = context;
        }


        void UoW(Action<ISharedPreferencesEditor> doWork)
        {
            lock (this.syncLock)
            {
                using (var editor = this.GetPrefs().Edit())
                {
                    doWork(editor);
                    editor.Commit();
                }
            }
        }


        public override bool Contains(string key)
        {
            lock (this.syncLock)
                return this.GetPrefs().Contains(key);
        }


        protected override object NativeGet(Type type, string key)
        {
            lock (this.syncLock)
            {
                using (var prefs = this.GetPrefs())
                {
                    var typeCode = Type.GetTypeCode(type);
                    switch (typeCode)
                    {

                        case TypeCode.Boolean:
                            return prefs.GetBoolean(key, false);

                        case TypeCode.Int32:
                            return prefs.GetInt(key, 0);

                        case TypeCode.Int64:
                            return prefs.GetLong(key, 0);

                        case TypeCode.Single:
                            return prefs.GetFloat(key, 0);

                        case TypeCode.String:
                            return prefs.GetString(key, String.Empty);

                        default:
                            var @string = prefs.GetString(key, String.Empty);
                            return Deserialize(type, @string);
                    }
                }
            }
        }


        protected override void NativeSet(Type type, string key, object value)
        {
            this.UoW(x =>
            {
                var typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        x.PutBoolean(key, (bool)value);
                        break;

                    case TypeCode.Int32:
                        x.PutInt(key, (int)value);
                        break;

                    case TypeCode.Int64:
                        x.PutLong(key, (long)value);
                        break;

                    case TypeCode.Single:
                        x.PutFloat(key, (float)value);
                        break;

                    case TypeCode.String:
                        x.PutString(key, (string)value);
                        break;

                    default:
                        var @string = this.Serialize(type, value);
                        x.PutString(key, @string);
                        break;
                }
            });
        }


        protected override void NativeRemove(string[] keys)
        {
            this.UoW(x =>
            {
                foreach (var key in keys)
                    x.Remove(key);
            });
        }


        protected override void NativeClear()
            => this.UoW(x => x.Clear());


        protected ISharedPreferences GetPrefs()
            => this.context.AppContext.GetSharedPreferences("Shiny", FileCreationMode.Private);
            //=> PreferenceManager.GetDefaultSharedPreferences(this.context.AppContext);
    }
}