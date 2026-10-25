using SmartDocs.Services;
using SmartDocs.Views;

namespace SmartDocs;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
                fonts.AddFont("MaterialIconsOutlined-Regular.otf", "MaterialIcons");
            });

        // Services (client HTTP de l'API, authentification, documents)
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<DocumentService>();
        builder.Services.AddSingleton<AdminService>();
        builder.Services.AddSingleton<ActivityService>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<DocumentsPage>();
        builder.Services.AddTransient<UploadPage>();
        builder.Services.AddTransient<ArchivesPage>();
        builder.Services.AddTransient<DocumentDetailPage>();
        builder.Services.AddTransient<VersionsPage>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<TrashPage>();
        builder.Services.AddTransient<ActivityPage>();
        builder.Services.AddTransient<InvitationsPage>();

        // Supprime le soulignement natif Android des champs de saisie (les champs sont habillés par des Border)
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        return builder.Build();
    }
}
