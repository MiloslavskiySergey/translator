﻿using Blazorise;
using Blazorise.Bulma;
using Blazorise.Icons.FontAwesome;
using CommunityToolkit.Maui;
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
            .UseMauiCommunityToolkit()
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
        builder.Services.AddSingleton<ITerminalService, TerminalWindowsService>();
#else
        builder.Services.AddSingleton<ISaveProgramFileDialogService, SaveProgramFileDialogBaseService>();
        builder.Services.AddSingleton<ITerminalService, TerminalBaseService>();
#endif

        return builder.Build();
    }
}
