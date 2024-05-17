using System;

namespace MyJournal.Desktop.Assets.Utilities;

[AttributeUsage(validOn: AttributeTargets.Class)]
public class UntestedAttribute(string? comment = null) : Attribute
{
	public string? Comment { get; } = comment;
}