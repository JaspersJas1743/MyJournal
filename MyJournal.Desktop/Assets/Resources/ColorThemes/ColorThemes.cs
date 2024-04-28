using Avalonia.Styling;

namespace MyJournal.Desktop.Assets.Resources.ColorThemes;

public static class ColorThemes
{
	public static ThemeVariant Light { get; } = new ThemeVariant(
		key: nameof(Light),
		inheritVariant: ThemeVariant.Light
	);

	public static ThemeVariant Dark { get; } = new ThemeVariant(
		key: nameof(Dark),
		inheritVariant: ThemeVariant.Dark
	);
}