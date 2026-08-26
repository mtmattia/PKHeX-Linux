using Avalonia;
using System;

namespace PKHeX.Avalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Render dropdown/combo popups inside the main window. On Linux (X11/XWayland under
            // KWin/Wayland) native popups can open misaligned from the control; overlay popups
            // are positioned relative to the control and stay aligned.
            .With(new X11PlatformOptions { OverlayPopups = true })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
