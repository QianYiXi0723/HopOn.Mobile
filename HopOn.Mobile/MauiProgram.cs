using HopOn.Mobile.Core;
using Microsoft.Extensions.Logging;

namespace HopOn.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ISecureStore, MauiSecureStore>();
        builder.Services.AddSingleton<IHopOnDataSource>(sp =>
            new PcRemoteDataSource(sp.GetRequiredService<ISecureStore>()));
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
