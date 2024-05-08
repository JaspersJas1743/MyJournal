using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace MyJournal.Desktop;

public sealed class Program
{
	[STAThread]
	public static void Main(string[] args)
		=> BuildAvaloniaApp().StartWithClassicDesktopLifetime(args: args);

	private static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().UseReactiveUI();
}