using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace MyJournal.Desktop.Assets.Resources.Behaviors;

public sealed class ListBoxRightClickBehavior : Behavior<ListBox>
{
	protected override void OnAttached()
	{
		base.OnAttached();
		AssociatedObject?.AddHandler(routedEvent: InputElement.PointerPressedEvent, handler: PointerPressedHandler, routes: RoutingStrategies.Tunnel);
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		AssociatedObject?.RemoveHandler(routedEvent: InputElement.PointerPressedEvent, handler: PointerPressedHandler);
	}

	private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
	{
		PointerPoint point = e.GetCurrentPoint(sender as Control);

		if (!point.Properties.IsRightButtonPressed || AssociatedObject is null)
			return;

		e.Handled = true;
	}
}