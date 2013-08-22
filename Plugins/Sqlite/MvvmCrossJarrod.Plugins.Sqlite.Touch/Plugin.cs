using Cirrious.CrossCore;
using Cirrious.CrossCore.Plugins;

namespace MvvmCrossJarrod.Plugins.Sqlite.Touch
{
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterSingleton<ISQLiteConnectionFactory>(new MvxTouchSQLiteConnectionFactory());
        }
    }
}