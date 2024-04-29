using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DynamicData;
using Markdown.Avalonia.Controls;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public partial class CodeInput : UserControl
{
	private const string DefaultClassForCell = "CodeEntryCell";
	private const string ClassForErrorCell = "ErrorCodeEntryCell";

	public static readonly StyledProperty<string> EntryCodeProperty = AvaloniaProperty.Register<CodeInput, string>(
		name: nameof(EntryCode),
		defaultValue: String.Empty,
		defaultBindingMode: BindingMode.TwoWay
	);
	public static readonly StyledProperty<bool> HaveErrorProperty = AvaloniaProperty.Register<CodeInput, bool>(
		name: nameof(HaveError),
		defaultBindingMode: BindingMode.TwoWay
	);
	public static readonly StyledProperty<int> CountOfCellProperty = AvaloniaProperty.Register<CodeInput, int>(
		name: nameof(CountOfCell),
		defaultValue: 7
	);
	public static readonly StyledProperty<bool> CodeIsEnteredProperty = AvaloniaProperty.Register<CodeInput, bool>(
		name: nameof(CodeIsEntered),
		defaultValue: false
	);
	public static readonly StyledProperty<ICommand?> CompletedCommandProperty = AvaloniaProperty.Register<CodeInput, ICommand?>(
		name: nameof(CompletedCommand)
	);
	public static readonly StyledProperty<object?> CompletedCommandParameterProperty = AvaloniaProperty.Register<CodeInput, object?>(
		name: nameof(CompletedCommandParameter)
	);
	public static readonly RoutedEvent<RoutedEventArgs> CompletedCodeEvent = RoutedEvent.Register<CodeInput, RoutedEventArgs>(
		name: nameof(CompletedCode),
		routingStrategy: RoutingStrategies.Direct
	);

	private IEnumerable<TextBox> _codeCells;

	public CodeInput()
	{
		InitializeComponent();

		this.WhenAnyValue(property1: input => input.EntryCode.Length)
			.Subscribe(onNext: length => CodeIsEntered = length == CountOfCell);

		this.WhenAnyValue(property1: input => input.EntryCode).Where(predicate: _ => CodeIsEntered).Subscribe(onNext: _ =>
		{
			RaiseEvent(e: new RoutedEventArgs(CompletedCodeEvent));

			if (CompletedCommand?.CanExecute(parameter: CompletedCommandParameter) == true)
				CompletedCommand?.Execute(parameter: CompletedCommandParameter);
		});
	}

	public event EventHandler<RoutedEventArgs> CompletedCode
	{
		add => AddHandler(routedEvent: CompletedCodeEvent, handler: value);
		remove => RemoveHandler(routedEvent: CompletedCodeEvent, handler: value);
	}

	public string? EntryCode
	{
		get => GetValue(property: EntryCodeProperty);
		set => SetValue(property: EntryCodeProperty, value: value);
	}

	public bool CodeIsEntered
	{
		get => GetValue(property: CodeIsEnteredProperty);
		set => SetValue(property: CodeIsEnteredProperty, value: value);
	}

	public bool HaveError
	{
		get => GetValue(property: HaveErrorProperty);
		set => SetValue(property: HaveErrorProperty, value: value);
	}

	public int CountOfCell
	{
		get => GetValue(property: CountOfCellProperty);
		set => SetValue(property: CountOfCellProperty, value: value);
	}

	public ICommand? CompletedCommand
	{
		get => GetValue(property: CompletedCommandProperty);
		set => SetValue(property: CompletedCommandProperty, value: value);
	}

	public object? CompletedCommandParameter
	{
		get => GetValue(property: CompletedCommandParameterProperty);
		set => SetValue(property: CompletedCommandParameterProperty, value: value);
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

		CodeEntryPanel.Width = 100 * CountOfCell;
		CodeEntryPanel.ColumnDefinitions = new AutoScaleColumnDefinitions(columnCount: CountOfCell, owner: CodeEntryPanel);
		CodeEntryPanel.Children.AddRange(items: Enumerable.Range(start: 0, count: CountOfCell).Select(selector: column =>
		{
			TextBox tb = new TextBox();
			Grid.SetColumn(element: tb, value: column);
			tb.Classes.Add(name: DefaultClassForCell);
			this.WhenAnyValue(property1: code => code.HaveError)
				.Where(predicate: hasError => hasError)
				.Subscribe(onNext: _ => tb.Classes.Add(name: ClassForErrorCell));
			this.WhenAnyValue(property1: code => code.HaveError)
				.Where(predicate: hasError => !hasError)
				.Subscribe(onNext: _ => tb.Classes.Remove(name: ClassForErrorCell));

			tb.AddHandler(routedEvent: GotFocusEvent, handler: OnCellGotFocus);
			tb.AddHandler(routedEvent: TextBox.PastingFromClipboardEvent, handler: OnPastingToCellFromClipboard);
			tb.AddHandler(routedEvent: KeyDownEvent, handler: OnKeyDownInCell, routes: RoutingStrategies.Tunnel);

			tb.WhenAnyValue(property1: textBox => textBox.Text)
			  .Where(predicate: text => text is not null)
			  .Subscribe(onNext: _ => EntryCode = String.Concat(values: _codeCells.Select(selector: textBox => textBox.Text)).Trim());
			return tb;
		}));
		_codeCells = CodeEntryPanel.Children.OfType<TextBox>();
		string? codeCopy = EntryCode;
		int iterationCount = Math.Min(val1: _codeCells.Count(), val2: codeCopy?.Length ?? 0);
		for (int i = 0; i < iterationCount; ++i)
			_codeCells.ElementAt(index: i).Text = codeCopy?[index: i].ToString();
		_codeCells.ElementAt(index: iterationCount).Focus();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e: e);
		SetFocusOnLastCell();
	}

	private void SetFocusOnLastCell() =>
		(_codeCells.FirstOrDefault(predicate: tb => String.IsNullOrEmpty(value: tb.Text)) ?? _codeCells.Last()).Focus();

	private void OnKeyDownInCell(object? sender, KeyEventArgs e)
	{
		TextBox tb = (sender as TextBox)!;
		if (Char.IsLetterOrDigit(s: e.KeySymbol ?? " ", index: 0))
		{
			tb.Text = e.KeySymbol;
			e.Handled = true;
			tb.SelectionStart = tb.Text?.Length ?? 0;
			tb.ClearSelection();
			GetNextTextBox(current: tb)?.Focus();
		}

		switch (e)
		{
			case { Key: Key.Back, KeyModifiers: KeyModifiers.Control }:
				foreach(TextBox textBox in _codeCells.Take(count: _codeCells.IndexOf(item: tb) + 1))
					textBox.Text = String.Empty;
				_codeCells.First().Focus();
				e.Handled = true;
				return;
			case { Key: Key.Left, KeyModifiers: KeyModifiers.Control }:
				_codeCells.First().Focus();
				e.Handled = true;
				return;
			case { Key: Key.Right, KeyModifiers: KeyModifiers.Control }:
				_codeCells.Last().Focus();
				e.Handled = true;
				return;
		}

		if (new Key[] { Key.Back, Key.Left }.Contains(value: e.Key) && _codeCells.IndexOf(item: tb) > 0)
			GetPreviousTextBox(current: tb)?.Focus();

		if (new Key[] { Key.Delete, Key.Right }.Contains(value: e.Key) && _codeCells.IndexOf(item: tb) < _codeCells.Count())
		{
			if (e.Key == Key.Delete)
				tb.Text = String.Empty;
			e.Handled = true;
			GetNextTextBox(current: tb)?.Focus();
		}
	}

	private TextBox? GetNextTextBox(TextBox current)
		=> _codeCells.ElementAtOrDefault(index: _codeCells.IndexOf(item: current) + 1);

	private TextBox? GetPreviousTextBox(TextBox current)
		=> _codeCells.ElementAtOrDefault(index: _codeCells.IndexOf(item: current) - 1);

	private void OnCellGotFocus(object? sender, GotFocusEventArgs e)
	{
		HaveError = false;
		TextBox tb = (sender as TextBox)!;
		tb.SelectionStart = tb.Text?.Length ?? 0;
		tb.ClearSelection();
	}

	private async void OnPastingToCellFromClipboard(object? sender, RoutedEventArgs e)
	{
		e.Handled = true;
		IClipboard? clipboard = TopLevel.GetTopLevel(visual: this)?.Clipboard;
		string? code = await clipboard?.GetTextAsync();
		if (code is null)
			return;

		TextBox tb = (sender as TextBox)!;
		int index = _codeCells.IndexOf(item: tb);
		int iterationCount = Math.Min(val1: _codeCells.Count() - index, val2: code.Length);
		for (int i = 0; i < iterationCount; ++i)
			_codeCells.ElementAt(index: i + index).Text = code[index: i].ToString();
		_codeCells.ElementAt(index: iterationCount - 1).Focus();
	}
}