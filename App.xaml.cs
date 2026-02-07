using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ChordBox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Log unhandled exceptions to console and file
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("UI Thread Exception", e.Exception);
        MessageBox.Show(e.Exception.Message, "ChordBox Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogError("Fatal Exception", ex);
    }

    private static void LogError(string context, Exception ex)
    {
        string msg = $"[{DateTime.Now:HH:mm:ss}] {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n";
        Console.Error.WriteLine(msg);
        try
        {
            File.AppendAllText("chordbox-error.log", msg);
        }
        catch { }
    }
}

