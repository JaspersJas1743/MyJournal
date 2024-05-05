using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public enum ComparisonOperations
{
	LessOrEquals,
	GreaterOrEquals
}

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
	public static readonly StyledProperty<double?> ThresholdPercentageProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, double?>(
		name: nameof(ThresholdPercentage)
	);
	public static readonly StyledProperty<ComparisonOperations?> ComparisonOperationsProperty = AvaloniaProperty.Register<ListBoxWithLoadOnScroll, ComparisonOperations?>(
		name: nameof(ComparisonOperations),
		defaultValue: Controls.ComparisonOperations.GreaterOrEquals
	);

	public ListBoxWithLoadOnScroll()
	{
		this.WhenAnyValue(property1: listBox => listBox.Scroll).WhereNotNull()
			.Subscribe(onNext: scroll => (scroll as ScrollViewer)!.ScrollChanged += OnScrollChanged);

		this.WhenAnyValue(property1: listBox => listBox.CurrentScrollHeight).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.MainThreadScheduler)
			.Where(predicate: offset => MaxScrollHeight > 0 && CheckOffset(offset: offset) && Items.Count >= 10)
			// .Where(predicate: offset => MaxScrollHeight > 0 && offset >= MaxScrollHeight / 5 * 4 && Items.Count >= 10)
			.Where(predicate: _ => Command is not null && Command.CanExecute(parameter: CommandParameter))
			.Subscribe(onNext: _ => Command!.Execute(parameter: CommandParameter));
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

	public double? ThresholdPercentage
	{
		get => GetValue(property: ThresholdPercentageProperty);
		set => SetValue(property: ThresholdPercentageProperty, value: value);
	}

	public ComparisonOperations? ComparisonOperations
	{
		get => GetValue(property: ComparisonOperationsProperty);
		set => SetValue(property: ComparisonOperationsProperty, value: value);
	}

	protected override Type StyleKeyOverride => typeof(ListBox);

	private double CalculatePercent(double? offset)
	{
		if (MaxScrollHeight is null)
			return 0;

		return (double)(offset / MaxScrollHeight)! * 100;
	}

	private bool CheckOffset(double? offset)
	{
		return ComparisonOperations switch
		{
			Controls.ComparisonOperations.LessOrEquals => CalculatePercent(offset: offset) <= ThresholdPercentage,
			Controls.ComparisonOperations.GreaterOrEquals => CalculatePercent(offset: offset) >= ThresholdPercentage,
			_ => false
		};
	}
}