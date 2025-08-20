using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using ObligatorioTT.Data;
using ObligatorioTT.Helpers;
using ObligatorioTT.Services;
<<<<<<< Updated upstream
using SQLitePCL;
using Microsoft.Maui.Controls.Maps; // ok dejarlo; si querés, podés envolverlo con #if ANDROID || IOS
=======

#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps; // Solo necesario en Android/iOS
#endif
>>>>>>> Stashed changes

namespace ObligatorioTT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SQLitePCL.Batteries_V2.Init();

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
            // Habilita MAUI Maps SOLO en Android/iOS (Windows queda aislado)
            builder.UseMauiMaps();
#endif

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "appdata.db3");

            builder.Services.AddSingleton<DatabaseService>(_ =>
            {
                var svc = new DatabaseService(dbPath);
                Task.Run(() => svc.InitAsync()).Wait();
                return svc;
            });

            // Servicio biométrico (si más adelante querés limitarlo a Android/iOS, se puede envolver con #if)
            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();

            // ✅ Registro seguro por defecto: implementación vacía del mapa (no rompe Windows)
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
