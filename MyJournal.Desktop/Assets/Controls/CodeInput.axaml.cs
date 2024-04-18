using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DynamicData;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public partial class CodeInput : UserControl
{
	public static readonly StyledProperty<string> EntryCodeProperty = AvaloniaProperty.Register<CodeInput, string>(
		name: nameof(EntryCode),
		defaultValue: String.Empty,
		defaultBindingMode: BindingMode.OneWayToSource
	);
	public static readonly StyledProperty<bool> HaveErrorProperty = AvaloniaProperty.Register<CodeInput, bool>(name: nameof(HaveError));
	public const int CountOfCell = 7;

	private readonly IEnumerable<TextBox> _codeCells;

	public CodeInput()
	{
		InitializeComponent();
		_codeCells = CodeEntryPanel.Children.OfType<TextBox>();
	}

	public string EntryCode
	{
		get => GetValue(property: EntryCodeProperty);
		set => SetValue(property: EntryCodeProperty, value: value);
	}

	public bool HaveError
	{
		get => GetValue(property: HaveErrorProperty);
		set => SetValue(property: HaveErrorProperty, value: value);
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

		foreach (TextBox tb in _codeCells)
		{
			tb.AddHandler(routedEvent: KeyDownEvent, handler: OnKeyDownInCell, routes: RoutingStrategies.Tunnel);
			tb.WhenAnyValue(property1: textBox => textBox.Text)
			  .Subscribe(onNext: _ => EntryCode = String.Concat(values: _codeCells.Select(selector: textBox => textBox.Text)).Trim());
		}
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e: e);
		_codeCells.First().Focus();
	}

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
			{
				foreach(TextBox textBox in _codeCells.Take(count: _codeCells.IndexOf(item: tb) + 1))
					textBox.Text = String.Empty;
				_codeCells.First().Focus();
				e.Handled = true;
				return;
			}
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
			GetNextTextBox(current: tb)?.Focus();
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
		for (int i = 0; i < _codeCells.Count() - index; ++i)
			_codeCells.ElementAt(index: i + index).Text = code[index: i].ToString();
	}
}