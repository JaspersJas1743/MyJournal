using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public sealed class ListBoxWithLoadOnScroll : ListBox
{
	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, ICommand?>(
		name: nameof(Command)
	);
	public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, object?>(
		name: nameof(CommandParameter)
	);
	public static readonly StyledProperty<double?> MaxScrollHeightProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, double?>(
		name: nameof(MaxScrollHeight)
	);
	public static readonly StyledProperty<double?> CurrentScrollHeightProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, double?>(
		name: nameof(CurrentScrollHeight)
	);

	public ListBoxWithLoadOnScroll()
	{
		this.WhenAnyValue(property1: listBox => listBox.Scroll).WhereNotNull()
			.Subscribe(onNext: scroll => (scroll as ScrollViewer)!.ScrollChanged += OnScrollChanged);

		this.WhenAnyValue(property1: listBox => listBox.CurrentScrollHeight).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.75))
			.Where(predicate: offset => Dispatcher.UIThread.Invoke(callback: () => MaxScrollHeight > 0 && offset >= MaxScrollHeight / 4 * 3 && Items.Count >= 10))
			.Where(predicate: _ => Dispatcher.UIThread.Invoke(callback: () => Command is not null && Command.CanExecute(parameter: CommandParameter)))
			.Subscribe(onNext: _ => Dispatcher.UIThread.Invoke(callback: () => Command!.Execute(parameter: CommandParameter)));
	}

	private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		ScrollViewer scrollViewer = (sender as ScrollViewer)!;

		CurrentScrollHeight = scrollViewer.Offset.Y;
		MaxScrollHeight = scrollViewer.ScrollBarMaximum.Y;
	}

	private double? MaxScrollHeight
	{
		get => GetValue(property: MaxScrollHeightProperty);
		set => SetValue(property: MaxScrollHeightProperty, value: value);
	}

	private double? CurrentScrollHeight
	{
		get => GetValue(property: CurrentScrollHeightProperty);
		set => SetValue(property: CurrentScrollHeightProperty, value: value);
	}

	public ICommand? Command
	{
		get => GetValue(property: CommandProperty);
		set => SetValue(property: CommandProperty, value: value);
	}
	public object? CommandParameter
	{
		get => GetValue(property: CommandParameterProperty);
		set => SetValue(property: CommandParameterProperty, value: value);
	}

	protected override Type StyleKeyOverride => typeof(ListBox);
}