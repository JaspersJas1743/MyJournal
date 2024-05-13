using System;
using System.Linq;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class BoolToDoubleMultiValueConverter
{
	public static readonly IMultiValueConverter And =
		new FuncMultiValueConverter<bool, double>(x => Convert.ToDouble(value: x.All(y => y)));

	public static readonly IMultiValueConverter Or =
		new FuncMultiValueConverter<bool, double>(x => Convert.ToDouble(value: x.Any(y => y)));
}