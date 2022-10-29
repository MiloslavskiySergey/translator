using Blazorise;
using Blazorise.Bulma;
using Blazorise.Icons.FontAwesome;
using translator.Services;
#if WINDOWS
using translator.Platforms.Windows;
#endif

namespace translator;

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
            });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddBlazorise(options =>
        {
            options.Immediate = true;
        })
            .AddBulmaProviders()
            .AddFontAwesomeIcons();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<TranslatorService>();
#if WINDOWS
        builder.Services.AddSingleton<ISaveProgramFileDialogService, SaveProgramFileDialogWindowsService>();
#else
        builder.Services.AddSingleton<ISaveProgramFileDialogService, SaveProgramFileDialogBaseService>();
#endif

        return builder.Build();
    }
}
