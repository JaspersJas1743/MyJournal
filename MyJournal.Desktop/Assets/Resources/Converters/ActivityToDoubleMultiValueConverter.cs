using System;
using System.Linq;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class ActivityToDoubleMultiValueConverter
{
	public static readonly IMultiValueConverter Instance =
		new FuncMultiValueConverter<bool, double>(x => Convert.ToDouble(value: x.All(y => y)));
}