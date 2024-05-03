using System.Reactive;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public sealed class Attachment
{
	public string FileName { get; set; }
	public ReactiveCommand<Unit, Unit> Remove { get; set; }
}