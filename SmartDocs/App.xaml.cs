namespace SmartDocs;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light; // les maquettes n'existent qu'en thème clair
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
#if WINDOWS
        // Les maquettes sont au format mobile : on ouvre une fenêtre étroite sous Windows
        window.Width = 420;
        window.Height = 880;
#endif
        return window;
    }
}
