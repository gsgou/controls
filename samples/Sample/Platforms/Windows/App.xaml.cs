using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Sample.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
		this.UnhandledException += (_, e) => Dump("UnhandledException", e.Exception);
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Dump("AppDomain", e.ExceptionObject as Exception);
	}

	static void Dump(string source, Exception? ex)
	{
		try
		{
			System.IO.File.AppendAllText(
				System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sample-crash.log"),
				$"=== {source} @ {DateTime.Now:O} ==={Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}"
			);
		}
		catch { }
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

