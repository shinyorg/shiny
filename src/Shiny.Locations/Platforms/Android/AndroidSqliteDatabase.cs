using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Android.Database;
using Android.Database.Sqlite;


namespace Shiny
{
    public class AndroidSqliteDatabase : SQLiteOpenHelper
    {
        readonly object syncLock = new object();


        public AndroidSqliteDatabase(AndroidPlatform platform) : base(platform.AppContext, "shinystore.db", null, 1)
        {
        }

        public override void OnCreate(SQLiteDatabase db)
        {
            db.ExecSQL("CREATE TABLE motion_activity(Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Confidence FLOAT NOT NULL, Event INTEGER NOT NULL, Timestamp INTEGER NOT NULL)");
            db.ExecSQL("CREATE INDEX idx_timestamp ON motion_activity(Timestamp);");
        }


        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
        }


        public virtual Task ExecuteNonQuery(string sql) => Task.Run(() =>
        {
            lock (this.syncLock)
                this.WritableDatabase.ExecSQL(sql);
        });


        public virtual Task<IList<T>> RawQuery<T>(string sql, Func<ICursor, T> builder) => Task.Run<IList<T>>(() =>
        {
            lock (this.syncLock)
            {
                var list = new List<T>();
                using (var cursor = this.WritableDatabase.RawQuery(sql, null))
                {
                    while (cursor.MoveToNext())
                    {
                        var obj = builder(cursor);
                        list.Add(obj);
                    }
                }
                return list;
            }
        });


        public virtual Task<IList<T>> GetList<T>(string sql) where T : new() => Task.Run<IList<T>>(() =>
        {
            //var values = new Android.Content.ContentValues();
            //values.Put()
            //this.WritableDatabase.Insert("", null, values);
            lock (this.syncLock)
            {
                var list = new List<T>();
                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

                using (var cursor = this.WritableDatabase.RawQuery(sql, null))
                {
                    while (cursor.MoveToNext())
                    {
                        var obj = new T();
                        foreach (var prop in props)
                        {
                            var value = this.Get(cursor, prop.Name);
                            prop.SetValue(obj, value);
                        }
                        list.Add(obj);
                    }
                }
                return list;
            }
        });


        public virtual Task<T> Get<T>(string sql) where T : new() => Task.Run(() =>
        {
            using (var cursor = this.WritableDatabase.RawQuery(sql, null))
            {
                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
                if (cursor.MoveToNext())
                {
                    var obj = new T();
                    foreach (var prop in props)
                    {
                        var value = this.Get(cursor, prop.Name);
                        prop.SetValue(obj, value);
                    }
                    return obj;
                }
            }
            return default;
        });


        protected object Get(ICursor cursor, string columnName)
        {
            var index = cursor.GetColumnIndex(columnName);
            var fieldType = cursor.GetType(index);
            switch (fieldType)
            {
                case FieldType.Blob:
                    return cursor.GetBlob(index);

                case FieldType.Float:
                    return cursor.GetFloat(index);

                case FieldType.Integer:
                    return cursor.GetInt(index);

                case FieldType.String:
                    return cursor.GetString(index);

                case FieldType.Null:
                default:
                    return null;
            }
        }
    }
}
