using System;
using System.Threading.Tasks;

namespace MyJournal.Desktop.Assets.Utilities;

public static class PlatformDetector
{
	private static void RunIfCurrentPlatform(Action action, Func<bool> predicate)
	{
		if (predicate())
			action();
	}

	private static async Task RunIfCurrentPlatform(Func<Task> action, Func<bool> predicate)
	{
		if (predicate())
			await action();
	}

	private static async Task<T?> RunIfCurrentPlatform<T>(Func<Task<T>> action, Func<bool> predicate)
	{
		if (predicate())
			return await action();

		return await Task.FromResult(result: default(T));
	}

	public static void RunIfCurrentPlatformIsWindows(Action action)
		=> RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsWindows);

	public static async Task RunIfCurrentPlatformIsWindows(Func<Task> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsWindows);

	public static async Task<T?> RunIfCurrentPlatformIsWindows<T>(Func<Task<T>> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsWindows);

	public static void RunIfCurrentPlatformIsLinux(Action action)
		=> RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsLinux);

	public static async Task RunIfCurrentPlatformIsLinux(Func<Task> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsLinux);

	public static async Task<T?> RunIfCurrentPlatformIsLinux<T>(Func<Task<T>> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsLinux);

	public static void RunIfCurrentPlatformIsMacOS(Action action)
		=> RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsMacOS);

	public static async Task RunIfCurrentPlatformIsMacOS(Func<Task> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsMacOS);

	public static async Task<T?> RunIfCurrentPlatformIsMacOS<T>(Func<Task<T>> action)
		=> await RunIfCurrentPlatform(action: action, predicate: OperatingSystem.IsMacOS);

	public static void Run(Action action)
	{
		switch (Environment.OSVersion.Platform)
		{
			case PlatformID.Unix: action(); break;
			case PlatformID.Win32NT: action(); break;
			case PlatformID.MacOSX: action(); break;
			default: throw new NotSupportedException();
		};
	}

	public static async Task Run(Func<Task> action)
	{
		switch (Environment.OSVersion.Platform)
		{
			case PlatformID.Unix: await action(); break;
			case PlatformID.Win32NT: await action(); break;
			case PlatformID.MacOSX: await action(); break;
			default: throw new NotSupportedException();
		};
	}

	public static async Task<T?> Run<T>(Func<Task<T>> action)
	{
		return Environment.OSVersion.Platform switch
		{
			PlatformID.Unix => await action(),
			PlatformID.Win32NT => await action(),
			PlatformID.MacOSX => await action(),
		};
	}
}