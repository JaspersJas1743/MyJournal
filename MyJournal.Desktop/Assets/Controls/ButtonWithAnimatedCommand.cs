using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public sealed class ButtonWithAnimatedCommand : Button
{
	private bool _isSubscribed = false;

	public ButtonWithAnimatedCommand()
		=> Click += OnButtonClick;

	public static readonly StyledProperty<bool> CommandIsExecutingProperty =
		AvaloniaProperty.Register<Button, bool>(name: nameof(CommandIsExecuting));

	public bool CommandIsExecuting
	{
		get => GetValue(CommandIsExecutingProperty);
		private set => SetValue(property: CommandIsExecutingProperty, value: value);
	}

	private void OnButtonClick(object? o, RoutedEventArgs routedEventArgs)
	{
		if (_isSubscribed)
			return;
		_isSubscribed = !_isSubscribed;
		(Command as IReactiveCommand)!.IsExecuting.Subscribe(onNext: value => CommandIsExecuting = value);
	}
}