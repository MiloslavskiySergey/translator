﻿using Blazorise;
using Blazorise.Bulma;
using Blazorise.Icons.FontAwesome;
using translator.Services;
using translator.Windows.Main;

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
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddSingleton<TranslatorService>();

        return builder.Build();
    }
}
