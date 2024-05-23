using System;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class IntegerConverters
{
	public static readonly IValueConverter IsNull = new FuncValueConverter<int, bool>(convert: num => num == default(Int32));
	public static readonly IValueConverter IsNotNull = new FuncValueConverter<int, bool>(convert: num => num != default(Int32));
}