using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace MyJournal.Desktop.Assets.Controls;

public partial class EmailInput : UserControl
{
	private readonly string[] _domains = new string[]
	{
		"@mail.ru",
		"@gmail.com",
		"@yandex.ru",
		"Другое"
	};

	public static readonly StyledProperty<string> EntryEmailProperty = AvaloniaProperty.Register<PhoneNumberInput, string>(
		name: nameof(EntryEmail),
		defaultValue: String.Empty,
		defaultBindingMode: BindingMode.TwoWay
	);

	public static readonly StyledProperty<bool> HaveErrorProperty = AvaloniaProperty.Register<CodeInput, bool>(
		name: nameof(HaveError),
		defaultBindingMode: BindingMode.TwoWay
	);

	public EmailInput()
		=> InitializeComponent();

	public string EntryEmail
	{
		get => GetValue(property: EntryEmailProperty);
		set => SetValue(property: EntryEmailProperty, value: value);
	}

	public bool HaveError
	{
		get => GetValue(property: HaveErrorProperty);
		set => SetValue(property: HaveErrorProperty, value: value);
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

		PART_EmailName.Focus();

		PART_Domain.ItemsSource = _domains;

		if (!String.IsNullOrWhiteSpace(value: EntryEmail))
			SetEmail();
		else
			PART_Domain.SelectedIndex = 0;
	}

	private void OnEmailNameChanged(object? sender, TextChangedEventArgs e)
	{
		PART_Domain.IsVisible = !PART_EmailName.Text?.Contains(value: '@') ?? false;
		if (PART_Domain.IsVisible && PART_Domain.SelectedIndex == _domains.Length - 1)
			PART_Domain.SelectedIndex = 0;

		SetEnteredEmail();
	}

	private void OnDomainChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (PART_Domain.SelectedIndex == _domains.Length - 1)
		{
			PART_EmailName.Text += '@';
			PART_EmailName.SelectionStart = PART_EmailName.Text.Length;
			PART_EmailName.Focus();
			PART_EmailName.ClearSelection();
		}
		else SetEnteredEmail();
	}

	private void SetEnteredEmail()
		=> EntryEmail = PART_EmailName.Text + (PART_Domain.IsVisible ? PART_Domain.SelectedItem : String.Empty);

	private void SetEmail()
	{
		if (!EntryEmail.Contains(value: '@'))
		{
			PART_EmailName.Text = EntryEmail;
			return;
		}

		string[] parts = EntryEmail.Split(separator: '@');
		string enteredDomain = '@' + parts.Last();
		if (!_domains.Contains(value: enteredDomain))
		{
			PART_EmailName.Text = EntryEmail;
			return;
		}

		PART_EmailName.Text = parts.First();
		PART_Domain.SelectedItem = enteredDomain;
	}
}