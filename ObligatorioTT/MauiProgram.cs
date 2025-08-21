using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using SQLitePCL;

using ObligatorioTT.Data;
using ObligatorioTT.Helpers;
using ObligatorioTT.Services;

#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;
#endif

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
                    fonts.AddFont("Arial.ttf", "ArialAlias");
                });

#if ANDROID || IOS
            // Habilitar Maps en Android/iOS
            builder.UseMauiMaps();
#endif

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "appdata.db4");

            // Registrar DatabaseService SIN bloquear la UI (InitAsync se llama en App.xaml.cs)
            builder.Services.AddSingleton(new DatabaseService(dbPath));

            // Servicio biométrico
            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();

            // Implementación No-Op del mapa para que Windows no rompa
            builder.Services.AddSingleton<ISponsorMapView, NoOpSponsorMapView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            ServiceHelper.Services = app.Services;
            return app;
        }
    }
}
