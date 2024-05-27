using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;

namespace MyJournal.Desktop.Assets.Controls;

public class CustomTabControl : TabControl
{
	private const string DefaultClass = "Default";
	private const string PreviousClass = "Previous";
	private const string NextClass = "Next";

	private TabItem? _previousNextSelection;
	private TabItem? _previousPreviousSelection;

	public CustomTabControl()
		=> AddHandler(routedEvent: SelectionChangedEvent, handler: SelectionChangedHandler);

	~CustomTabControl()
		=> RemoveHandler(routedEvent: SelectionChangedEvent, handler: SelectionChangedHandler);

	protected override void LogicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		base.LogicalChildrenCollectionChanged(sender, e);

		foreach (TabItem newItem in e.NewItems?.OfType<TabItem>() ?? Enumerable.Empty<TabItem>())
			newItem.Classes.Add(name: DefaultClass);

		foreach (TabItem oldItem in e.OldItems?.OfType<TabItem>() ?? Enumerable.Empty<TabItem>())
			oldItem.Classes.Remove(name: DefaultClass);

		if (LogicalChildren.OfType<TabItem>().ToArray() is not { Length: > 2 } items)
			return;

		items[0].IsSelected = true;
		items[1].Classes.Add(name: NextClass);
		_previousNextSelection = items[1];
	}

	private void SelectionChangedHandler(object? sender, SelectionChangedEventArgs e)
	{
		TabItem[] items = LogicalChildren.OfType<TabItem>().ToArray();

		TabItem? nextItem = items.ElementAtOrDefault(index: SelectedIndex + 1);
		if (nextItem == _previousNextSelection)
			return;

		nextItem?.Classes.Add(name: NextClass);
		_previousNextSelection?.Classes.Remove(name: NextClass);
		_previousNextSelection = nextItem;

		TabItem? previousItem = items.ElementAtOrDefault(index: SelectedIndex - 1);
		previousItem?.Classes.Add(name: PreviousClass);
		previousItem?.Classes.Remove(name: DefaultClass);

		_previousPreviousSelection?.Classes.Remove(name: PreviousClass);
		_previousPreviousSelection?.Classes.Add(name: DefaultClass);
		_previousPreviousSelection = previousItem;
	}
}