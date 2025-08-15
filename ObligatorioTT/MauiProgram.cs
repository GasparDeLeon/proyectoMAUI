using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using ObligatorioTT.Data;
using ObligatorioTT.Helpers; 
using ObligatorioTT.Services;
using SQLitePCL;
using Microsoft.Maui.Controls.Maps;

namespace ObligatorioTT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseMauiMaps();

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "appdata.db3");

            builder.Services.AddSingleton<DatabaseService>(_ =>
            {
                var svc = new DatabaseService(dbPath);
                Task.Run(() => svc.InitAsync()).Wait();
                return svc; 
            });


            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif


            var app = builder.Build();
            ServiceHelper.Services = app.Services;
            return app;
        }
    }
}
