using System;
using System.IO;
using SQLite;

namespace MvvmCrossJarrod.Plugins.Sqlite.Touch
{
    public class MvxTouchSQLiteConnectionFactory : ISQLiteConnectionFactory
    {
        public ISQLiteConnection Create(string address)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return new SQLiteConnection(Path.Combine(path, address));
        }

        public ISQLiteAsyncConnection AsyncCreate(string address)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return new SQLiteAsyncConnection(Path.Combine(path, address));
        }
    }
}