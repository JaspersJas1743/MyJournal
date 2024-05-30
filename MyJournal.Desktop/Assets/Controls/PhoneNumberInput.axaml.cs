using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using DynamicData;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Controls;

public partial class PhoneNumberInput : UserControl
{
	private IEnumerable<MaskedTextBox> _cells;

	public static readonly StyledProperty<string> EntryPhoneProperty = AvaloniaProperty.Register<PhoneNumberInput, string>(
		name: nameof(EntryPhone),
		defaultValue: String.Empty,
		defaultBindingMode: BindingMode.TwoWay
	);

	public static readonly StyledProperty<bool> HaveErrorProperty = AvaloniaProperty.Register<CodeInput, bool>(
		name: nameof(HaveError),
		defaultBindingMode: BindingMode.TwoWay
	);

	public PhoneNumberInput()
		=> InitializeComponent();

	public string EntryPhone
	{
		get => GetValue(property: EntryPhoneProperty);
		set => SetValue(property: EntryPhoneProperty, value: value);
	}

	public bool HaveError
	{
		get => GetValue(property: HaveErrorProperty);
		set => SetValue(property: HaveErrorProperty, value: value);
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

		_cells = Panel.Children.OfType<MaskedTextBox>();

		this.WhenAnyValue(property1: input => input.EntryPhone)
			.Where(predicate: phone => !String.IsNullOrWhiteSpace(value: phone))
			.Subscribe(onNext: SetPhone);

		this.WhenAnyValue(property1: input => input.EntryPhone)
			.Where(String.IsNullOrEmpty).Subscribe(onNext: SetEmpty);

		foreach (MaskedTextBox mtb in _cells)
		{
			mtb.AddHandler(routedEvent: KeyDownEvent, handler: OnKeyDownToCell, routes: RoutingStrategies.Tunnel);
			mtb.AddHandler(routedEvent: TextBox.TextChangedEvent, handler: OnTextChanged);
			mtb.WhenAnyValue(property1: textBox => textBox.Text)
				.Subscribe(onNext: _ =>
				{
					EntryPhone = _cells.All(predicate: mtb => mtb.Text?.All(predicate: c => c != '_') == true) 
						? $"+7({FirstPartOfNumber.Text}){SecondPartOfNumber.Text}-{ThirdPartOfNumber.Text}" 
						: String.Empty;
				});
			mtb.WhenAnyValue(property1: textBox => textBox.Text).WhereNotNull()
			   .Where(predicate: text => text.All(predicate: c => c == '_'))
			   .Subscribe(onNext: _ => mtb.Classes.Add(name: "Empty"));
			mtb.WhenAnyValue(property1: textBox => textBox.Text).WhereNotNull()
			   .Where(predicate: text => text.Any(predicate: c => c != '_'))
			   .Subscribe(onNext: _ => mtb.Classes.Remove(name: "Empty"));
		}
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		(_cells.FirstOrDefault(predicate: tb => tb.Text?.Contains(value: '_') ?? false) ?? _cells.Last()).Focus();
	}

	private async void OnKeyDownToCell(object? sender, KeyEventArgs e)
	{
		MaskedTextBox maskedTextBox = (sender as MaskedTextBox)!;
		if (e is { Key: Key.V, KeyModifiers: KeyModifiers.Control })
			await PastePhone(e: e);
		else if (e.Key == Key.Back && (maskedTextBox.Text?.All(predicate: c => c == '_') ?? false))
			GetPreviousMaskedTextBox(current: maskedTextBox)?.Focus();
		else if (Char.IsLetterOrDigit(s: e.KeySymbol ?? " ", index: 0) && maskedTextBox.Text?.All(predicate: c => c != '_') == true)
			GetNextMaskedTextBox(current: maskedTextBox)?.Focus();
	}

	private void OnCellGotFocus(object? sender, GotFocusEventArgs e)
	{
		HaveError = false;
		MaskedTextBox maskedTextBox = (sender as MaskedTextBox)!;
		maskedTextBox.SelectionStart = maskedTextBox.Text!.Where(predicate: Char.IsDigit).Count();
		maskedTextBox.ClearSelection();
	}

	private void OnTextChanged(object? sender, TextChangedEventArgs e)
	{
		MaskedTextBox maskedTextBox = (sender as MaskedTextBox)!;
		if (maskedTextBox.MaskFull ?? false)
			GetNextMaskedTextBox(current: maskedTextBox)?.Focus();
		else if (maskedTextBox.Text?.All(predicate: c => c == '_') ?? false)
			GetPreviousMaskedTextBox(current: maskedTextBox)?.Focus();
	}

	private MaskedTextBox? GetNextMaskedTextBox(MaskedTextBox current)
		=> _cells.ElementAtOrDefault(index: _cells.IndexOf(item: current) + 1);

	private MaskedTextBox? GetPreviousMaskedTextBox(MaskedTextBox current)
		=> _cells.ElementAtOrDefault(index: _cells.IndexOf(item: current) - 1);

	private async void OnPastingToCellFromClipboard(object? sender, RoutedEventArgs e)
		=> await PastePhone(e: e);

	private async Task PastePhone(RoutedEventArgs e)
	{
		e.Handled = true;
		IClipboard? clipboard = TopLevel.GetTopLevel(visual: this)?.Clipboard;
		SetPhone(phone: await clipboard?.GetTextAsync()!);
	}

	private void SetPhone(string? phone)
	{
		if (phone is null)
			return;

		phone = String.Concat(values: phone.Where(predicate: Char.IsDigit).Skip(count: 1));
		int iterationCount = Math.Min(val1: _cells.Sum(selector: c => c.Text!.Length), val2: phone.Length);
		int cellIndex = 0;
		for (int i = 0; i < iterationCount;)
		{
			MaskedTextBox currentCell = _cells.ElementAt(index: cellIndex);
			currentCell.Text = phone.Substring(startIndex: i, length: Math.Min(val1: currentCell.Text!.Length, val2: phone.Length - i));
			i += currentCell.Text!.Length;
			++cellIndex;
		}
		_cells.ElementAt(index: cellIndex - 1 < 0 ? 0 : cellIndex - 1).Focus();
	}

	private void SetEmpty(string phone)
	{
		foreach (MaskedTextBox mtb in _cells)
			mtb.Clear();
	}
}