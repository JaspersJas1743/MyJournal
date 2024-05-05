using System.Reactive;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public sealed class Attachment : ReactiveObject
{
	private string _fileName;
	private ReactiveCommand<Unit, Unit> _remove;
	private bool _isLoaded;

	public string FileName
	{
		get => _fileName;
		set => this.RaiseAndSetIfChanged(backingField: ref _fileName, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> Remove
	{
		get => _remove;
		set => this.RaiseAndSetIfChanged(backingField: ref _remove, newValue: value);
	}

	public bool IsLoaded
	{
		get => _isLoaded;
		set => this.RaiseAndSetIfChanged(backingField: ref _isLoaded, newValue: value);
	}
}